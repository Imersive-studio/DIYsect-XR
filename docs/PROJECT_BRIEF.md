# Project brief

## The problem
In a dissection class, only the people nearest the specimen can really see. If the teacher teaches in VR,
the class can follow on phones, but then the teacher is in a headset and **cannot see the class**: who is
following, who is lost, who is exploring. And students looking at the same 3D frog from different angles
cannot say "this one" and be understood.

## One sentence
A teacher guides one learner through a 3D dissection in Quest 3 headsets while the whole class follows on
phones, and the interface keeps all of them aware of each other's views, questions and attention.

## The people and their devices

| Role | Device | What they can do |
|---|---|---|
| Teacher | Quest 3 (laptop fallback) | Guides the dissection, sees awareness dots and question pins, answers questions |
| Learner (one student) | Quest 3 (laptop fallback) | Does the dissection with the teacher guiding |
| Class | Phone or laptop via QR code | Private view of the frog, follow teacher's or learner's view, touch a part to ask |

## "Beyond shared reality": the techniques we are contributing

1. **Shared state, private camera.** The frog's state (which parts are out) is shared; every viewer's camera
   is private. A viewer can snap to the teacher's or learner's view and back.
2. **Translated pointer.** When the teacher points at a part, every student's screen shows *that part* in
   *their own* view: it glows, an arrow points to it if it is off-screen, and a label says "under Skin"
   if it is hidden. (Built; to be polished.)
3. **Tap-to-ask.** A question is anchored to a touched part and carries the asker's view, so the teacher
   knows exactly what was meant. (Built; the floating student-view window is still to do.)
4. **Class awareness inside the headset.** Glowing dots with counts show the teacher where students are
   looking, and "?" pins show open questions. (Built.)

## Rules enforced by the server
You cannot take out the heart before the skin and muscle; you cannot put the muscle back while an organ is
still out; only teacher and learner can cut; only the teacher can answer. These live in
`shared/protocol.js` and the server checks them.

## Explicit non-goals and honest limits
- Not a medical device, no real patient data, no claim that it improves learning or surgical outcomes.
- The frog is a stylized model, not anatomically exact.
- The Quest 3 path has not yet been run on a real headset (see README).
- Whether a biology teacher wants this has not been checked. Talk to one.

## Possible later direction
The same system with a different model could support operating-room team *training or case review*
(never live surgery). Keep the content swappable: part ids and coverage rules live in `shared/parts.js`.
