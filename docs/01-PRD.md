# navigay: Product Requirements Document (PRD)

Doc 1 of 6 · Draft 2 · 22 Nov 2026

The other five docs build on this one:

1. **PRD** (this doc): what we're building and for whom
2. **TRD**: tech, tools and services
3. **App Flow**: every screen and every step
4. **UI/UX Brief**: how it looks and feels
5. **Map Data Schema**: what goes in `ghs.geojson` and `graph.json` (the template calls this "Backend Schema"; navigay has no server)
6. **Implementation Plan**: build order, with a check for each step

---

## 1. Name and tagline

**navigay**

AR walking directions to any door inside GHS.

## 2. Summary

navigay is an iPhone app for walking inside the GHS hostel campus. You pick a place, and the camera view shows dots on the lane in front of you, leading to the next turn, with a countdown in metres. It routes on lanes we draw ourselves, ends at the nearest door you're allowed to reach, and works without internet. When it isn't sure where you are, it shows less instead of guessing.

## 3. Problem

GHS (Good Host Spaces) is the hostel campus of Manipal University Jaipur, across the road from the academic campus. It covers about 22 hectares (55 acres), roughly 540 m by 890 m. Over 5,000 residents live in blocks B1–B11 and the girls' blocks, with messes, restaurants, shops, offices, gates and sports areas joined by lanes between the buildings.

Map apps don't work inside it:

- **Apple Maps** doesn't know the inner lanes. From inside GHS it shows "Proceed to the route" and starts directions on Manipal University Road outside ([screenshot](../UI%20package/05-apple-maps-nav-proceed-to-route-inside-ghs.jpg)).
- **Google Maps** has Street View only on the road outside GHS; the inner lanes have none (checked 22 Nov 2026). Its terms also forbid copying its map into another app.
- **OpenStreetMap** has an older drawing of GHS. It misses newer blocks such as B11 and has no entrances.
- None of them know which door of a building to walk to, or which lanes only boys, only girls or only staff may use.

So people ask someone, or walk around a building looking for the right entrance. New residents in their first weeks feel this most, along with anyone going somewhere they haven't been before.

## 4. What makes navigay different

- **Our own lanes.** Routes follow GHS's real lanes, drawn by us, and end at the nearest door of the place.
- **Directions on the path.** Dots on the lane in front of you, with "in __ m" counting down to each turn.
- **Right side of campus.** It never routes you through a lane you're not allowed to use.
- **Nothing on campus.** No posters, QR codes, beacons or scans.
- **Offline.** Once installed, it needs no internet.
- **Honest.** When the position is poor, it shows less (shorter dots, or only an arrow and text) rather than a confident wrong direction.

## 5. Target users

**Primary: a new GHS resident.** A first-year in their first weeks at GHS. They know the way from their block to the main gate, but not to the warden office, the laundry, or the mess entrance nearest a friend's block. They walk between places several times a day, including after dark, and use an iPhone.

**Secondary: any resident going somewhere new**, such as a restaurant on the other side of campus or the underpass for the first time.

**v1 audience:** the builder plus up to about 10 testers on iPhones, installed from Xcode or TestFlight. Visitors and parents come later.

## 6. Places in scope

Every place people walk to inside GHS, outdoors. Each place has one or more doors. A route ends at the nearest door the user is allowed to reach.

| Type | Places (draft: confirm on a walk) |
|---|---|
| Blocks | B1–B11 (boys); girls' blocks (G1–G4 seen on maps; confirm the rest) |
| Food | BlueDove Mess, Quess Mess; restaurants and shops such as Burger Farm, Amul, Domino's, Nescafe, Belgian Waffle, Tea Post, Dev Sweets and Snacks, Kebab Nation, A One Mart |
| Offices and services | Warden office (one main office), medical centre, laundry, ATMs, security room, guest house |
| Gates | Main gate(s), underpass entrance |
| Sports | Pool, gyms, courts, futsal ground, jogging track |

A mess has entrances on several sides. Each open side is its own door.

## 7. Must-have features (v1)

Feature IDs are used by the other docs.

**F1 · Pick a place.** A list of places grouped by type (Blocks, Food, Offices and services, Gates, Sports). Tapping one shows a card with the place name, walking minutes and a Go button.

**F2 · Access side.** At first launch, the user picks Boys' side or Girls' side. Routes never use lanes that side can't enter, and never use staff-only lanes. The choice can be changed later.

**F3 · Ready check.** Before directions start, the app finds the user's position and places it on the nearest lane, never on a lane on the other side of a block. Directions start only after the position has stayed good for 2 seconds. If it can't start, it says why:

- "You're inside B3. Step outside to start." The position is inside a block outline by more than its error.
- "Step into the open." The position error is worse than about 25 m.
- "Point the camera at the path." Camera tracking is lost.

**F4 · Route.** The shortest walking route on GHS lanes to the nearest allowed door of the place. Computed on the phone in under a second, without internet.

**F5 · Dots on the lane.** In the camera view, dots run along the middle of the lane from the user's feet to the next checkpoint. They get smaller and fade with distance, up to about 40 m. When the position is corrected, the dots slide to their new place over about a second instead of jumping.

**F6 · Checkpoints.** A checkpoint at every turn and at the destination door. Its sign shows the step number, the turn, a landmark and the distance, for example "2/4 · Turn left · B2 · in 30 m". It counts as reached within about 8 m, or once the user has passed it. The phone vibrates, and the dots to the next checkpoint appear.

**F7 · Countdown.** "in __ m" is the distance along the route, not in a straight line, from the user to the next checkpoint. It goes down as they walk, never jumps up because of a GPS correction, and restarts at each checkpoint. It goes up only if the user really walks away from the route.

**F8 · Turn card and trip bar.** At the top: the next turn, for example "Turn left at B2 · 30 m". At the bottom: metres left, minutes left and arrival time, plus an End button.

**F9 · Arrows when the dots aren't visible.**

- Dots off-screen, because the user is facing the wrong way: an arrow at the screen edge points to them.
- Dots hidden at night, during camera trouble, or with the phone lowered: a big flat arrow on screen points to the next checkpoint using the compass and GPS, with the same text.

**F10 · Confidence levels.** The app shows only as much as its position allows. Starting values, tuned on test walks:

| Level | When | What shows |
|---|---|---|
| Full | Position within ~5 m and heading within ~10° | Dots to ~40 m, signs, countdown |
| Short | Position within ~12 m and heading within ~20° | Dots to ~10 m, signs, countdown |
| Hold | Worse than that, too dark, or tracking lost | No dots; flat arrow and text; a prompt to walk a few steps or wave the phone in a figure-eight to fix the compass |

**F11 · Re-route.** If the user is about 20 m off the route after moving, the app computes a new route from where they are, without asking.

**F12 · Arrival.** At the door: a sign with the place name, a vibration, and a summary of the time taken and distance walked.

**F13 · Underpass entrance.** The underpass is a destination like any other. Directions end at its entrance. The app doesn't guide inside or through it.

**F14 · Works offline.** The GHS map ships inside the app. Routing and directions need no internet.

## 8. Nice to have (v2)

- **2D map.** A small map under the camera view, and a full-screen map when the phone is held flat. See `UI package` images 04–09.
- **Step counter.** The camera measures the distance walked, so the countdown is smoother in daylight.
- **Voice prompts** for turns.
- **Search, favourites and recent places.**
- **Map updates without an app update.** The app downloads `graph.json` at launch, so a new block or door needs no new build.
- **Android, App Store and Play Store release.** Public release needs a small server that hands out Niantic tokens.
- **Depth.** Hide dots behind people and objects, using depth sensing on newer phones.
- **Step-free routes** that avoid stairs.
- **Camera scans of specific junctions**, only if test walks show they stay wrong.

## 9. Out of scope (v1 will not do this)

- Indoors: corridors, floors, rooms.
- Guidance inside or through the underpass, or anywhere outside GHS (academic campus, city).
- Driving or cycling directions.
- Posters, QR codes, beacons or any hardware on campus.
- Camera scans of places (Niantic VPS sites).
- Google or Apple map data, imagery or routing inside the app.
- Accounts, login, servers or analytics.
- Live info such as mess menus, crowd levels or opening hours.
- A web version for visitors.
- Android. It comes later, built from the same Unity project.

## 10. User stories

| As a… | I want to… | So that… | Features |
|---|---|---|---|
| new resident | pick a place from a short list and press Go | I'm walking within seconds | F1, F4 |
| resident | have the route end at the nearest open door of the mess | I don't walk around the building | F4 |
| boys' side resident | get routes that never cross lanes I can't enter | I'm not stopped at a gate | F2 |
| walker | see dots on the lane ahead | I know which lane to take without reading a map | F5 |
| walker | see "in 30 m" count down to the next turn | I know exactly when to turn | F6, F7 |
| walker | feel a vibration at each checkpoint | I know I'm on track without staring at the screen | F6 |
| walker facing the wrong way | see an arrow at the edge of the screen | I can find the dots again | F9 |
| person standing inside a block | be told to step outside | I'm never shown a wrong route | F3 |
| person walking at night | get an arrow and distance even when the camera can't see well | I still get there | F9, F10 |
| person who takes a wrong turn | have the app re-route on its own | I don't have to start over | F11 |
| person heading to the university | be guided to the underpass entrance | I know where to cross | F13 |
| resident with no mobile data | have navigation keep working | weak signal doesn't stop me | F14 |

## 11. Success metrics

Measured on about 10 real test routes, by day and at night. Targets are starting values.

| Metric | Target | How we measure |
|---|---|---|
| Routes follow real lanes | 10/10 routes; 0 through buildings or off-limits lanes | Walk each route; compare with the ground |
| Arrival without help | 9 of 10 routes reach the right door | Tester walks using only the app |
| Error at turns, day | Median ≤ 3 m, worst ≤ 8 m | At each turn, metres between where the app says "turn" and the real junction |
| Error at turns, night | Worst ≤ 12 m | Same, after dark |
| Wrong confident direction | 0 times Full-level dots point into the wrong lane | Screen recording of each walk |
| Countdown never jumps up while walking forward | 0 times | App log of the countdown |
| Time to first directions, outdoors by day | ≤ 20 s from opening the app | Stopwatch |
| Re-route speed | New route within 10 s of being ~20 m off | App log |
| Battery | Recorded per 10-minute walk; investigate if above 15% | iPhone battery level before and after |
| Crashes | 0 across all test walks | Xcode logs |

## 12. Constraints and assumptions

- **GPS is rough between blocks.** In Niantic's benchmark, raw phone GPS was about 5 m off typically and up to ~16 m; Niantic's on-phone mix of GPS, compass and camera was about 4 m typically and up to ~11 m. GHS's narrow lanes may be worse. We measure on walks.
- **Lane snap removes the sideways error.** The error along the lane remains until the next checkpoint. Expected about 2–3 m after the first turn, to be measured.
- **Night is harder.** Camera tracking is weaker in the dark, so the app falls back to plain GPS and the flat arrow more often.
- **The map is only as good as our drawing.** Satellite photos can be shifted by metres. Door positions are fixed on foot by averaging GPS for about 30 seconds at each door, twice.
- **GHS is flat.** Dots sit on the ground the camera detects, or about 1.5 m below the phone if no ground is found.
- **Boys' and girls' areas are separate.** Lanes need access tags, and the user picks a side (F2).
- **Niantic free tier.** A developer token is enough for v1 testers. The on-phone position mix uses no cloud credits. Whether it works fully offline after sign-in is checked during setup.
- **iPhone only for v1, tested on an iPhone 17.** It's the base model, with no LiDAR, so it finds the ground from the camera image. That takes a moment longer than on Pro models, especially on plain concrete or at night.

## 13. Risks and what we do about them

- **Position too jumpy in narrow lanes.** Lane snap, confidence levels and a reset at each checkpoint. Measure first; add the step counter (v2) or scans only where walks show a need.
- **Snapping to the wrong parallel lane.** Never snap to a lane with a block outline in between; prefer the lane the route is on.
- **Missing lane or door in the map.** The build script reports gaps; test walks catch the rest; fix the file and rebuild.
- **Camera can't track at night.** Flat arrow and text keep working on plain GPS.
- **Heat and battery in the Jaipur sun.** Lowering the phone switches to the flat arrow; measure battery on walks.
- **Niantic SDK changes** (it changed once already). All position code sits in one place (`WhereAmI`), so it can be swapped.
- **Privacy.** No accounts and no data stored by us. The user's position stays on the phone, apart from any telemetry the Niantic SDK sends.

## 14. Open decisions

| # | Decision | Status |
|---|---|---|
| D1 | v1 on iPhone only, or iPhone and Android? | **Locked:** iPhone only. Android is a later build from the same Unity project |
| D2 | Which iPhone will testers use? | **Locked:** iPhone 17 (base model, no LiDAR) |
| D3 | One warden office, or one per block? | **Locked:** one main warden office |
| D4 | First-launch access choice | **Locked:** two options, Boys' side and Girls' side. No staff option |
| D5 | Final list of places for v1 | Open: builder provides it after a walk |

## 15. Words used in these docs

| Word | Meaning |
|---|---|
| Lane | A walkable path between buildings, drawn as a line along its middle |
| Junction | A point where lanes meet; a checkpoint can sit here |
| Door | A point where a place can be entered |
| Place | A destination (block, mess, office…) with one or more doors |
| Block outline | The shape of a building, used for "step outside" and snapping |
| Checkpoint | A turn or the destination door along the route |
| Lane snap | Placing the user's position on the middle of the lane they're on |
| Countdown | "in __ m": distance along the route to the next checkpoint |
| Confidence level | Full, Short or Hold: how much the app shows, based on position accuracy |
| Edge arrow | Arrow at the screen edge when the dots are off-screen |
| Flat arrow | Big on-screen arrow used when the dots are hidden |
| `ghs.geojson` | The map file we draw in geojson.io |
| `graph.json` | The route file the build script makes from `ghs.geojson` |
| `WhereAmI` | The one part of the app that works out where the user is |
