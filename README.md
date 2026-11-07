# Dissect Together (working title)

One shared 3D frog. The **teacher** and one **learner** wear Meta Quest 3 headsets. The rest of the
class joins on phones or laptops by scanning a **QR code**, gets a private view of the same frog, and can
touch any part to ask a question. The teacher sees which part, where the class is looking, and what each
asking student was looking at.

Built for the IEEE VR 2027 3DUI Contest theme **"Beyond Shared Reality"** (and our local hackathon).
The contribution is the interaction techniques, not "a frog app". See `docs/JUDGING.md`.

## What works right now (verified)

| Piece | State |
|---|---|
| Room server: rooms, roles, dissection rules, questions, attention counts, QR codes | Works. `npm test` runs 15 checks and all pass. |
| Laptop and phone clients (student view, teacher/learner desktop mode) | Works. Checked in a browser: cut sync, raise hand, translated pointer, awareness dots. |
| Quest 3 (WebXR) code path | Written but **not yet run on a real headset**. First job for lane B. |

## Quick start

```bash
npm install
npm start          # http://localhost:3000
npm test           # server rules, 15 checks
```

1. Open the home page, press **Create a room**. You get a 4-letter code and a QR code.
2. Open **Teacher** and **Learner** on two laptop tabs (or two Quests) and a **Student** on your phone.
3. Teacher or learner: click a part to take it out (skin, then muscle, then organs). `R` puts it back.

Phones on the same Wi-Fi can open the **LAN address** the server prints (for example `http://192.168.0.161:3000`).
Open the home page through that address (not `localhost`), or the QR code will point at `localhost`.

## Run it on the Quest

The Quest browser only allows WebXR on **HTTPS** (or `localhost`). Pick one:

1. **Tunnel (easiest).** `brew install cloudflared`, then in a second terminal
   `cloudflared tunnel --url http://localhost:3000`. It prints an `https://...trycloudflare.com` link.
   Open that link in the Quest browser and in your phones. The link is public while the tunnel runs, so close it
   when you are done.
2. **USB cable.** Turn on developer mode for the Quest, plug it in, run
   `adb reverse tcp:3000 tcp:3000`, then open `http://localhost:3000` in the Quest browser.
3. **Deploy.** Put it on Render, Fly or Railway for the final demo. Do this before the last day.

On the Quest: open the home page, press **Open as Teacher** (or Learner), then tap **ENTER VR**.

## Repo map

```
shared/    parts.js (frog parts + coverage), protocol.js (messages + dissection rules), frog.js (builds the frog)
server/    index.js        rooms, rules, relay, QR            <- Lane A
vr/        index.html, main.js   Quest + laptop teacher/learner  <- Lane B
audience/  index.html, main.js   phone/laptop student view      <- Lane C
public/    index.html      home page: create room, QR
scripts/   smoke-test.js   npm test
docs/      PROJECT_BRIEF, PROTOCOL, LANES, PROMPTS, DEMO_SCRIPT, JUDGING
```

Nothing is copied from the internet: Three.js is installed from npm and served locally from
`/vendor/three/`, so the demo still works if the venue Wi-Fi has no internet (the tunnel/deploy is only
needed for the Quest's HTTPS).

## The three lanes (details in `docs/LANES.md`)

| Lane | Owner | Job |
|---|---|---|
| **A: Server, data, quality** | _name_ | Deploy, question queue, better frog, event log for the study, tests, backup video |
| **B: Headsets** | _name_ | Get Quest 3 working, student-view window, guide line, polish |
| **C: Audience** | _name_ | Phone UX, translated pointer polish, quiz mode, follow views, visuals |

Each person has a ready-to-paste AI prompt in `docs/PROMPTS.md`.

## Team rules

- `shared/` is the contract. **Tell the group before changing it.**
- One lane, one folder. If you need something from another lane, message them, don't edit their files.
- `git init`, then one branch per lane (`lane-a`, `lane-b`, `lane-c`), merge to `main` at every checkpoint.
- Never break `npm test` on `main`.
- Say honestly what is simulated or untested. Judges respect that.

## Key dates (IEEE VR 2027 3DUI Contest, from the official call)

| Date | What |
|---|---|
| Fri 11 Dec 2026 | Register (title and team members) |
| Wed 16 Dec 2026 | Two-page abstract and 3 to 5 minute video due |
| Wed 23 Dec 2026 | Notification |
| Tue 12 Jan 2027 | Camera-ready |

Check the hackathon's own dates and rules too; we did not have them.
