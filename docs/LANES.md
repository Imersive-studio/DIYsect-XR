# Lanes: who does what

The base is already built and tested on a laptop (see README). Each lane below is what is **left**, ordered by
priority. Aim for equal effort: lane B has the riskiest work, so A and C carry the extras.

Rule for every lane: finish the **must** items first, keep `main` green (`npm test`), and record a short screen
capture as soon as your must items work (it becomes backup demo footage).

---

## Lane A: Server, data, quality
**Owns:** `server/`, `shared/parts.js`, `shared/frog.js`, `scripts/`, `public/`, deployment, submission text.

| # | Task | Done when | Priority |
|---|---|---|---|
| A1 | **Public HTTPS link.** Cloudflare tunnel today, deployed host (Render/Fly/Railway) before demo day. | The Quest browser and a phone both open the home page over HTTPS. | MUST (day 1) |
| A2 | **Reset and rejoin.** A `reset` message (teacher) that puts the frog back together and clears questions. Make a reconnecting teacher/learner get everything back. | Refreshing a tab never loses the room state. | MUST |
| A3 | **Question queue.** Group duplicates ("4 students asked about the Heart"), sort by part or time, keep answered ones out of the way. | The teacher panel shows grouped counts. | MUST |
| A4 | **Better frog.** Improve the stylized frog (lumpy intestines, beating heart, nicer materials) or swap in a CC0 glTF with the **same part ids** and `coveredBy` rules. | Looks good in the Quest and on a phone, and part picking still works. | SHOULD |
| A5 | **Event log for the study.** Write every cut, question, answer (with timestamps) to a JSON file per room. | After a test session you can compute: questions per part, time to answer. | SHOULD |
| A6 | **Tests and device matrix.** Extend `npm test` for each new message. Test on iPhone Safari, Android Chrome, laptop, Quest browser. | A table of what works where, in `docs/`. | MUST |
| A7 | **Submission.** Draft the two-page abstract and help record the final video (`docs/JUDGING.md`). | Draft done two days before the deadline. | MUST |

## Lane B: Headsets (Quest 3)
**Owns:** `vr/`.

| # | Task | Done when | Priority |
|---|---|---|---|
| B1 | **First real Quest run.** Tunnel link, Enter VR, check frog placement and scale, controller ray, trigger cut, grips. Fix what breaks. | Teacher can take out skin and heart with controllers. Comfortable for 10 minutes. | MUST (day 1) |
| B2 | **Two-headset test.** Teacher and learner both in Quests. Check avatars, hands and the shared frog. | Each sees the other's head and hands in the right place. | MUST |
| B3 | **Student-view window.** When a student asks, show their view as a floating window beside the teacher (second camera rendered to a texture, using the question's `view`). The teacher chooses to "jump" with a short fade. **Never move the headset camera suddenly.** | Select a question and see exactly what the student saw. | MUST |
| B4 | **Guide line.** Teacher draws a line in the air (`guide` message); the learner sees it on the frog. | The learner follows a cut line the teacher drew. | SHOULD |
| B5 | **Polish.** Hover labels, sounds, haptics, readable text, nicer avatars with names. | A judge understands it without explanation. | SHOULD |
| B6 | **Hand tracking (optional).** Use bare hands if controllers feel clumsy. Keep controllers as the default. | Only if everything above works. | COULD |

## Lane C: Audience (phones and laptops)
**Owns:** `audience/`.

| # | Task | Done when | Priority |
|---|---|---|---|
| C1 | **Real-phone UX.** Test on an iPhone and an Android: gestures, safe areas, bottom sheet, tiny screens, rotation. | Nothing is cut off or fiddly on a small screen. | MUST (day 1) |
| C2 | **Translated pointer polish.** Cleaner arrows, "hidden under Skin" wording, a gentle pulse, a clear way to see both teacher and learner pointers. | A first-time student understands where to look. | MUST |
| C3 | **Quiz mode.** Teacher asks "Tap the Heart"; students tap; teacher sees how many got it. Needs a `quiz` message (coordinate with A). | The teacher sees live right and wrong counts in the headset. | MUST |
| C4 | **Follow modes.** Smooth transitions between my view, teacher's view and learner's view, a clear "back to my view", optional small picture-in-picture of the teacher's view. | Switching never feels jarring. | SHOULD |
| C5 | **States and resilience.** Reconnecting banner, empty states, offline hint, graceful errors. | Unplugging Wi-Fi and plugging it back in recovers. | SHOULD |
| C6 | **Visuals for the submission.** Branding, screenshots, GIFs, the home-page look. Pair with A on the video. | Assets ready two days before the deadline. | SHOULD |

---

## Checkpoints

| When | What must be true |
|---|---|
| End of day 1 | A1, B1, C1 done. A frog moves on a laptop, a phone and a Quest at once. |
| Middle of day 2 | Touch a part on a phone, see it pinned in the headset. B2, C2 done. |
| End of day 2 | The full loop works once, even roughly: cut, point, ask, answer. Record a backup video now. |
| Feature freeze | Stop adding features. Fix bugs, test on real devices, rehearse. |

## Fallbacks
- No second Quest: the learner uses a laptop (desktop mode already works).
- Controllers fiddly: skip hand tracking, use controllers.
- Quest link fails on demo day: use the laptop teacher mode, show the Quest on video.

## Working rules
- Change `shared/` only after telling the group.
- If your task needs a new message, ask lane A (or write it yourself with a smoke test, then tell them).
- Small commits, one branch per lane, merge at the checkpoints.
