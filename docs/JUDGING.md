# Judging, requirements and submission

## IEEE VR 2027 3DUI Contest (17th)
Source: https://ieeevr.org/2027/contribute/3dui-contest/ (read the live page again before submitting).

**Theme:** "Beyond Shared Reality: 3D User Interfaces for Shared Spatial Interaction." New interaction
techniques that help users communicate, coordinate, manipulate shared content and keep awareness across
different devices, viewpoints and immersion levels. Any collaborative domain.

**Requirements:** address the theme; show novel 3DUI techniques across heterogeneous devices; say which
techniques were created, adapted or extended; 3 to 5 minute video; a standalone executable or clear
deployment instructions; OpenXR compatible where applicable. The primary contribution must be the
interaction design, not the application. Teams up to ten people.

**Deadlines (Anywhere on Earth):** registration Fri 11 Dec 2026 · abstract and video Wed 16 Dec 2026 ·
notification Wed 23 Dec 2026 · camera-ready Tue 12 Jan 2027. Conference 27 Feb to 3 Mar 2027, Melbourne.
At least one author must attend to present.

**Scoring:** expert judges try the systems, plus audience voting at the demo session.

## How our project maps to the five criteria

| Criterion | What we show | Gap to close |
|---|---|---|
| Innovation of the 3D user interface | Translated pointer, tap-to-ask with the asker's view, class awareness in the headset | Confirm novelty with a related-work check; make one technique clearly the headline |
| Support for shared spatial interaction | One shared frog state, private cameras, two headsets plus a class of phones | Show both directions of the loop in the video |
| Usability and user experience | Joins by QR code, no install, simple gestures | Test with a real teacher and a few students |
| Technical quality and robustness | Server-enforced rules, tests, offline-capable local assets | Deploy early, test on all devices, backup video |
| Overall impact and creativity | A real classroom problem (the teacher can't see the class) | Talk to one biology teacher; say what is simulated |

## Related work to cite and differentiate from (found in our research; read before citing)
- Anatomy Studio II, a cross-reality dissection system: https://arxiv.org/html/2203.02186
- Teacher in VR with students on a large display (geoscience): https://doi.org/10.3390/geosciences16010028
- VR plus desktop asymmetric collaboration in chemistry: https://slejournal.springeropen.com/articles/10.1186/s40561-024-00331-8
- Collaborative learning in VR, symmetric vs asymmetric (CHI 2022): https://www.uni-ulm.de/fileadmin/website_uni_ulm/iui.inst.100/1-hci/hci-paper/2022/CHI2022_Collaborative_Learning_in_VR_Drey.pdf
- Student and teacher meet in a shared VR (anatomy tutoring): https://arxiv.org/html/2011.07926v1
- BioDigital multiuser anatomy (commercial): https://www.wolterskluwer.com/en/solutions/lippincott-medicine/medical-education/biodigital
- The 2026 winner for style reference: https://wordpress.cs.vt.edu/3digroup/2026/02/04/from-slides-to-space-interactive-scale-navigation-for-xr-presentations

Findings worth using: asymmetric setups can reduce engagement when there is no teacher avatar, and teachers
can struggle to orient themselves. Our teacher avatar on student screens and the awareness dots address both.
Do a deeper literature search before claiming anything is new.

## Two-page abstract outline (VGTC format)
1. The collaborative problem (teacher in a headset cannot see the class)
2. System overview (roles, devices, architecture in one figure)
3. The techniques: shared state with private cameras, translated pointer, tap-to-ask, class awareness
4. Implementation (WebXR, Node, frog space, server-enforced rules)
5. Related work and what is different
6. Design process and iterations (what we tried and changed; keep notes as you build!)
7. Optional evaluation (a small pilot: a teacher plus 5 to 10 students; log from lane A5)

## Evaluation idea (small and honest)
Run one session with a teacher, a learner and 5 to 10 students. Log questions per part and time-to-answer.
Ask: "Did the teacher know where the class was looking?" and "Could students say what they meant?"
Report what you saw, with the small sample stated. Do not claim learning gains.
