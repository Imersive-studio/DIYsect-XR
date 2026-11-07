// Realtime room server. Owns the truth about: which parts are removed, the guide line,
// the question queue, and who is in the room. Relays poses and pointer targets.
// Lane A owns this file. Message shapes: docs/PROTOCOL.md.

import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import { randomUUID, randomInt } from 'node:crypto';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { WebSocketServer } from 'ws';
import QRCode from 'qrcode';
import { PART_BY_ID } from '../shared/parts.js';
import { ROLES, C2S, S2C, ROOM_CODE_RE, LIMITS, canRemove, canRestore } from '../shared/protocol.js';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const THREE_DIR = path.join(ROOT, 'node_modules', 'three');
const PUBLIC_DIRS = new Set(['public', 'audience', 'vr', 'shared']);
const ROOM_IDLE_MS = 30 * 60 * 1000;

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.glb': 'model/gltf-binary',
  '.gltf': 'model/gltf+json',
  '.wasm': 'application/wasm',
};

// ---------------------------------------------------------------- rooms ------

const rooms = new Map();

function getRoom(code) {
  let room = rooms.get(code);
  if (!room) {
    room = {
      code,
      removed: new Set(),
      guide: null,
      questions: [],
      nextQuestionId: 1,
      users: new Map(),
      attentionDirty: false,
      emptyTimer: null,
    };
    rooms.set(code, room);
  }
  clearTimeout(room.emptyTimer);
  room.emptyTimer = null;
  return room;
}

function newRoomCode() {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  for (;;) {
    let code = '';
    for (let i = 0; i < 4; i++) code += alphabet[randomInt(alphabet.length)];
    if (!rooms.has(code)) return code;
  }
}

const snapshot = (room) => ({
  removed: [...room.removed],
  guide: room.guide,
  questions: room.questions,
});

const rosterOf = (room) =>
  [...room.users.values()].map((u) => ({ id: u.id, role: u.role, name: u.name }));

function send(ws, obj) {
  if (ws.readyState === 1) ws.send(JSON.stringify(obj));
}

function broadcast(room, obj, exceptId = null) {
  const data = JSON.stringify(obj);
  for (const u of room.users.values()) {
    if (u.id !== exceptId && u.ws.readyState === 1) u.ws.send(data);
  }
}

const sendError = (user, code, message) => send(user.ws, { type: S2C.ERROR, code, message });

const isVec = (a, n) =>
  Array.isArray(a) && a.length === n && a.every((x) => typeof x === 'number' && Number.isFinite(x) && Math.abs(x) < 1e4);

const isPose = (p) => p && isVec(p.p, 3) && isVec(p.q, 4);

const isStaff = (user) => user.role === ROLES.TEACHER || user.role === ROLES.LEARNER;

// -------------------------------------------------------- message handling ---

function handleJoin(user, msg, ws) {
  if (user.room) return;
  const code = String(msg.room ?? '').toUpperCase();
  if (!ROOM_CODE_RE.test(code)) return sendError(user, 'bad_room', 'Room code must be 4 letters or numbers');
  if (!Object.values(ROLES).includes(msg.role)) return sendError(user, 'bad_role', 'Unknown role');

  const room = getRoom(code);
  if (msg.role !== ROLES.AUDIENCE) {
    const taken = [...room.users.values()].some((u) => u.role === msg.role);
    if (taken) return sendError(user, 'role_taken', `The ${msg.role} seat is already taken in room ${code}`);
  }

  user.room = room;
  user.role = msg.role;
  user.name = String(msg.name ?? msg.role).slice(0, LIMITS.maxName) || msg.role;
  room.users.set(user.id, user);

  send(ws, {
    type: S2C.WELCOME,
    id: user.id,
    role: user.role,
    room: code,
    state: snapshot(room),
    roster: rosterOf(room),
  });
  // Catch a late joiner up on where teacher/learner currently are and are pointing.
  for (const u of room.users.values()) {
    if (u.id === user.id || !isStaff(u)) continue;
    if (u.pose) send(ws, { type: S2C.POSE, id: u.id, role: u.role, ...u.pose });
    send(ws, { type: S2C.POINT, id: u.id, role: u.role, partId: u.pointTarget ?? null });
  }
  broadcast(room, { type: S2C.ROSTER, roster: rosterOf(room) });
}

function handleMessage(user, msg) {
  if (msg.type === C2S.JOIN) return handleJoin(user, msg, user.ws);
  const room = user.room;
  if (!room) return;

  switch (msg.type) {
    case C2S.POSE: {
      if (!isStaff(user)) return;
      const now = Date.now();
      if (now - user.lastPoseAt < LIMITS.poseMinIntervalMs) return;
      if (!isPose(msg.head)) return;
      const hands = Array.isArray(msg.hands) ? msg.hands.slice(0, 2).map((h) => (isPose(h) ? h : null)) : [];
      user.lastPoseAt = now;
      user.pose = { head: msg.head, hands };
      broadcast(room, { type: S2C.POSE, id: user.id, role: user.role, ...user.pose }, user.id);
      return;
    }

    case C2S.POINT: {
      if (!isStaff(user)) return;
      const partId = msg.partId == null ? null : String(msg.partId);
      if (partId !== null && !PART_BY_ID[partId]) return;
      user.pointTarget = partId;
      broadcast(room, { type: S2C.POINT, id: user.id, role: user.role, partId }, user.id);
      return;
    }

    case C2S.REMOVE:
    case C2S.RESTORE: {
      if (!isStaff(user)) return sendError(user, 'forbidden', 'Only the teacher and learner can cut');
      const partId = String(msg.partId ?? '');
      const verdict = msg.type === C2S.REMOVE ? canRemove(room.removed, partId) : canRestore(room.removed, partId);
      if (!verdict.ok) return sendError(user, 'rule', verdict.reason);
      if (msg.type === C2S.REMOVE) room.removed.add(partId);
      else room.removed.delete(partId);
      broadcast(room, { type: S2C.STATE, state: snapshot(room) });
      return;
    }

    case C2S.GUIDE: {
      if (user.role !== ROLES.TEACHER) return sendError(user, 'forbidden', 'Only the teacher can draw a guide');
      if (msg.points == null) room.guide = null;
      else if (Array.isArray(msg.points) && msg.points.length <= LIMITS.maxGuidePoints && msg.points.every((p) => isVec(p, 3))) {
        room.guide = msg.points;
      } else return;
      broadcast(room, { type: S2C.STATE, state: snapshot(room) });
      return;
    }

    case C2S.ASK: {
      if (user.role !== ROLES.AUDIENCE) return;
      const now = Date.now();
      if (now - user.lastAskAt < LIMITS.askMinIntervalMs) return sendError(user, 'slow_down', 'Wait a moment before asking again');
      const partId = msg.partId == null ? null : String(msg.partId);
      if (partId !== null && !PART_BY_ID[partId]) return;
      const text = String(msg.text ?? '').trim().slice(0, LIMITS.maxText);
      const view = isPose(msg.view) ? { p: msg.view.p, q: msg.view.q } : null;
      user.lastAskAt = now;
      room.questions.push({
        id: room.nextQuestionId++,
        userId: user.id,
        name: user.name,
        partId,
        text,
        view,
        status: 'open',
        at: now,
      });
      if (room.questions.length > LIMITS.maxQuestions) {
        const firstDone = room.questions.findIndex((q) => q.status === 'answered');
        room.questions.splice(firstDone === -1 ? 0 : firstDone, 1);
      }
      broadcast(room, { type: S2C.STATE, state: snapshot(room) });
      return;
    }

    case C2S.ANSWER: {
      if (user.role !== ROLES.TEACHER) return sendError(user, 'forbidden', 'Only the teacher can answer');
      const q = room.questions.find((x) => x.id === msg.questionId);
      if (!q || q.status === 'answered') return;
      q.status = 'answered';
      broadcast(room, { type: S2C.STATE, state: snapshot(room) });
      return;
    }

    case C2S.ATTENTION: {
      if (user.role !== ROLES.AUDIENCE) return;
      const partId = msg.partId == null ? null : String(msg.partId);
      if (partId !== null && !PART_BY_ID[partId]) return;
      if (user.attention !== partId) {
        user.attention = partId;
        room.attentionDirty = true;
      }
      return;
    }
  }
}

function onConnection(ws) {
  const user = {
    id: randomUUID().slice(0, 8),
    ws,
    room: null,
    role: null,
    name: '',
    pose: null,
    pointTarget: null,
    attention: null,
    lastPoseAt: 0,
    lastAskAt: 0,
  };
  ws.on('message', (data, isBinary) => {
    if (isBinary) return;
    let msg;
    try {
      msg = JSON.parse(data.toString());
    } catch {
      return;
    }
    if (!msg || typeof msg.type !== 'string') return;
    try {
      handleMessage(user, msg);
    } catch (err) {
      console.error('handler error', err);
    }
  });
  ws.on('close', () => {
    const room = user.room;
    if (!room) return;
    room.users.delete(user.id);
    if (room.users.size === 0) {
      room.emptyTimer = setTimeout(() => rooms.delete(room.code), ROOM_IDLE_MS);
    } else {
      room.attentionDirty = true;
      broadcast(room, { type: S2C.ROSTER, roster: rosterOf(room) });
      if (isStaff(user)) broadcast(room, { type: S2C.POINT, id: user.id, role: user.role, partId: null });
    }
  });
}

/** Aggregated "where is the class looking" counts, ~4 times a second. */
function flushAttention() {
  for (const room of rooms.values()) {
    if (!room.attentionDirty) continue;
    room.attentionDirty = false;
    const counts = {};
    for (const u of room.users.values()) {
      if (u.role === ROLES.AUDIENCE && u.attention) counts[u.attention] = (counts[u.attention] ?? 0) + 1;
    }
    broadcast(room, { type: S2C.ATTENTION, counts });
  }
}

// ------------------------------------------------------------------ http -----

function serveStatic(req, res, pathname) {
  let p;
  try {
    p = decodeURIComponent(pathname);
  } catch {
    res.writeHead(400).end('Bad request');
    return;
  }
  if (p === '/') p = '/public/index.html';
  if (p.endsWith('/')) p += 'index.html';

  let base;
  let filePath;
  if (p.startsWith('/vendor/three/')) {
    base = THREE_DIR;
    filePath = path.join(THREE_DIR, p.slice('/vendor/three/'.length));
  } else {
    const top = p.split('/')[1];
    if (!PUBLIC_DIRS.has(top)) {
      res.writeHead(404).end('Not found');
      return;
    }
    base = ROOT;
    filePath = path.join(ROOT, p);
  }
  const resolved = path.resolve(filePath);
  if (!resolved.startsWith(base + path.sep)) {
    res.writeHead(403).end('Forbidden');
    return;
  }
  fs.readFile(resolved, (err, data) => {
    if (err) {
      res.writeHead(404).end('Not found');
      return;
    }
    res.writeHead(200, {
      'Content-Type': MIME[path.extname(resolved).toLowerCase()] ?? 'application/octet-stream',
      'Cache-Control': 'no-cache',
    });
    res.end(data);
  });
}

async function handleHttp(req, res) {
  const url = new URL(req.url ?? '/', 'http://local');

  if (url.pathname === '/api/new-room') {
    const room = newRoomCode();
    res.writeHead(200, { 'Content-Type': 'application/json' }).end(JSON.stringify({ room }));
    return;
  }

  if (url.pathname === '/api/qr') {
    const room = (url.searchParams.get('room') ?? '').toUpperCase();
    let origin;
    try {
      const u = new URL(url.searchParams.get('base') ?? '');
      if (u.protocol !== 'http:' && u.protocol !== 'https:') throw new Error('protocol');
      origin = u.origin;
    } catch {
      res.writeHead(400).end('Bad base URL');
      return;
    }
    if (!ROOM_CODE_RE.test(room)) {
      res.writeHead(400).end('Bad room code');
      return;
    }
    const svg = await QRCode.toString(`${origin}/audience/?room=${room}`, { type: 'svg', margin: 1, width: 280 });
    res.writeHead(200, { 'Content-Type': 'image/svg+xml', 'Cache-Control': 'no-store' }).end(svg);
    return;
  }

  serveStatic(req, res, url.pathname);
}

// ----------------------------------------------------------------- start -----

export function startServer(port = Number(process.env.PORT) || 3000) {
  const server = http.createServer((req, res) => {
    handleHttp(req, res).catch((err) => {
      console.error(err);
      if (!res.headersSent) res.writeHead(500);
      res.end('Server error');
    });
  });
  const wss = new WebSocketServer({ server, path: '/ws', maxPayload: 64 * 1024 });
  wss.on('connection', onConnection);
  const timer = setInterval(flushAttention, 250);
  server.on('close', () => clearInterval(timer));
  return new Promise((resolve) => server.listen(port, () => resolve(server)));
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const server = await startServer();
  const { port } = server.address();
  console.log(`\nDissect Together is running.\n  This computer:  http://localhost:${port}`);
  for (const list of Object.values(os.networkInterfaces())) {
    for (const ni of list ?? []) {
      if (ni.family === 'IPv4' && !ni.internal) console.log(`  Same Wi-Fi:     http://${ni.address}:${port}`);
    }
  }
  console.log('\nThe Quest browser needs HTTPS: see README.md ("Run it on the Quest").\n');
}
