// End-to-end check of the server rules. Run with: npm test
// Starts the server on a random port, connects fake teacher/learner/audience clients,
// and checks the dissection rules, permissions, questions, attention and pose relay.

import { WebSocket } from 'ws';
import { startServer } from '../server/index.js';
import { C2S, S2C, ROLES } from '../shared/protocol.js';

const server = await startServer(0);
const port = server.address().port;

function client() {
  const ws = new WebSocket(`ws://127.0.0.1:${port}/ws`);
  const waiters = [];
  ws.on('message', (data) => {
    const msg = JSON.parse(data.toString());
    for (const w of [...waiters]) {
      if (w.pred(msg)) {
        waiters.splice(waiters.indexOf(w), 1);
        clearTimeout(w.timer);
        w.resolve(msg);
      }
    }
  });
  return {
    open: new Promise((r) => ws.on('open', r)),
    send: (o) => ws.send(JSON.stringify(o)),
    // Register BEFORE sending the action that should trigger the message.
    next: (pred, ms = 1500) =>
      new Promise((resolve, reject) => {
        const w = { pred, resolve };
        w.timer = setTimeout(() => {
          waiters.splice(waiters.indexOf(w), 1);
          reject(new Error('timed out waiting for message'));
        }, ms);
        waiters.push(w);
      }),
    close: () => ws.close(),
  };
}

const results = [];
async function check(name, fn) {
  try {
    await fn();
    results.push([true, name]);
  } catch (err) {
    results.push([false, `${name} -> ${err.message}`]);
  }
}
const assert = (cond, msg) => {
  if (!cond) throw new Error(msg);
};

const ROOM = 'TEST';
const teacher = client();
const learner = client();
const audience = client();
await Promise.all([teacher.open, learner.open, audience.open]);

await check('teacher joins and gets a welcome with empty state', async () => {
  const p = teacher.next((m) => m.type === S2C.WELCOME);
  teacher.send({ type: C2S.JOIN, room: ROOM, role: ROLES.TEACHER, name: 'Dr T' });
  const m = await p;
  assert(m.role === 'teacher' && m.state.removed.length === 0, 'bad welcome');
});

await check('learner joins; teacher sees the roster update', async () => {
  const p = teacher.next((m) => m.type === S2C.ROSTER && m.roster.some((u) => u.role === 'learner'));
  learner.send({ type: C2S.JOIN, room: ROOM, role: ROLES.LEARNER, name: 'Lee' });
  await p;
});

await check('second teacher is refused (role_taken)', async () => {
  const extra = client();
  await extra.open;
  const p = extra.next((m) => m.type === S2C.ERROR && m.code === 'role_taken');
  extra.send({ type: C2S.JOIN, room: ROOM, role: ROLES.TEACHER });
  await p;
  extra.close();
});

await check('audience joins', async () => {
  const p = audience.next((m) => m.type === S2C.WELCOME);
  audience.send({ type: C2S.JOIN, room: ROOM, role: ROLES.AUDIENCE, name: 'Sam' });
  await p;
});

await check('cannot take out the heart before skin and muscle', async () => {
  const p = teacher.next((m) => m.type === S2C.ERROR && m.code === 'rule');
  teacher.send({ type: C2S.REMOVE, partId: 'heart' });
  const m = await p;
  assert(/Muscle/.test(m.message), `unexpected message: ${m.message}`);
});

await check('removing skin is broadcast to everyone', async () => {
  const p = audience.next((m) => m.type === S2C.STATE && m.state.removed.includes('skin'));
  teacher.send({ type: C2S.REMOVE, partId: 'skin' });
  await p;
});

await check('learner can remove muscle, then the teacher can remove the heart', async () => {
  const p1 = teacher.next((m) => m.type === S2C.STATE && m.state.removed.includes('muscle'));
  learner.send({ type: C2S.REMOVE, partId: 'muscle' });
  await p1;
  const p2 = audience.next((m) => m.type === S2C.STATE && m.state.removed.includes('heart'));
  teacher.send({ type: C2S.REMOVE, partId: 'heart' });
  await p2;
});

await check('audience is not allowed to cut', async () => {
  const p = audience.next((m) => m.type === S2C.ERROR && m.code === 'forbidden');
  audience.send({ type: C2S.REMOVE, partId: 'liver' });
  await p;
});

await check('audience question appears in state with part and view', async () => {
  const p = teacher.next((m) => m.type === S2C.STATE && m.state.questions.length === 1);
  audience.send({
    type: C2S.ASK,
    partId: 'heart',
    text: 'Why is it so small?',
    view: { p: [1, 2, 3], q: [0, 0, 0, 1] },
  });
  const m = await p;
  const q = m.state.questions[0];
  assert(q.partId === 'heart' && q.view && q.status === 'open', 'question malformed');
});

await check('teacher can mark the question answered; audience cannot', async () => {
  const bad = audience.next((m) => m.type === S2C.ERROR && m.code === 'forbidden');
  audience.send({ type: C2S.ANSWER, questionId: 1 });
  await bad;
  const ok = audience.next((m) => m.type === S2C.STATE && m.state.questions[0]?.status === 'answered');
  teacher.send({ type: C2S.ANSWER, questionId: 1 });
  await ok;
});

await check('awareness: teacher gets aggregated attention counts', async () => {
  const p = teacher.next((m) => m.type === S2C.ATTENTION && m.counts.liver === 1);
  audience.send({ type: C2S.ATTENTION, partId: 'liver' });
  await p;
});

await check('teacher pose and pointer are relayed to the audience', async () => {
  const pose = audience.next((m) => m.type === S2C.POSE && m.role === 'teacher');
  teacher.send({ type: C2S.POSE, head: { p: [0, 1.6, 0], q: [0, 0, 0, 1] }, hands: [null, { p: [0, 1, -0.3], q: [0, 0, 0, 1] }] });
  await pose;
  const point = audience.next((m) => m.type === S2C.POINT && m.partId === 'liver');
  teacher.send({ type: C2S.POINT, partId: 'liver' });
  await point;
});

await check('cannot put the muscle back while the heart is still out', async () => {
  const p = teacher.next((m) => m.type === S2C.ERROR && m.code === 'rule');
  teacher.send({ type: C2S.RESTORE, partId: 'muscle' });
  await p;
});

await check('putting the heart back, then the muscle, works', async () => {
  const a = teacher.next((m) => m.type === S2C.STATE && !m.state.removed.includes('heart'));
  teacher.send({ type: C2S.RESTORE, partId: 'heart' });
  await a;
  const b = teacher.next((m) => m.type === S2C.STATE && !m.state.removed.includes('muscle'));
  teacher.send({ type: C2S.RESTORE, partId: 'muscle' });
  await b;
});

await check('garbage input does not crash the server', async () => {
  audience.send({ type: C2S.ATTENTION, partId: { evil: true } });
  audience.send({ type: C2S.ASK, partId: 'nope', text: 'x'.repeat(5000) });
  const p = teacher.next((m) => m.type === S2C.STATE, 1500);
  teacher.send({ type: C2S.REMOVE, partId: 'muscle' });
  await p;
});

let failed = 0;
for (const [ok, name] of results) {
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}`);
  if (!ok) failed++;
}
console.log(`\n${results.length - failed}/${results.length} checks passed`);
process.exit(failed ? 1 : 0);
