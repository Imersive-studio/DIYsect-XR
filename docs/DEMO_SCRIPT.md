# Demo script (about 90 seconds) and checklists

## The 90-second demo

| Time | What happens | What you say |
|---|---|---|
| 0:00 to 0:10 | A judge puts on the Quest as the teacher. Teammates have phones. | "In a headset, the teacher can't see the class, and students can't say which part they mean." |
| 0:10 to 0:30 | Judge takes out the skin, then the muscle. All phones update. Students rotate their own copies freely. | "One shared frog, but everyone has a private camera." |
| 0:30 to 0:50 | Judge points at the heart. Phones show "Teacher: Heart", with an arrow for a student who is looking from behind. | "The pointer is translated into each student's own view." |
| 0:50 to 1:10 | A student taps the liver and raises a hand. The judge sees a "?" pin and a glowing dot with a count. | "The teacher sees what was asked, and where the class is looking." |
| 1:10 to 1:25 | The judge opens the student's view in the floating window and answers. | "No guessing which part they meant." |
| 1:25 to 1:30 | Close. | "Shared state, private views, and the interface that connects them." |

Say the contribution out loud: **techniques for keeping a headset user and phone users aware of each other**,
not "frog dissection".

## Before every demo
- [ ] Server running, public HTTPS link opens on the Quest browser and two phones
- [ ] Two phones already joined to the room (so dots and pointer show immediately)
- [ ] Quest charged, guardian area set, controllers charged, room lit
- [ ] Laptop teacher mode open as a fallback
- [ ] Backup video on the laptop and on a phone
- [ ] `npm test` passes on the deployed branch
- [ ] Someone stands next to the headset user (safety, and to hand over the controllers)

## Backup video checklist
- [ ] Headset view recorded (Quest recording or casting)
- [ ] Phone views recorded at the same moment
- [ ] Show the sequence cut, point, ask, answer
- [ ] Captions for each technique
- [ ] 3 to 5 minutes, names and affiliations on screen (contest requirement), under 300 MB

## If something breaks
| Problem | Do this |
|---|---|
| Quest won't load the page | Use the laptop in teacher mode, show the headset video |
| Wi-Fi is bad | Use the LAN address on a hotspot, or play the backup video |
| A phone doesn't join | Re-scan the QR code, check the room code |
