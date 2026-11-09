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

Later, lower risk: an explicit "cutting" dissection stage, scalpel zone reporting, one organ presentation phase for hover, label, ghost and haptics.

If you are adding a feature with phases, follow the house pattern and add it to this table.
