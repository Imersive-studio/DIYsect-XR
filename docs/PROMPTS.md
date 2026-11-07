# Prompts for each teammate

Paste the **shared context** first, then your **lane prompt**, into your AI coding assistant
opened in this project folder. Each prompt tells the assistant what to touch and what "done" means.

---

## Shared context (everyone pastes this first)

```
You are helping a team of three students build "Dissect Together" for a hackathon and the IEEE VR 2027
3DUI Contest (theme: "Beyond Shared Reality: 3D User Interfaces for Shared Spatial Interaction").

The product: a teacher and one learner wear Meta Quest 3 headsets and dissect one shared 3D frog. The rest
of the class joins on phones/laptops by scanning a QR code, sees the same frog from a PRIVATE camera, can
follow the teacher's or learner's view, and can touch any part of the frog to raise a hand and ask a question.
The teacher sees which part was asked about, glowing awareness dots where students are looking, and can
answer in real time or look at the asking student's view.

The contribution is the INTERACTION TECHNIQUES, not the frog: shared state + private cameras, translated
pointer (the teacher's pointer shown correctly in every student's own view), tap-to-ask with the asker's view
attached, and class awareness inside the headset.

Read first: README.md, docs/PROJECT_BRIEF.md, docs/PROTOCOL.md, docs/LANES.md.
Stack: Node 20+, ws, Three.js served locally from /vendor/three/, plain ES modules (no bundler), WebXR.

Hard rules:
- shared/ is the contract between three people. Do NOT change it without telling me first; if a change is
  needed, propose the exact diff and wait.
- Only edit files in my lane (below). If I need something from another lane, tell me what to ask them.
- Keep `npm test` passing. Add a check to scripts/smoke-test.js for any server behaviour you add.
- Poses on the wire are in frog space (see docs/PROTOCOL.md). Never send world coordinates.
- Rules (what can be cut, who can do what) are enforced by shared/protocol.js + the server, not the clients.
- Never move a VR user's camera suddenly (motion sickness): use fades or floating windows.
- No real patient data, no medical or learning-outcome claims.
- Be honest about what you tested. Say "untested on a headset" when it is.
- Work in small steps and show me what to run to see each change.
```

---

## Lane A prompt: Server, data, quality

```
My lane is A: server, data and quality. I own server/, shared/parts.js, shared/frog.js, scripts/, public/,
deployment, and the submission text. Do not edit vr/ or audience/.

Work through these in order, one at a time, and stop after each so I can check:
A1  Public HTTPS link so the Quest browser and phones can open it (Cloudflare quick tunnel now; then a
    deploy config for Render or Fly). Update README.md with exact steps that I verified.
A2  A `reset` message (teacher only) that puts the frog back together and clears questions; make a
    reconnecting teacher/learner recover the full state. Add smoke tests.
A3  Group duplicate questions per part ("4 students asked about the Heart") and expose the grouped list in
    `state` without breaking the existing `questions` shape (propose the shape to me first).
A4  Improve the stylized frog in shared/frog.js (lumpy intestines, a beating heart, nicer materials), keeping
    the part ids and `coveredBy` rules from shared/parts.js. Optionally swap in a CC0 glTF with the same ids.
A5  An event log: append every cut, question and answer with timestamps to recordings/<room>.json so we can
    compute questions per part and time-to-answer for the paper.
A6  Extend `npm test` for each new message and write a device test matrix in docs/DEVICES.md
    (iPhone Safari, Android Chrome, laptop, Quest browser).
A7  Draft the two-page abstract using docs/JUDGING.md.

Definition of done for each: it works on a real device, `npm test` passes, README or docs updated.
Before you start, read the code and tell me in 5 lines what already exists and what you will change first.
```

---

## Lane B prompt: Headsets (Quest 3)

```
My lane is B: the Meta Quest 3 client. I own vr/ only. Do not edit audience/, server/ or shared/ (ask me to
coordinate with the owner if you need a change there).

The file is vr/main.js (WebXR with a laptop fallback). The XR code path is written but has NEVER been run on a
real headset, so assume there are bugs. Work through these one at a time and stop after each:
B1  Get a real Quest 3 run working over HTTPS (see README "Run it on the Quest"). Check frog placement and
    scale, the controller rays, trigger = take out a part, left grip = put back, right grip = answer. Fix what
    breaks and tell me exactly what I should look for in the headset.
B2  Two-headset test: teacher and learner each in a Quest. Check the other person's head and hands appear in
    the right place in frog space.
B3  Student-view window: when the teacher selects a question, render that student's view (question.view
    in frog space) into a texture shown as a floating panel beside the teacher, with an optional "jump"
    that uses a short fade. Never move the camera abruptly.
B4  Guide line: the teacher draws a line in the air with a button (send `guide` in frog space); the learner
    sees it on the frog.
B5  Polish: hover labels, haptics, sounds, readable text, named avatars.
B6  Optional: hand tracking, with controllers staying the default.

Definition of done: works in the headset for 10 minutes without discomfort, the laptop fallback still works,
and you tell me honestly what you could not test. Before starting, read vr/main.js and summarise it in 5 lines.
```

---

## Lane C prompt: Audience (phones and laptops)

```
My lane is C: the student view for phones and laptops. I own audience/ only. Do not edit vr/, server/ or
shared/ (ask me to coordinate if you need a change there).

Files: audience/index.html and audience/main.js. It already has: a private orbit camera, follow modes
(teacher's / learner's view), tap-to-ask, a translated pointer (glow + label + off-screen arrow + "under
Skin"), and attention reporting. Work through these in order, one at a time:
C1  Test and fix the real-phone UX on an iPhone and an Android: gestures, safe areas, bottom sheet,
    rotation, small screens.
C2  Polish the translated pointer: cleaner arrows, clear wording when a part is hidden, a gentle pulse, and an
    unmistakable difference between the teacher's and the learner's pointer.
C3  Quiz mode: the teacher says "Tap the Heart"; students tap; the teacher sees how many got it right. This
    needs a new `quiz` message, so first write the exact message shapes and ask me to agree them with the
    server owner (lane A) and the headset owner (lane B) before coding.
C4  Smooth transitions between my view, teacher's view and learner's view, a clear "back to my view", and
    an optional small picture-in-picture of the teacher's view while exploring.
C5  Resilience: a reconnecting banner, empty states, graceful errors. Check that dropping the Wi-Fi and
    restoring it recovers.
C6  Visuals for the submission: branding, screenshots and GIFs.

Definition of done: works on a real phone, no horizontal scrolling or cut-off controls, the 16px side gutter
is respected, and you tell me honestly what you could not test. Before starting, read audience/main.js and
summarise it in 5 lines.
```
