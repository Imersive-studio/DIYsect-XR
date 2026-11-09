# State machines

Some objects in DIYsect move through phases (an organ is home, held, easing back or loose). Each phase must live in **one explicit place**, not in a mix of booleans, "no one" ids, running coroutines or physics flags.

## House pattern

Copy `DissectionStage` + `LayerOrderRule`:

1. One enum holds the state.
2. A plain C# rule table decides `(state, event) -> result, next state`. It is unit tested in EditMode.
3. One writer. For networked phases that is the server; every peer and late joiners can read it.
4. A change event. Presenters (labels, haptics, physics) react to it and never keep their own copy.

Prove a state bug with a failing test before fixing it. Old public properties stay as derived properties so existing code and tests keep working.

## Plan (branch `refactor/explicit-state`)

| Step | What | Fixes | Owner |
|---|---|---|---|
| 1 | Failing tests for the bugs below | proof before change | Saurav |
| 2 | `GrabRequest` (Idle, Requested, Granted, Releasing) shared by organ grab, scalpel grab, depth handle, guide pen | a "yes" after you let go leaves a stale flag; depth handle stuck streaming; pen ignores a "no" | Saurav |
| 3 | `OrganPhase` (Home, Held, Easing, Loose), server written, replicated | phase spread over 5 places; clients cannot see loose or easing | Saurav |
| 4 | `ExclusiveClaim`: one type for NoHolder, NoSteerer, NoController, NoDrawer | four hand copies of "one at a time" | Saurav |
| 5 | Student view window mode; table alignment ignores plane detection while carrying; pen ends its stroke when disabled | stops following new questions; table vanishes mid-carry; pen blocks others | Saurav |
| 6 | `QuitMenuState` (Closed, Open, Confirming) with a pure rule table in `QuitMenuRules`, one writer in `QuitMenu` | quit by accident; a menu that cannot be closed | Saurav |
| 7 | `ChecklistTransition` (Snap, Advance, Revert, Reset, Complete), pure `ChecklistTransitionRule`; `ChecklistPanel` writes presentation and raises `TransitionChanged` | simultaneous steps, re-enabled panels, quiet reversals; learning state stays in `ChecklistTracker` | Prajwal |
| 8 | `ChecklistCueStrength` (Subtle, Explicit), pure `ChecklistEscalationRule`; `ChecklistBodyCues` writes strength and raises `StrengthChanged` | increase guidance after 12 seconds without progress, reset on step or incision progress | Prajwal |

Later, lower risk: an explicit "cutting" dissection stage, scalpel zone reporting, one organ presentation phase for hover, label, ghost and haptics.

If you are adding a feature with phases, follow the house pattern and add it to this table.

## Professor tools together

The professor has three tools in `Professor_MR`: the guide pen, the depth handle (guiding hand) and the student view window. They are independent on purpose: each has its own state and its own server-side owner, so none of them needs to know about the others.

| Tool | Input | What it touches | State and owner |
|---|---|---|---|
| Guide pen (`GuideLinePen`) | Grab the pen (grip), hold the trigger to draw | Samples its own tip position. It does not raycast and has no trigger that the scalpel or forceps react to | `GrabRequest` phase on the pen; the server (`GuideLine`) holds the drawer and the points |
| Depth handle (`DepthControlInput`) | Grab the handle (grip), lower or raise it | Streams the handle height as the cut depth | `GrabRequest` phase on the handle; the server (`GuidingHand`) holds the controller and the depth |
| Student view window (`StudentViewWindow`) | Ray-select the previous and next buttons; it opens by itself when a question has a view | Moves its own camera; the panel and the buttons are on the `CloseUpScreen` layer and the screen and frame have no collider | `StudentViewMode`, local to the professor's client; derived from the question board |

What blocks what:

- Each tool is a separate grab interactable, and a hand holds one at a time. Taking the handle with the hand that holds the pen lets go of the pen, which ends the stroke through the pen's own release path. Drawing with one hand while the other holds the handle is allowed and works: the line and the depth are separate server objects.
- The pen is blocked only by another drawer (the server answers no), the handle only when nobody can be guided (`CanRequestControl`). Neither looks at the other.
- The window never blocks a tool and no tool blocks it. A question with a view opening the window, or stepping through it, changes no pen or handle state.
- The pen and the handle exist only in the professor's half of `Professor_MR` (`ClassroomRole` switches them off on every other device), so a classmate cannot grab them.

Other professor inputs and how they meet the tools:

| Input | What it does | Effect on the tools |
|---|---|---|
| Left thumbstick, right thumbstick (walk, snap turn) | Moves or turns the rig | None. A walk moves the hand sideways, and the handle reads only its height, so the depth does not change. A drawn point is stored in the body's space, so walking while drawing is the same as moving the hand |
| Thumbstick click (`TableRecenterInput`; it was A or X until those became grab buttons) | Moves the table root, and the body, tray, handle and window with it | The pen ends its stroke when the body moves under it, and leaves out the closing point, so no line jumps across the body. The handle rests at its place relative to the table, so it goes back to the tray after the table moved |
| Class roster | A text panel with no collider and no interactable, on the opposite side of the table from the student view window | None |

A and X grab, as well as the grip (the toolkit's Select action is bound to all three), and no tool reads the thumbsticks or their clicks.

`ProfessorToolsTogetherTests` uses the tools together and checks that each still works alone afterwards. Not verified on a headset: which button of a real controller or hand pose each action maps to comes from the XR Interaction Toolkit rig, not from DIYsect code.

## Help request (student asks, professor guides the depth)

| Part | Where |
|---|---|
| Enum | `AssistPhase` (None, Requested, Helping) |
| Rule table | `AssistRule.Next(phase, AssistEvent)`, tested in `AssistRuleTests` |
| Writer | the server, `AssistBoard` (body prefab). Events: StudentRequested and StudentCancelled (by Rpc, only from the scalpel's steerer), ProfessorTookControl and ProfessorReleased (from `GuidingHand.ControllerChanged`), SteererLeft (from `ScalpelSteering.SteererChanged` or the requester disconnecting) |
| Change event | `AssistBoard.PhaseChanged`, also raised once on spawn so a late joiner draws the state |
| Presentation | `AssistBeacon` (ring and label on every device), `AssistHaptics`, a line in `ClassRosterPanel`; all derived from the phase |
| Input | `AssistRequestInput`: the Y button (left secondary), student only. B and the left menu button are the quit menu, A and X grab, thumbstick clicks recentre the table |

The student can ask only while holding the scalpel, because the professor can guide the depth only while somebody steers. Putting the scalpel down, leaving, or the professor letting go always closes the request. Set up with Tools > DIYsect > Add Help Request (`AddAssistRequest.ApplyBatch`).
