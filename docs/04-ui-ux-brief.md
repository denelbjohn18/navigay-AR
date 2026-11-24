# navigay: UI/UX Brief

Doc 4 of 6 · Draft 4 · 24 Nov 2026

This is how v1 looks. The screens, the words, and the order are already final in the [App Flow](03-app-flow.md) and in `UI package/App flow screens`. This doc does not add a screen. It gives those six frames the dark, glassy look of Apple Maps. App Flow wins if a word or button disagrees with an older draft.

Features are in the [PRD](01-PRD.md). The camera stack is in the [TRD](02-TRD.md), section 9. Build order: [Implementation Plan](06-implementation-plan.md).

The person is walking, often one-handed, in sun or at night. Type stays big. The camera is the background on S4, S5, and S6.

---

## 1. Which picture decides what

| Pictures | Job |
|---|---|
| [all-screens.png](../UI%20package/App%20flow%20screens/all-screens.png) and S1–S6 | The frames. Where each block sits. The words. |
| `UI package` images 05, 06, 08, 09 | The Apple Maps look: dark glass, big white type, one blue pill, three trip numbers |
| `02-store-dots-and-product-label.png` | The blue dots on the ground, and a short label over the path |
| `01-store-3d-arrow-on-floor.png` | Mood only. v1 does **not** draw that 3D floor arrow. Hold uses a flat **2D** on-screen arrow instead (PRD F9, App Flow S5) |
| `03-nav-app-icon-lane-arrow.png` | Mood for a later icon; also the idea of an edge arrow when dots are off-screen |
| `04-apple-maps-3d-view.png` | A 2D map is v2. v1 has no map |

None of these files ship in the app. No Apple or Google map image, tile, pin, or route line goes in the app.

From the Apple Maps shots, take the material and the type. Leave their map, their place names, and their extra actions (Add Stop, Share ETA, Report, Voice, stars).

| Reference | Take this | navigay uses it on |
|---|---|---|
| [05](../UI%20package/05-apple-maps-nav-proceed-to-route-inside-ghs.jpg) | Dark rounded banner at the top, white instruction, icon on the left. Dark capsule at the bottom with three big numbers and small grey labels | S5 turn card and trip bar |
| [06](../UI%20package/06-apple-maps-nav-trip-sheet-half.jpg) | Same bottom numbers, sitting in a soft dark sheet | S5 trip bar |
| [07](../UI%20package/07-apple-maps-nav-trip-sheet-full-end-route.jpg) | The red of End Route | The End pill on the S5 bar. Not a second sheet. [S5](../UI%20package/App%20flow%20screens/S5-guide.png) already has End |
| [08](../UI%20package/08-apple-maps-destination-pin-card.jpg) | A compact dark card, name plus a grey subtitle, X on the right | The density of S3 and S6 |
| [09](../UI%20package/09-apple-maps-destination-sheet-go-button.jpg) | Bottom sheet, grabber, large name, one full-width blue pill | S3 Go, S1 Open Settings, S6 Done |

---

## 2. Material, type, colour

Portrait only. Tap targets are at least 44 pt, including End. Cards sit 16 pt in from the sides and clear the status bar and the home indicator.

Unity has no iOS blur. Paint the glass as `#1C1C1E` at about 92% opacity, with corners as below. A real blur is optional later. The shape and the type do the Apple Maps job without it.

On the iPhone, use the system sans (SF). Do not put an SF font file in the repo. One family: Regular, Semibold, Bold.

| Token | Value | Where |
|---|---|---|
| Screen black | `#000000` | S1, S2 |
| Glass | `#1C1C1E` at 92% | Turn card, trip bar, sheets, pills, rows |
| Nested fill | `#2C2C2E` | The time box on S3, list rows |
| Text | `#FFFFFF` | Names, numbers, buttons |
| Secondary | `#8E8E93` | Group labels, "Nearest entrance", "4 min · 320 m", trip labels |
| Blue | `#0A84FF` | Go, Done, Open Settings, the dots |
| End red | `#FF453A` | End on S5 only |
| Sheet radius | 36 pt | S3 |
| Banner radius | 22 pt | S5 turn card, S6 card |
| Pill radius | half the height | Buttons, Back, status, End |
| Row radius | 14 pt | S2 rows |
| Page title | 34 pt Bold | Where to? |
| Instruction | 20 pt Semibold | Turn line. The metres stay this size |
| Name on a sheet | 22 pt Bold | S3, S6 |
| Body | 17 pt Regular | Rows, button labels |
| Caption | 13 pt Regular | Group labels, subtitles |

The turn word is written (`left`, `right`, `straight`, `arrive`). Colour is not the only cue.

---

## 3. S1 Permissions

Frame: [S1-permissions.png](../UI%20package/App%20flow%20screens/S1-permissions.png).

Black screen. The sentence is 28 pt Bold, white, left aligned, 24 pt from the sides, in the upper third. One sentence at a time, the three lines from the App Flow.

**Open Settings** is the blue pill from image 09: 50 pt tall, 16 pt from the sides, 16 pt above the home indicator, label 17 pt Semibold. No other control.

---

## 4. S2 Place list

Frame: [S2-place-list.png](../UI%20package/App%20flow%20screens/S2-place-list.png).

Black screen, not the camera. **Where to?** is a text field in the page-title slot, placeholder 34 pt Bold. Tapping it opens the keyboard. When the field has text, a clear control sits at the trailing edge, 44 pt target.

Under the field, the suggestions from the App Flow. A group label is the caption style. **Near you** uses that same label. A row is its own rounded rectangle, 52 pt tall, nested fill, name at 17 pt, grey chevron on the right. Gap between rows in a group is 8 pt. Gap before the next group label is 20 pt. Rows scroll under the field. Empty groups are omitted. A typed query hides the group labels and shows matching names only.

The sample names on the wireframe (including Warden office and Pool) show the groups. Live names come from `graph.json`. The wireframe does not yet draw the caret or **Near you**.

---

## 5. S3 Place card

Frame: [S3-place-card.png](../UI%20package/App%20flow%20screens/S3-place-card.png). Look: image 09, shortened.

S2 stays behind, dimmed to about 40% brightness. The sheet rises from the bottom, glass fill, 36 pt top corners, a 36×5 pt grabber centred 8 pt from the top.

Inside, 20 pt padding:

1. Place name, 22 pt Bold. **X** at the top right, 44 pt target, returns to S2.
2. Type, 13 pt, secondary: Block, Food, Office, Gate, or Sports.
3. A nested box, 14 pt radius. Large line **About 6 min** (22 pt Semibold). Under it, **Nearest entrance** (13 pt secondary).
4. **Go**, the same blue pill as S1.

Until the phone has stuck the user to a path, the large line is **Step outside to see the time.** A loose location inside a building does not replace it. Go stays blue and tappable. Once stuck, the large line is the minutes for the route from that point, the same minutes the guide will open with.

---

## 6. S4 Ready

Frame: [S4-ready.png](../UI%20package/App%20flow%20screens/S4-ready.png).

The camera is full screen. Two glass pills:

- **Back**, top left, under the status bar. Height 36 pt. White chevron and the word Back. Returns to S3.
- The status line, centred, vertically around the middle. Height 44 pt. Horizontal padding 16 pt. The words are the pills listed on the S4 picture, one at a time, 17 pt Semibold.

The Immersal logo slot is the bottom-left box on the wireframe. Section 10.

---

## 7. S5 Guide

Frame: [S5-guide.png](../UI%20package/App%20flow%20screens/S5-guide.png). This drawing is Daytime Full. Look: the top banner and the bottom numbers in image 05.

**Turn card.** Glass banner, 16 pt from the sides, under the status bar, about 64 pt tall, 22 pt radius. On the left, a 36 pt circle with a white turn arrow (left, right, straight, or arrive). On the right, one line: `Turn left at B2 · 30 m`.

**Dots.** From image 02: flat blue circles on the lane centre, about 1.5 m apart, from the feet to the next checkpoint. The near dot is about 0.4 m across. Far dots shrink and fade. Full shows about 40 m. Short shows about 10 m. At most about 30 dots. Colour `#0A84FF`.

If the ground plane is not ready yet, the dots sit 1.5 m below the camera, then drop onto the plane when ARKit finds it. The iPhone 17 has no LiDAR, so the first moment on plain concrete uses that fallback.

**Checkpoint sign.** A small glass pill in the world, facing the camera, 15 pt Semibold: `2/4 · Turn left · B2 · in 30 m`. A 2 pt white line drops from the pill to the trail, as on the wireframe.

**Trip bar.** Glass capsule, 16 pt from the sides, 8 pt above the home indicator, about 64 pt tall. Three columns, number 22 pt Semibold, label 11 pt secondary:

| 320 | 4 | 9:45 |
| metres left | minutes left | arrival |

**End** sits inside the bar, on the right. A 36 pt red pill, white **End**, 15 pt Semibold. The tap target is 44 pt. It returns to S2. Image 07's full red button and its extra sheet are not this screen.

**Hold** is this same frame with the dots and the sign hidden. The turn card and the trip bar stay. A flat **2D** on-screen arrow (white, large, centred in the lower third, clear of the logo and the trip bar) points to the next checkpoint. Not the 3D store arrow. Night, a weak camera, and a lost camera use this frame. They do not go back to Ready. Dots return only when the camera is fine and the level is Short or Full. Short is this frame with the shorter trail. **Edge arrow:** a small chevron on the screen edge in Full/Short when the trail is off-screen; hidden in Hold.

On Hold the turn card may add one sentence, from the App Flow: **Walk a few steps.** when the position is the weak part, or **Wave the phone in a figure-eight.** when the position is already within about 12 m and the heading is the weak part. A dark camera adds no sentence.

---

## 8. S6 Arrival

Frame: [S6-arrival.png](../UI%20package/App%20flow%20screens/S6-arrival.png).

A glass card in the turn-card slot, 22 pt radius, 16 pt padding.

1. Place name, 22 pt Bold.
2. `4 min · 320 m`, 15 pt, secondary.
3. For the underpass only, a third line: `This is the entrance. navigay stops here.`
4. **Done**, the blue pill, full width of the card. Returns to S2.

The camera stays up behind the card. The logo stays bottom left. The trip bar is gone.

---

## 9. Motion

Sheets ease up in about 0.35 s. Pills swap text in place. They do not bounce.

When the position corrects, the dots and the sign slide over about 1 second. They do not jump. The countdown number does not animate upward on a noisy GPS fix.

A checkpoint and the arrival each vibrate once. Re-route does not. End and Done do not.

---

## 10. Immersal logo

The box labelled "Immersal logo" on S4, S5, and S6 is the slot, not the art. Use Immersal's real white logo from the SDK, unmodified. Bottom left, 16 pt from the side. On S5 it sits 12 pt above the trip bar. On S4 and S6 it sits 16 pt above the home indicator.

At least 30% transparency, as their watermark rule allows. Nothing else enters that rectangle: no dots, no sign, no card, no pill.

---

## 11. Icon

v1 may keep Unity's default icon. A later icon is a black rounded square, one blue lane, one white arrow. Image 03 is the mood. Its speed badges and street name are not the icon.

---

## 12. Not on these screens

- A map, a puck, or a compass. Image 04 waits for v2.
- A second search on the camera screens. Search is the **Where to?** field on S2.
- A microphone, a star, share, add stop, voice.
- Boys, Girls, Staff, or an access tag.
- A door ID, or a line that promises a metres-accurate fix.
- A redraw of the Immersal logo.
- Image 07's End Route sheet. End is the red pill already on S5.
