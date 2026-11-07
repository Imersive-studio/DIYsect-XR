// Teacher / Learner client. Works in a Quest 3 (WebXR) AND on a laptop (desktop fallback,
// so the other two teammates can test without a headset). Lane B owns this file.
//
//   /vr/?room=ABCD&role=teacher     or     /vr/?room=ABCD&role=learner
//
// Quest controls:   trigger = take out the part you point at
//                   left grip = put the last part back
//                   right grip (teacher) = mark the oldest open question answered
// Laptop controls:  click a part = take it out, R = put back, A = answer, drag = orbit
//
// What the headset user sees that the audience does not:
//  - awareness dots: glowing balls on the parts students are looking at, with a count
//  - question pins ("?") on the parts students asked about, plus a floating panel
// Coordinates: everything sent over the network is in FROG SPACE (docs/PROTOCOL.md).

import * as THREE from 'three';
import { VRButton } from 'three/addons/webxr/VRButton.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { buildFrog } from '/shared/frog.js';
import { PART_BY_ID } from '/shared/parts.js';
import { C2S, S2C, ROLES, LIMITS, ROOM_CODE_RE, canRemove, canRestore } from '/shared/protocol.js';

const $ = (s) => document.querySelector(s);
const params = new URLSearchParams(location.search);
const room = (params.get('room') ?? '').toUpperCase();
const role = params.get('role') === 'learner' ? ROLES.LEARNER : ROLES.TEACHER;
const myName = (params.get('name') ?? '').slice(0, LIMITS.maxName) || (role === ROLES.TEACHER ? 'Teacher' : 'Learner');

$('#title').textContent = role === ROLES.TEACHER ? 'Teacher' : 'Learner';

if (!ROOM_CODE_RE.test(room)) {
  $('#who').textContent = 'No room code. Open this page from the home page (it creates a room for you).';
} else {
  main();
}

function main() {
  $('#who').textContent = `Room ${room}`;
  $('#help').innerHTML =
    'Laptop: <kbd>click</kbd> take out, <kbd>R</kbd> put back' + (role === ROLES.TEACHER ? ', <kbd>A</kbd> answer' : '') + ', drag to orbit.<br>Quest: trigger take out, left grip put back' + (role === ROLES.TEACHER ? ', right grip answer.' : '.');

  // ----------------------------------------------------------- 3D scene ----
  const canvas = $('#c');
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
  renderer.xr.enabled = true;
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0f1419);
  const camera = new THREE.PerspectiveCamera(60, 1, 0.05, 50);
  camera.position.set(0, 1.5, 0.7);

  scene.add(new THREE.HemisphereLight(0xffffff, 0x334455, 1.1));
  const sun = new THREE.DirectionalLight(0xffffff, 1.6);
  sun.position.set(2, 4, 2);
  scene.add(sun);
  const grid = new THREE.GridHelper(10, 20, 0x2a3440, 0x1b232c);
  scene.add(grid);

  // The frog sits on a virtual table in front of the user.
  const frog = buildFrog(THREE);
  frog.group.position.set(0, 1.05, -0.7);
  frog.group.scale.setScalar(0.3);
  scene.add(frog.group);
  const table = new THREE.Mesh(new THREE.BoxGeometry(1.4, 0.04, 0.9), new THREE.MeshStandardMaterial({ color: 0x2b3642 }));
  table.position.set(0, 0.78, -0.7);
  scene.add(table);

  const controls = new OrbitControls(camera, canvas);
  controls.target.set(0, 1.05, -0.7);
  controls.enableDamping = true;
  controls.enablePan = false;

  document.body.appendChild(VRButton.createButton(renderer));

  function resize() {
    renderer.setSize(innerWidth, innerHeight, false);
    camera.aspect = innerWidth / innerHeight;
    camera.updateProjectionMatrix();
  }
  addEventListener('resize', resize);
  resize();

  // ------------------------------------------------ frog-space conversions ---
  const _m = new THREE.Matrix4();
  const _inv = new THREE.Matrix4();
  const _p = new THREE.Vector3();
  const _q = new THREE.Quaternion();
  const _s = new THREE.Vector3();
  const _one = new THREE.Vector3(1, 1, 1);

  /** world matrix -> {p, q} in frog space */
  function toFrog(matrixWorld) {
    frog.group.updateWorldMatrix(true, false);
    _inv.copy(frog.group.matrixWorld).invert();
    _m.multiplyMatrices(_inv, matrixWorld).decompose(_p, _q, _s);
    return { p: _p.toArray(), q: _q.toArray() };
  }
  /** {p, q} in frog space -> world position + quaternion (written into the outputs) */
  function fromFrog(pose, outP, outQ) {
    frog.group.updateWorldMatrix(true, false);
    _m.compose(_p.fromArray(pose.p), _q.fromArray(pose.q), _one).premultiply(frog.group.matrixWorld);
    _m.decompose(outP, outQ, _s);
  }

  // ------------------------------------------------------ labels (sprites) ---
  function textSprite(text, { color = '#ffffff', bg = 'rgba(15,20,25,.85)', size = 0.5 } = {}) {
    const c = document.createElement('canvas');
    c.width = 256;
    c.height = 128;
    const g = c.getContext('2d');
    g.fillStyle = bg;
    g.beginPath();
    g.roundRect(8, 8, 240, 112, 24);
    g.fill();
    g.fillStyle = color;
    g.font = 'bold 72px system-ui, sans-serif';
    g.textAlign = 'center';
    g.textBaseline = 'middle';
    g.fillText(text, 128, 66);
    const mat = new THREE.SpriteMaterial({ map: new THREE.CanvasTexture(c), depthTest: false, transparent: true });
    const sprite = new THREE.Sprite(mat);
    sprite.scale.set(size, size / 2, 1);
    sprite.renderOrder = 20;
    return sprite;
  }

  // Awareness dots + question pins live inside the frog group, so they scale with it.
  const dots = new THREE.Group();
  const pins = new THREE.Group();
  frog.group.add(dots, pins);

  const topOf = (id) => {
    const part = PART_BY_ID[id];
    return new THREE.Vector3(part.pos[0], part.pos[1] + part.size[1] + 0.18, part.pos[2]);
  };

  function renderDots(counts) {
    dots.clear();
    for (const [id, count] of Object.entries(counts)) {
      const at = topOf(id);
      const ball = new THREE.Mesh(
        new THREE.SphereGeometry(0.06 + 0.035 * Math.sqrt(count), 20, 14),
        new THREE.MeshBasicMaterial({ color: 0xffb020, transparent: true, opacity: 0.9, depthTest: false }),
      );
      ball.renderOrder = 10;
      ball.position.copy(at);
      const label = textSprite(String(count), { color: '#1a1200', bg: 'rgba(255,176,32,.95)', size: 0.4 });
      label.position.copy(at).add(new THREE.Vector3(0, 0.2, 0));
      dots.add(ball, label);
    }
  }

  function renderPins() {
    pins.clear();
    for (const q of questions.filter((x) => x.status === 'open' && x.partId)) {
      const pin = textSprite('?', { color: '#04201a', bg: 'rgba(53,224,192,.95)', size: 0.4 });
      pin.position.copy(topOf(q.partId)).add(new THREE.Vector3(0.22, 0.05, 0));
      pins.add(pin);
    }
  }

  // Floating question panel (readable in the headset).
  const panelCanvas = document.createElement('canvas');
  panelCanvas.width = 768;
  panelCanvas.height = 512;
  const panelTex = new THREE.CanvasTexture(panelCanvas);
  const panel = new THREE.Mesh(new THREE.PlaneGeometry(0.66, 0.44), new THREE.MeshBasicMaterial({ map: panelTex, transparent: true }));
  panel.position.set(-0.85, 1.4, -0.55);
  panel.lookAt(0, 1.4, 0.3);
  scene.add(panel);

  function drawPanel() {
    const g = panelCanvas.getContext('2d');
    g.clearRect(0, 0, 768, 512);
    g.fillStyle = 'rgba(15,20,25,.92)';
    g.beginPath();
    g.roundRect(0, 0, 768, 512, 36);
    g.fill();
    const open = questions.filter((q) => q.status === 'open');
    g.fillStyle = '#35e0c0';
    g.font = 'bold 44px system-ui, sans-serif';
    g.fillText(`Questions (${open.length})`, 32, 70);
    g.font = '34px system-ui, sans-serif';
    g.fillStyle = '#e8eef5';
    open.slice(0, 5).forEach((q, i) => {
      const where = q.partId ? PART_BY_ID[q.partId].label : 'General';
      const line = `${where}: ${q.text || '(no text)'} - ${q.name}`;
      g.fillText(line.length > 40 ? `${line.slice(0, 39)}...` : line, 32, 130 + i * 60);
    });
    if (!open.length) {
      g.fillStyle = '#8ea0b3';
      g.fillText('No open questions', 32, 130);
    }
    g.fillStyle = '#ffb020';
    g.font = '30px system-ui, sans-serif';
    g.fillText(message, 32, 480);
    panelTex.needsUpdate = true;
  }

  // ---------------------------------------------------- remote avatar ---------
  function makeAvatar(color) {
    const g = new THREE.Group();
    const mat = new THREE.MeshStandardMaterial({ color, roughness: 0.5 });
    const head = new THREE.Group();
    head.add(new THREE.Mesh(new THREE.SphereGeometry(0.12, 20, 14), mat));
    const nose = new THREE.Mesh(new THREE.ConeGeometry(0.035, 0.1, 12), mat);
    nose.rotation.x = -Math.PI / 2;
    nose.position.z = -0.14;
    head.add(nose);
    const hands = [0, 1].map(() => {
      const h = new THREE.Mesh(new THREE.SphereGeometry(0.04, 12, 10), mat);
      g.add(h);
      return h;
    });
    g.add(head);
    g.visible = false;
    scene.add(g);
    return { g, head, hands };
  }
  const otherRole = role === ROLES.TEACHER ? ROLES.LEARNER : ROLES.TEACHER;
  const other = makeAvatar(otherRole === ROLES.TEACHER ? 0x4aa3ff : 0xffa24a);

  // ------------------------------------------------------------ app state ----
  let removed = new Set();
  let removedOrder = [];
  let questions = [];
  let roster = [];
  let message = '';
  let messageTimer;
  let hover = null;
  let remoteHover = null;

  function say(text) {
    message = text;
    $('#msg').textContent = text;
    drawPanel();
    clearTimeout(messageTimer);
    messageTimer = setTimeout(() => {
      message = '';
      $('#msg').textContent = '';
      drawPanel();
    }, 3000);
  }

  function refreshGlow() {
    frog.clearGlow();
    if (remoteHover) frog.setGlow(remoteHover, otherRole === ROLES.TEACHER ? 0x4aa3ff : 0xffa24a, 0.55);
    if (hover) frog.setGlow(hover, 0xffffff, 0.35);
  }

  function renderQuestionsHud() {
    $('#qs').innerHTML = questions
      .filter((q) => q.status === 'open')
      .slice(0, 5)
      .map((q) => `<div><b>${q.partId ? PART_BY_ID[q.partId].label : 'General'}</b> ${q.name}: ${q.text || '(no text)'}</div>`)
      .join('');
  }

  // ----------------------------------------------------------- networking ----
  let ws = null;
  let joined = false;
  const send = (o) => ws?.readyState === 1 && ws.send(JSON.stringify(o));

  function connect() {
    ws = new WebSocket(`${location.protocol === 'https:' ? 'wss' : 'ws'}://${location.host}/ws`);
    ws.onopen = () => send({ type: C2S.JOIN, room, role, name: myName });
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
        if (msg.role === otherRole) {
          fromFrog(msg.head, other.head.position, other.head.quaternion);
          msg.hands.forEach((h, i) => {
            other.hands[i].visible = Boolean(h);
            if (h) fromFrog(h, other.hands[i].position, _tmpQ);
          });
          other.g.visible = true;
        }
        break;
      case S2C.POINT:
        if (msg.role === otherRole) {
          remoteHover = msg.partId;
          refreshGlow();
        }
        break;
      case S2C.ATTENTION:
        renderDots(msg.counts);
        break;
      case S2C.ERROR:
        say(msg.message);
        break;
    }
  }
  const _tmpQ = new THREE.Quaternion();

  function applyState(state) {
    removed = new Set(state.removed);
    removedOrder = state.removed;
    questions = state.questions;
    frog.applyRemoved(state.removed);
    renderPins();
    renderQuestionsHud();
    drawPanel();
  }

  function applyRoster(list) {
    roster = list;
    const students = list.filter((u) => u.role === ROLES.AUDIENCE).length;
    const has = (r) => list.some((u) => u.role === r);
    $('#who').textContent = `Room ${room}  |  teacher ${has('teacher') ? 'yes' : 'no'}, learner ${has('learner') ? 'yes' : 'no'}, ${students} student${students === 1 ? '' : 's'}`;
    if (!has(otherRole)) other.g.visible = false;
  }

  // ----------------------------------------------------------- actions -------
  function tryRemove(id) {
    if (!id) return;
    const verdict = canRemove(removed, id);
    if (!verdict.ok) return say(verdict.reason);
    send({ type: C2S.REMOVE, partId: id });
  }
  function tryRestoreLast() {
    const id = removedOrder[removedOrder.length - 1];
    if (!id) return say('Nothing to put back');
    const verdict = canRestore(removed, id);
    if (!verdict.ok) return say(verdict.reason);
    send({ type: C2S.RESTORE, partId: id });
  }
  function answerOldest() {
    if (role !== ROLES.TEACHER) return;
    const q = questions.find((x) => x.status === 'open');
    if (!q) return say('No open questions');
    send({ type: C2S.ANSWER, questionId: q.id });
  }

  // ---------------------------------------------------- picking and input ----
  const raycaster = new THREE.Raycaster();
  const visibleMeshes = () => [...frog.meshes.values()].filter((m) => m.visible);
  const hitPart = () => raycaster.intersectObjects(visibleMeshes(), false)[0]?.object.userData.partId ?? null;

  // Desktop
  const mouse = new THREE.Vector2();
  function pickMouse(e) {
    mouse.set((e.clientX / innerWidth) * 2 - 1, -(e.clientY / innerHeight) * 2 + 1);
    raycaster.setFromCamera(mouse, camera);
    return hitPart();
  }
  let down = null;
  canvas.addEventListener('pointermove', (e) => {
    if (renderer.xr.isPresenting) return;
    const id = pickMouse(e);
    if (id !== hover) {
      hover = id;
      refreshGlow();
      sendPoint(id);
    }
  });
  canvas.addEventListener('pointerdown', (e) => (down = { x: e.clientX, y: e.clientY }));
  canvas.addEventListener('pointerup', (e) => {
    if (!down || renderer.xr.isPresenting) return;
    const moved = Math.hypot(e.clientX - down.x, e.clientY - down.y);
    down = null;
    if (moved < 6) tryRemove(pickMouse(e));
  });
  addEventListener('keydown', (e) => {
    if (e.key === 'r' || e.key === 'R') tryRestoreLast();
    if (e.key === 'a' || e.key === 'A') answerOldest();
  });

  // Quest controllers
  const controllers = [];
  function makeRay() {
    const line = new THREE.Line(
      new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(), new THREE.Vector3(0, 0, -1.2)]),
      new THREE.LineBasicMaterial({ color: 0xffffff }),
    );
    line.name = 'ray';
    return line;
  }
  function pickController(c) {
    const origin = c.getWorldPosition(new THREE.Vector3());
    const dir = new THREE.Vector3(0, 0, -1).applyQuaternion(c.getWorldQuaternion(new THREE.Quaternion()));
    raycaster.set(origin, dir);
    return hitPart();
  }
  function buzz(c) {
    try {
      c.userData.gamepad?.hapticActuators?.[0]?.pulse?.(0.5, 60);
    } catch {
      /* haptics are optional */
    }
  }
  for (let i = 0; i < 2; i++) {
    const c = renderer.xr.getController(i);
    c.addEventListener('connected', (e) => {
      c.userData.handedness = e.data.handedness;
      c.userData.gamepad = e.data.gamepad;
      c.userData.connected = true;
      c.add(makeRay());
    });
    c.addEventListener('disconnected', () => {
      c.userData.connected = false;
      c.clear();
    });
    c.addEventListener('selectstart', () => {
      tryRemove(pickController(c));
      buzz(c);
    });
    c.addEventListener('squeezestart', () => {
      if (c.userData.handedness === 'left') tryRestoreLast();
      else answerOldest();
      buzz(c);
    });
    scene.add(c);
    controllers.push(c);
  }

  // ---------------------------------------------------------- sending --------
  let lastPoint;
  let lastPointAt = 0;
  function sendPoint(id) {
    const now = performance.now();
    if (!joined || id === lastPoint || now - lastPointAt < 80) return;
    lastPoint = id;
    lastPointAt = now;
    send({ type: C2S.POINT, partId: id });
  }

  let lastPoseAt = 0;
  function sendPose(now) {
    if (!joined || now - lastPoseAt < 66) return;
    lastPoseAt = now;
    const headObj = renderer.xr.isPresenting ? renderer.xr.getCamera() : camera;
    headObj.updateWorldMatrix(true, false);
    const head = toFrog(headObj.matrixWorld);
    const hands = [null, null];
    controllers.forEach((c) => {
      if (!c.userData.connected) return;
      c.updateWorldMatrix(true, false);
      hands[c.userData.handedness === 'left' ? 0 : 1] = toFrog(c.matrixWorld);
    });
    send({ type: C2S.POSE, head, hands });
  }

  // -------------------------------------------------------------- loop -------
  renderer.setAnimationLoop((now) => {
    const inXR = renderer.xr.isPresenting;
    controls.enabled = !inXR;
    if (!inXR) controls.update();

    if (inXR) {
      let id = null;
      for (const c of [...controllers].reverse()) {
        if (!c.userData.connected) continue;
        id = pickController(c);
        if (id) break;
      }
      if (id !== hover) {
        hover = id;
        refreshGlow();
      }
      sendPoint(id);
    }

    sendPose(now);
    renderer.render(scene, camera);
  });

  connect();
  drawPanel();
}
