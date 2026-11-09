# Labels and instructions

Every organ label and instruction uses one look (ink-teal cards, Inter type, mint for done) and one motion table.

![Inspection title above the liver, minimal labels on the stomach and kidney](previews/label_design.png)

## Organ labels

| Label | When | Where it sits |
|---|---|---|
| Minimal label (name and body system) | A controller points at an organ | Just above the organ's own top, worked out from its size every frame |
| Callout (leader line, name and short role) | The organ is held | Line from the organ's top to the text |
| Inspection title (eyebrow, large name, function, one fact) | Held or pointed at by the professor or student | Above the organ and the label, pushed to the far side so it never covers the anatomy |

Names and callouts draw on top of the body with a soft dark halo, so they stay readable while the organ is still under the skin. The text lives in `OrganInfoLibrary.asset` (name, body system, short role, function, fact).

## Instructions

The guide card shows one action per step with a line icon for the tool. When a step is done the old line is struck through and shrinks into a mint check while the next one rolls up. The first time this headset holds an organ, a brief "Turn to examine" hint appears beside it and dissolves.

## Timings

All durations are in `Assets/Scripts/Presentation/MotionSpec.cs`: labels 180 to 220 ms, leader line 200 to 280 ms, title 250 to 350 ms, instruction roll 220 to 280 ms, check 250 ms.

## After changing the body or the panel

Run **Tools > DIYsect > Apply Label Design** (fonts, library, line material, and the on-top text shader in the build) and **Tools > DIYsect > Add Dissection Checklist** (the guide card).
