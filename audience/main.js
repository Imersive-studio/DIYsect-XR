// Student view (phone / laptop). Lane C owns this file.
//
// What it does today:
//  - Shows the shared frog with a PRIVATE free camera (orbit/zoom never affects anyone).
//  - "Teacher's view" / "Learner's view": the camera follows their head, no video needed,
//    because we only receive their pose (frog space) and render our own copy of the frog.
//  - Tap a part -> ask the teacher about it (raise hand, with the part AND your view).
//  - Translated pointer: when the teacher/learner points at a part, it glows and a label
//    tells you where it is relative to YOUR camera (off-screen arrow, "hidden under Skin").
//  - Sends where you are looking so the teacher sees awareness dots.
//
// Ideas for lane C are listed in docs/LANES.md.

import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { buildFrog } from '/shared/frog.js';
import { PART_BY_ID } from '/shared/parts.js';
import { C2S, S2C, ROLES, LIMITS, ROOM_CODE_RE } from '/shared/protocol.js';

const $ = (s) => document.querySelector(s);
const params = new URLSearchParams(location.search);
const room = (params.get('room') ?? '').toUpperCase();

if (!ROOM_CODE_RE.test(room)) {
  $('#joinForm').hidden = false;
  $('#sheet').hidden = true;
  $('#modes').hidden = true;
  $('#joinForm').addEventListener('submit', (e) => {
    e.preventDefault();
    const v = $('#roomInput').value.trim().toUpperCase();
    if (ROOM_CODE_RE.test(v)) location.search = `?room=${v}`;
  });
} else {
  main();
}

function main() {
  const myName = (params.get('name') ?? '').slice(0, LIMITS.maxName) || `Student ${100 + Math.floor(Math.random() * 900)}`;
  $('#roomLabel').textContent = `Room ${room}`;

  // ------------------------------------------------------------- 3D scene ----
  const canvas = $('#c');
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0f1419);
  const camera = new THREE.PerspectiveCamera(50, 1, 0.05, 100);
  camera.position.set(2.8, 1.8, 3.4);

  const controls = new OrbitControls(camera, canvas);
  controls.enableDamping = true;
  controls.minDistance = 1.2;
  controls.maxDistance = 9;
  controls.enablePan = false;

  scene.add(new THREE.HemisphereLight(0xffffff, 0x334455, 1.1));
  const sun = new THREE.DirectionalLight(0xffffff, 1.6);
  sun.position.set(3, 5, 4);
  scene.add(sun);
  const grid = new THREE.GridHelper(8, 16, 0x2a3440, 0x1b232c);
  grid.position.y = -0.7;
  scene.add(grid);

  const frog = buildFrog(THREE);
  scene.add(frog.group);

  function resize() {
    renderer.setSize(innerWidth, innerHeight, false);
    camera.aspect = innerWidth / innerHeight;
    camera.updateProjectionMatrix();
  }
  addEventListener('resize', resize);
  resize();

  // Teacher / learner avatars (frog space, so no conversion needed here).
  function makeAvatar(color) {
    const g = new THREE.Group();
    const mat = new THREE.MeshStandardMaterial({ color, roughness: 0.5 });
    const head = new THREE.Group();
    head.add(new THREE.Mesh(new THREE.SphereGeometry(0.28, 24, 16), mat));
    const nose = new THREE.Mesh(new THREE.ConeGeometry(0.08, 0.24, 12), mat);
    nose.rotation.x = -Math.PI / 2; // cone tip points to head-forward (-z)
    nose.position.z = -0.32;
    head.add(nose);
    const hands = [0, 1].map(() => {
      const h = new THREE.Mesh(new THREE.SphereGeometry(0.11, 16, 12), mat);
      g.add(h);
      return h;
    });
    g.add(head);
    g.visible = false;
    scene.add(g);
    return { g, head, hands };
  }
  const avatars = { teacher: makeAvatar(0x4aa3ff), learner: makeAvatar(0xffa24a) };
  const poses = { teacher: null, learner: null };
  const pointTargets = { teacher: null, learner: null };

  // ------------------------------------------------------------ app state ----
  let removed = [];
  let questions = [];
  let roster = [];
  let myId = null;
  let selected = null; // part id I touched
  let viewMode = 'free';
  let savedFreeView = null;

  const raycaster = new THREE.Raycaster();
  const visibleMeshes = () => [...frog.meshes.values()].filter((m) => m.visible);
  function pick(clientX, clientY) {
    raycaster.setFromCamera(new THREE.Vector2((clientX / innerWidth) * 2 - 1, -(clientY / innerHeight) * 2 + 1), camera);
    return raycaster.intersectObjects(visibleMeshes(), false)[0]?.object.userData.partId ?? null;
  }

  // ----------------------------------------------------------- networking ----
  let ws = null;
  let joined = false;
  const send = (o) => ws?.readyState === 1 && ws.send(JSON.stringify(o));

  function connect() {
    ws = new WebSocket(`${location.protocol === 'https:' ? 'wss' : 'ws'}://${location.host}/ws`);
    ws.onopen = () => send({ type: C2S.JOIN, room, role: ROLES.AUDIENCE, name: myName });
    ws.onclose = () => {
      joined = false;
      $('#dot').classList.remove('on');
      setTimeout(connect, 1500);
    };
    ws.onmessage = (e) => {
      let msg;
      try {
        msg = JSON.parse(e.data);
      } catch {
        return;
      }
      onMessage(msg);
    };
  }

  function onMessage(msg) {
    switch (msg.type) {
      case S2C.WELCOME:
        joined = true;
        myId = msg.id;
        $('#dot').classList.add('on');
        applyState(msg.state);
        applyRoster(msg.roster);
        break;
      case S2C.STATE:
        applyState(msg.state);
        break;
      case S2C.ROSTER:
        applyRoster(msg.roster);
        break;
      case S2C.POSE:
        if (msg.role in poses) poses[msg.role] = { head: msg.head, hands: msg.hands ?? [] };
        break;
      case S2C.POINT:
        if (msg.role in pointTargets) {
          pointTargets[msg.role] = msg.partId;
          refreshGlow();
        }
        break;
      case S2C.ERROR:
        toast(msg.message);
        break;
    }
  }

  function applyState(state) {
    removed = state.removed;
    questions = state.questions;
    frog.applyRemoved(removed);
    if (selected && !frog.meshes.get(selected).visible) selected = null;
    refreshGlow();
    renderSheet();
  }

  function applyRoster(list) {
    roster = list;
    const has = (role) => list.some((u) => u.role === role);
    for (const btn of document.querySelectorAll('#modes button')) {
      const mode = btn.dataset.mode;
      if (mode !== 'free') btn.disabled = !has(mode);
    }
    if (viewMode !== 'free' && !has(viewMode)) setMode('free');
    const students = list.filter((u) => u.role === ROLES.AUDIENCE).length;
    $('#presence').textContent = `${students} student${students === 1 ? '' : 's'}`;
  }

  // ---------------------------------------------------------------- UI -------
  let toastTimer;
  function toast(text) {
    const el = $('#toast');
    el.textContent = text;
    el.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => el.classList.remove('show'), 2200);
  }

  function renderSheet() {
    const part = selected ? PART_BY_ID[selected] : null;
    $('#sheetTitle').textContent = part ? part.label : 'Touch any part of the frog';
    $('#sheetHint').textContent = part
      ? 'Ask your teacher about it. They will see which part you mean and what you were looking at.'
      : 'Then ask your teacher a question about it.';
    $('#askBox').hidden = !part;
    const mine = questions.filter((q) => q.userId === myId);
    $('#mine').innerHTML = mine
      .slice(-2)
      .map((q) => `<div><b>${q.partId ? PART_BY_ID[q.partId].label : 'General'}</b>: ${q.status === 'answered' ? 'answered' : 'waiting for your teacher'}</div>`)
      .join('');
  }

  function refreshGlow() {
    frog.clearGlow();
    if (pointTargets.learner) frog.setGlow(pointTargets.learner, 0xffa24a, 0.55);
    if (pointTargets.teacher) frog.setGlow(pointTargets.teacher, 0x4aa3ff, 0.65);
    if (selected) frog.setGlow(selected, 0x35e0c0, 0.5);
  }

  function setMode(mode) {
    if (mode === viewMode) return;
    if (viewMode === 'free') savedFreeView = { p: camera.position.clone(), q: camera.quaternion.clone() };
    viewMode = mode;
    for (const btn of document.querySelectorAll('#modes button')) btn.setAttribute('aria-pressed', String(btn.dataset.mode === mode));
    controls.enabled = mode === 'free';
    camera.fov = mode === 'free' ? 50 : 75;
    camera.updateProjectionMatrix();
    if (mode === 'free' && savedFreeView) {
      camera.position.copy(savedFreeView.p);
      camera.quaternion.copy(savedFreeView.q);
      controls.target.set(0, 0, 0);
      controls.update();
    }
  }
  document.querySelectorAll('#modes button').forEach((b) => b.addEventListener('click', () => setMode(b.dataset.mode)));

  // Tap (not drag) to select a part.
  let down = null;
  canvas.addEventListener('pointerdown', (e) => (down = { x: e.clientX, y: e.clientY, t: performance.now() }));
  canvas.addEventListener('pointerup', (e) => {
    if (!down) return;
    const moved = Math.hypot(e.clientX - down.x, e.clientY - down.y);
    const quick = performance.now() - down.t < 450;
    down = null;
    if (moved > 8 || !quick) return;
    selected = pick(e.clientX, e.clientY);
    refreshGlow();
    renderSheet();
    sendAttention(true);
  });

  $('#ask').addEventListener('click', () => {
    if (!selected) return;
    send({
      type: C2S.ASK,
      partId: selected,
      text: $('#text').value,
      view: { p: camera.position.toArray(), q: camera.quaternion.toArray() },
    });
    $('#text').value = '';
    toast('Hand raised. Your teacher can see this part and your view.');
  });
  $('#clear').addEventListener('click', () => {
    selected = null;
    refreshGlow();
    renderSheet();
    sendAttention(true);
  });

  // ------------------------------------------------- awareness (to teacher) ---
  let lastAttention;
  function sendAttention(force = false) {
    if (!joined) return;
    const part = selected ?? pick(innerWidth / 2, innerHeight / 2);
    if (force || part !== lastAttention) {
      lastAttention = part;
      send({ type: C2S.ATTENTION, partId: part });
    }
  }
  setInterval(() => sendAttention(), 250);

  // ------------------------------------- translated pointer (markers/arrows) ---
  const markerEls = { teacher: $('#markerTeacher'), learner: $('#markerLearner') };
  const arrowFor = (x, y) => {
    const a = Math.atan2(-y, x); // screen y is down
    const dirs = ['→', '↗', '↑', '↖', '←', '↙', '↓', '↘'];
    return dirs[Math.round(((a + Math.PI * 2) % (Math.PI * 2)) / (Math.PI / 4)) % 8];
  };

  function updateMarker(role) {
    const el = markerEls[role];
    const id = pointTargets[role];
    const mesh = id ? frog.meshes.get(id) : null;
    if (!mesh || !mesh.visible) {
      el.style.display = 'none';
      return;
    }
    const who = role === 'teacher' ? 'Teacher' : 'Learner';
    const world = mesh.getWorldPosition(new THREE.Vector3());
    const ndc = world.clone().project(camera);
    const behind = ndc.z > 1;
    let { x, y } = ndc;
    if (behind) {
      x = -x;
      y = -y;
    }
    const onScreen = !behind && Math.abs(x) <= 0.9 && Math.abs(y) <= 0.8;
    let text = `${who}: ${PART_BY_ID[id].label}`;
    if (onScreen) {
      raycaster.setFromCamera(new THREE.Vector2(x, y), camera);
      const hit = raycaster.intersectObjects(visibleMeshes(), false)[0];
      if (hit && hit.object.userData.partId !== id) text += ` (under ${PART_BY_ID[hit.object.userData.partId].label})`;
    } else {
      const k = Math.max(Math.abs(x) / 0.9, Math.abs(y) / 0.8, 1e-6);
      x /= k;
      y /= k;
      text = `${arrowFor(x, y)} ${text}`;
    }
    el.textContent = text;
    el.style.left = `${((x + 1) / 2) * innerWidth}px`;
    el.style.top = `${((1 - y) / 2) * innerHeight}px`;
    el.style.display = 'block';
  }

  // ------------------------------------------------------------- frame loop ---
  const tmpP = new THREE.Vector3();
  const tmpQ = new THREE.Quaternion();
  function updateAvatars() {
    for (const role of ['teacher', 'learner']) {
      const av = avatars[role];
      const pose = poses[role];
      const present = roster.some((u) => u.role === role) && pose;
      av.g.visible = Boolean(present) && viewMode !== role; // don't draw the head we are inside
      if (!present) continue;
      av.head.position.fromArray(pose.head.p);
      av.head.quaternion.fromArray(pose.head.q);
      av.hands.forEach((mesh, i) => {
        const h = pose.hands[i];
        mesh.visible = Boolean(h);
        if (h) mesh.position.fromArray(h.p);
      });
    }
  }

  function loop() {
    requestAnimationFrame(loop);
    if (viewMode === 'free') {
      controls.update();
    } else if (poses[viewMode]) {
      tmpP.fromArray(poses[viewMode].head.p);
      tmpQ.fromArray(poses[viewMode].head.q);
      camera.position.lerp(tmpP, 0.35);
      camera.quaternion.slerp(tmpQ, 0.35);
    }
    updateAvatars();
    updateMarker('teacher');
    updateMarker('learner');
    renderer.render(scene, camera);
  }

  renderSheet();
  connect();
  loop();
}
