# Learning layer: how to add an organ card or a quiz question

The learning layer lives in `Assets/Scripts/Learning` (assembly `DIYsect.Learning`) with its tests in
`Assets/Tests/EditMode/Learning` and `Assets/Tests/PlayMode/Learning`. Content is data, so adding a card or a
question needs no code change.

## Add or change an organ info card

1. Open `Assets/Scripts/Learning/OrganInfo/OrganInfoLibrary.asset` in the Inspector.
2. Add an entry (or edit one):
   - **Organ Name**: the organ's object name in the body, which is its model file name without `.glb`
     (for example `liver`, `kidney_l`). It must match exactly, apart from letter case.
   - **Display Name**: what people read, for example `Left kidney`. Keep it the same as the floating name label.
   - **Function**: one plain sentence, at most 120 characters.
   - **Fact**: one short, true fact, at most 120 characters. Use textbook anatomy only.
3. Run the EditMode tests in `DIYsect.Learning.EditTests`. `OrganInfoLibraryTests` fails if an organ in the body
   has no entry, a field is empty or a line is too long.

A new organ model also needs its own entry; the card stays hidden for an organ the library does not know.

## Add or change a quiz question

1. Open `Assets/Scripts/Learning/Quiz/DefaultQuizQuestions.asset` (or create another set with
   **Create > DIYsect > Quiz Question Set** and assign it to the professor's `QuizPanel`).
2. Add a question:
   - **Prompt**: what classmates read, for example `Tap the organ that stores bile`. At most 120 bytes; longer
     text is cut. Leave it empty to show "Quiz: tap the <organ>".
   - **Target Organ**: the organ object name of the right answer, for example `gallbladder`.
3. Run the EditMode tests. `QuizQuestionSetTests` fails if a target organ is not in the body.

Only ask about organs a classmate can reach with a tap: an organ still under the skin, muscle or omentum is
covered, so the tap hits the layer above it.

## Where things are wired

- `Tools > DIYsect > Add Organ Info Cards` puts the card presenter on the body prefab and links the library.
- `Tools > DIYsect > Add Quiz` puts the quiz on the body, the professor panel in `Professor_MR` and the answer
  input in `Classmate_Desktop`, and links the question set.
- `Tools > DIYsect > Add Dissection Checklist` and `Tools > DIYsect > Add Sound Cues` add the checklist and the sound
  cues to the scenes.

Run each step again after rebuilding the body or the scenes. Never edit scenes or prefabs by hand.
