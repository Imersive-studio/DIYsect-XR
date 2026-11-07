# Protocol

WebSocket at `/ws`, JSON messages `{ "type": "...", ... }`. Constants are in `shared/protocol.js`.
The server is the referee: clients may pre-check rules for instant feedback, the server decides.

## Frog space (important)
All poses on the wire are in **frog space**: origin at the frog's centre, `+z` toward the head, `+y` up.
The headset client places the frog on a virtual table (position `(0,1.05,-0.7)`, scale `0.3`) and converts
between world and frog space with `toFrog` / `fromFrog`. The audience client draws the frog at the origin
with scale 1, so it can use poses directly. Never send raw world coordinates.

## Roles
`teacher` (one per room), `learner` (one per room), `audience` (unlimited). A second teacher or learner gets
`error` with code `role_taken`.

## Client to server

| type | who | fields | notes |
|---|---|---|---|
| `join` | all | `room` (4 chars A-Z0-9), `role`, `name` | must be first. Room is created on first join. |
| `pose` | teacher, learner | `head: {p:[x,y,z], q:[x,y,z,w]}`, `hands: [left\|null, right\|null]` | ~15 per second; server drops faster ones |
| `point` | teacher, learner | `partId` or `null` | send on change; drives the translated pointer |
| `removePart` | teacher, learner | `partId` | rules in `canRemove` |
| `restorePart` | teacher, learner | `partId` | rules in `canRestore` |
| `guide` | teacher | `points: [[x,y,z], ...]` (max 64) or `null` | guide line in frog space |
| `ask` | audience | `partId`, `text` (max 280), `view: {p, q}` | one per 2 seconds per student |
| `answer` | teacher | `questionId` | marks answered |
| `attention` | audience | `partId` or `null` | which part I am looking at |

## Server to client

| type | fields | notes |
|---|---|---|
| `welcome` | `id`, `role`, `room`, `state`, `roster` | first reply to `join` |
| `state` | `state: { removed: [ids], guide, questions: [...] }` | sent to everyone on every change |
| `roster` | `roster: [{id, role, name}]` | who is in the room |
| `pose` | `id`, `role`, `head`, `hands` | relayed from teacher/learner to everyone else |
| `point` | `id`, `role`, `partId` | relayed pointer target |
| `attention` | `counts: { partId: n }` | aggregated, about 4 per second |
| `error` | `code`, `message` | codes: `bad_room`, `bad_role`, `role_taken`, `forbidden`, `rule`, `slow_down` |

A question: `{ id, userId, name, partId, text, view: {p,q}|null, status: "open"|"answered", at }`.

## Adding a message
1. Add the constant to `shared/protocol.js` and a row here.
2. Lane A adds the handler in `server/index.js` (validate every field) and a check in `scripts/smoke-test.js`.
3. Lanes B and C use it. Tell the group first.

Ideas that will need new messages: `quiz` (teacher asks "tap the heart", students answer), `follow`
(everyone follow me), `reset` (put the frog back together).
