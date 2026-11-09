<div align="center">

# DIYsect

### Collaborative anatomy learning across mixed reality, VR, and phones.

One shared specimen. A professor who can guide. A classroom that can participate.

![Unity](https://img.shields.io/badge/Unity-6-111827?logo=unity&logoColor=white)
![Meta Quest](https://img.shields.io/badge/Meta_Quest-3-0081FB?logo=meta&logoColor=white)
![Android](https://img.shields.io/badge/Android-3DDC84?logo=android&logoColor=white)
![OpenXR](https://img.shields.io/badge/OpenXR-6D5DFB)

**Immersion Studio Hackathon · November 6–9, 2026**

[Experience](#the-experience) · [Gallery](#gallery) · [Getting started](#getting-started) · [Team](#team)

<img src="docs/product/classmate.png" alt="The current DIYsect classmate interface with anatomy, view controls and participation tools" width="1100">

</div>

## Anatomy is a shared experience

A dissection should be easy to follow from every seat in the classroom. DIYsect connects the person teaching, the student working with the anatomy, and classmates exploring from their own devices.

The professor guides a shared human-anatomy model. The student handles the instruments in VR. Classmates examine the same specimen on their phones, identify organs, draw annotations and ask questions attached to the part they mean.

Everyone follows the same procedure, with their own view of the anatomy.

## The experience

| | Professor | Student | Classmates |
|---|---|---|---|
| **Device** | Meta Quest 3 | Meta Quest 3 | Android phone or Unity on a laptop |
| **View** | Mixed reality or virtual classroom | Immersive classroom | Orbit, zoom and follow views |
| **Interaction** | Guide cuts, adjust depth, answer questions and run quizzes | Use instruments, open layers, inspect organs and answer quizzes | Tap to identify, hold to ask, draw on the body and raise a hand |

### Learn by doing

Trace a midline incision, open the body layers, lift the omentum and examine the organs beneath it. Instruments, step guidance and organ information support the procedure. Shared ownership and validated dissection order keep the class working with the same anatomy.

### Teach with context

Draw a guide line, help control scalpel depth and inspect the viewpoint attached to a question. Question pins remain attached to organs, related questions form clusters, and answered questions are marked for the class. The close-up screen gives everyone another view of the work.

### Participate from any seat

Classmates can explore independently or follow the professor or student. A quick tap identifies an organ; a hold places a question pin. Drawing mode turns a finger movement into a shared annotation on the body. Raised hands, speaking controls and voice connect the discussion to the procedure.

### Check understanding together

The professor selects a question and starts a round. Headset students point at an organ and pull a trigger; phone classmates tap their answers. The professor sees the results and can reveal the answer to the class.

## Gallery

The classmate interface above supports independent exploration and participation. The professor leads questions from the classroom, and classmates join through the app.

<table>
  <tr>
    <td width="65%" valign="top">
      <img src="docs/product/professor-quiz.png" alt="Current professor quiz panel with question selection and round controls" width="100%">
      <p><strong>Professor controls.</strong> Select a question and lead the class through a quiz.</p>
    </td>
    <td width="35%" valign="top">
      <img src="docs/product/join.png" alt="Current classmate join screen with room code entry" width="100%">
      <p><strong>Join the classroom.</strong> Enter the professor's room from the classmate app.</p>
    </td>
  </tr>
</table>

<sub>Application views captured from the current Unity scene.</sub>

## Built for a connected classroom

The professor hosts the session. Clients send interaction requests, and the host validates changes to dissection state, organ ownership, questions and quiz results. Each device renders the synchronized anatomy locally while keeping its own camera.

| Area | Technology |
|---|---|
| Application | Unity 6000.3.28f1, C#, Universal Render Pipeline |
| XR and mixed reality | OpenXR, XR Interaction Toolkit, AR Foundation, Meta OpenXR |
| Multiplayer | Netcode for GameObjects, Unity Transport, Authentication, Lobby and Relay |
| Voice | Vivox |
| Anatomy assets | Z-Anatomy, Blender/Python processing, glTFast |

## Getting started

### Open the project

Install **Unity 6000.3.28f1** with Android Build Support, SDK/NDK tools and OpenJDK. Install Git LFS, then clone the repository:

```bash
git clone https://github.com/Imersive-studio/DIYsect-XR.git
cd DIYsect-XR
git lfs install --local
git lfs pull
```

Open the existing project folder in Unity Hub and allow packages and assets to import. Link the project to the team's Unity Gaming Services project, or configure your own Authentication, Lobby, Relay and Vivox services.

### Build the role apps

All three apps use `Assets/Scenes/Professor_MR.unity`, with the appropriate experience selected by the application role.

| Unity menu under Tools → DIYsect | APK |
|---|---|
| **Build Professor APK** | `Builds/Android-Professor/DIYsect-Professor.apk` |
| **Build Student APK** | `Builds/Android-Student/DIYsect-Student.apk` |
| **Build Classmate Phone APK** | `Builds/Android-Classmate/DIYsect-Classmate.apk` |

Install the matching APK on each device. Launch the professor first, followed by the student and classmates. The classroom uses cloud services and requires internet access.

The Unity editor defaults to the classmate experience. For an editor-hosted session, enter Play mode, wait for authentication, then select **Tools → DIYsect → Host Test Lobby (Play Mode)**.

[Installation and service configuration](docs/DEPLOYMENT.md) · [Headset controls](docs/VR_CONTROLS.md) · [Architecture](docs/ARCHITECTURE.md)

## Team

| Teammate | Contribution |
|---|---|
| **Aaditya** | Networking, shared anatomy state, organ ownership and depth guidance |
| **Saurav** | Student VR, classroom environment, incision and organ interaction |
| **Arjun** | Professor experience, Android classmates, question pins and shared views |

## Acknowledgements

Human anatomy is based on [Z-Anatomy](https://github.com/Z-Anatomy/The-blend), shared under **CC BY-SA 4.0**, with underlying BodyParts3D attribution retained. Environment assets include [University Lecture Hall by Heat Of Fusion](https://sketchfab.com/3d-models/university-lecture-hall-452cc01ebd1a47de9925bd43f44d7da4) and [surgical instruments by ferofluid](https://sketchfab.com/3d-models/surgical-instrument-2971a44d334b45ff926792e92a6bcee7), both **CC BY 4.0**. Seated audience imagery is derived from [Microsoft Rocketbox](https://github.com/microsoft/Microsoft-Rocketbox), **MIT**.

DIYsect is an educational prototype with simplified anatomy. [Full asset credits](docs/CREDITS.md) include attribution, modifications and licensing details.
