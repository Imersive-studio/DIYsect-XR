# Related work

This page lists the closest published work we found and says how DIYsect differs. We checked each entry's title, authors, year and venue against its DOI record or official page. We read abstracts and metadata, not every paper in full, so read a paper before you quote it. DIYsect has only been run in the Unity editor so far. It has not been tested with real users, so nothing here claims a user benefit.

## Table

| # | Work | What it does | How DIYsect differs |
|---|---|---|---|
| 1 | Anatomy Studio II, Jorge et al., 2022, arXiv preprint [R1] | A cross-reality anatomy teaching system built on Visible Human data. Groups of students dissect together in real time with 3D views, sketching, audio and 2D tablets. | Closest system in topic. DIYsect adds phones and laptops as full classmates, a private camera for each viewer, and awareness cues (translated pointer, question pins, student view window). We cannot say what its cues cover beyond the abstract. |
| 2 | Stojanovska et al., 2019, Medical Science Educator [R2] | Compares HoloLens mixed reality anatomy with cadaveric dissection for learning. | A study of one headset learner against a cadaver class. It is not a technique for sharing views between users on different devices. DIYsect makes no learning claim. |
| 3 | Santos et al., 2026, Geosciences [R3] | A teacher uses VR to handle 3D site models. The teacher's actions are mirrored on a large display for the class. | One shared screen for everyone. DIYsect gives each classmate a private camera and re-draws the teacher's pointing in that camera. |
| 4 | De Lorenzis et al., 2024, Smart Learning Environments [R4] | A chemistry lab in VR where some learners use a headset and others use a desktop interface. | Same device mix idea, different domain. DIYsect adds teacher-to-class cues and a way for classmates to ask about a named organ. |
| 5 | Drey et al., CHI 2022 [R5] | Compares co-located pairs learning in two VR headsets with pairs where one uses a tablet. | A comparison of symmetric and asymmetric setups, not a new cue. DIYsect is a set of cues for the asymmetric case. |
| 6 | ShareVR, Gugenheimer et al., CHI 2017 [R6] | Lets a person without a headset join a headset user in the same room using floor projection and a tracked display. | Targets one bystander in a game. DIYsect targets a class and a teaching task, over a network. |
| 7 | Dollhouse VR, Ibayashi et al., SIGGRAPH Asia 2015 E-Tech [R7] | Designers use a top-down tabletop view while others use headsets inside the same space. | Shows two viewpoints on one shared scene. DIYsect adds an object-anchored pointer and question pins between them. |
| 8 | Mini-Me, Piumsomboon et al., CHI 2018 [R8] | A small avatar shows a remote VR user's gaze and gestures inside an AR user's view. | Shows a person's body in the other view. DIYsect shows the target part, not the person's body, so it also works for a viewer who cannot see that body. |
| 9 | Halo, Baudisch and Rosenholtz, CHI 2003 [R9] | Draws a ring for each off-screen object, with the arc reaching into the screen. | General off-screen cue for a screen. DIYsect uses an arrow and a hint ("under the omentum") for a part that is off-screen or hidden. We adapt the idea, we did not invent it. |
| 10 | Wedge, Gustafson et al., CHI 2008 [R10] | Wedge shapes show direction and distance of off-screen items without clutter. | Same as Halo. The cue gets its target from another user's pointer, not from a map. |

## What is new in DIYsect

Combined from known ideas:

- Off-screen arrows follow Halo and Wedge. Showing a teacher's attention to others follows Mini-Me and the large display setup in R3. Mixing headset and non-headset users follows R4, R5 and R6.

Where we think we add something, based on this search only:

- The translated pointer. The teacher's pointing is turned into a target organ and drawn in each classmate's own camera, including a hint when the organ is hidden. We did not find this in the works above.
- Question pins anchored to organs, with glowing clusters and an answered state, plus the teacher seeing the asker's exact viewpoint in a floating window. This closes the loop from class to teacher. R3 and R4 describe teacher-to-class sharing more than class-to-teacher.
- The guiding hand, where the teacher sets cut depth while the student steers, and the guide line. We did not find the same split of control in this list.
- Server-authoritative grabbing, one holder per organ. This is an engineering choice, not a research claim.

Limits of this table: we searched a small set of sources, and we may have missed work. "We did not find" does not mean it does not exist. None of these techniques has been tested with users yet.

## References

- R1: J. Jorge, P. Belchior, A. Gomes, M. Sousa, J. Pereira, J.-F. Uhl. Anatomy Studio II: A Cross-Reality Application for Teaching Anatomy. arXiv:2203.02186, 2022. https://arxiv.org/abs/2203.02186
- R2: M. Stojanovska et al. Mixed Reality Anatomy Using Microsoft HoloLens and Cadaveric Dissection: A Comparative Effectiveness Study. Medical Science Educator, 2019. https://doi.org/10.1007/s40670-019-00834-x
- R3: A. Santos et al. Beyond Traditional Learning with a New Reality: Geoscience Education Enhanced by 3D Reconstruction, Virtual Reality, and a Large Display. Geosciences, 2026. https://doi.org/10.3390/geosciences16010028
- R4: F. De Lorenzis et al. Combining virtual reality with asymmetric collaborative learning: a case study in chemistry education. Smart Learning Environments, 2024. https://doi.org/10.1186/s40561-024-00331-8
- R5: T. Drey et al. Towards Collaborative Learning in Virtual Reality: A Comparison of Co-Located Symmetric and Asymmetric Pair-Learning. CHI 2022. https://doi.org/10.1145/3491102.3517641
- R6: J. Gugenheimer, E. Stemasov, J. Frommel, E. Rukzio. ShareVR: Enabling Co-Located Experiences for Virtual Reality between HMD and Non-HMD Users. CHI 2017. https://doi.org/10.1145/3025453.3025683
- R7: H. Ibayashi et al. Dollhouse VR: A Multi-view, Multi-user Collaborative Design Workspace with VR Technology. SIGGRAPH Asia 2015 Emerging Technologies. https://doi.org/10.1145/2818466.2818480
- R8: T. Piumsomboon et al. Mini-Me: An Adaptive Avatar for Mixed Reality Remote Collaboration. CHI 2018. https://doi.org/10.1145/3173574.3173620
- R9: P. Baudisch, R. Rosenholtz. Halo: A Technique for Visualizing Off-Screen Objects. CHI 2003. https://doi.org/10.1145/642611.642695
- R10: S. Gustafson, P. Baudisch, C. Gutwin, P. Irani. Wedge: Clutter-Free Visualization of Off-Screen Locations. CHI 2008. https://doi.org/10.1145/1357054.1357179
