# navigay: Product Requirements Document (PRD)

Doc 1 of 6 · Draft 6 · 24 Nov 2026

The other five docs build on this one:

1. **PRD** (this doc): what we're building and for whom
2. **[TRD](02-TRD.md)**: tech, tools and services
3. **[App Flow](03-app-flow.md)**: every screen and every step (screens and copy win over older drafts)
4. **[UI/UX Brief](04-ui-ux-brief.md)**: how it looks and feels
5. **[Map Data Schema](05-map-data-schema.md)**: what goes in `ghs.geojson`, the places sheet and `graph.json` (the template calls this "Backend Schema"; navigay has no server)
6. **[Implementation Plan](06-implementation-plan.md)**: build order, with a check for each step

**If two docs disagree:** screens and on-screen words → App Flow + UI Brief. Map files and IDs → Map Data Schema. Stack and folders → TRD. Build order and who does what → Implementation Plan. Product must-haves → this PRD.

**Build order note:** the first task is a walk between **two places** in GHS. Map the rest of the campus after that slice works, one piece at a time. Details: [Implementation Plan](06-implementation-plan.md) §7.

**Walk model (locked 24 Nov 2026):** a walk starts where the user is standing, once the phone has **stuck** them to a path and they are outside. The map zero and the connectivity root are not that point. The user taps **Where to?**, picks a place near that stuck point or searches for one, and the route runs from that point. The route is calculated when Go is pressed, before Ready waits, then settles once onto the stuck point before the guide. If the place was already ranked from that stuck point, Go keeps the stored route. It is calculated again only after they leave the route. Night does not block Ready. On the guide, a weak or dark camera uses the flat arrow. Dots wait until the camera is fine.

---

## 1. Name and tagline

**navigay**

AR walking directions to any door inside GHS.

## 2. Summary

navigay is an iPhone app for walking inside the GHS hostel campus. You pick a place, and the camera view shows dots on the lane in front of you, leading to the next turn, with a countdown in metres. It routes on lanes we draw ourselves, across the whole campus, and ends at the nearest door. It knows where you are from the phone's GPS, compass and camera, and from photo maps we take at chosen junctions. Everything runs on the phone, costs $0 and works without internet. When it isn't sure where you are, it shows less instead of guessing.

## 3. Problem

GHS (Good Host Spaces) is the hostel campus of Manipal University Jaipur, across the road from the academic campus. It covers about 22 hectares (55 acres), roughly 540 m by 890 m. Over 5,000 residents live in blocks B1–B11 and the girls' blocks, with messes, restaurants, shops, offices, gates and sports areas joined by lanes between the buildings.

Map apps don't work inside it:

- **Apple Maps** doesn't know the inner lanes. From inside GHS it shows "Proceed to the route" and starts directions on Manipal University Road outside ([screenshot](../UI%20package/05-apple-maps-nav-proceed-to-route-inside-ghs.jpg)).
- **Google Maps** has Street View only on the road outside GHS; the inner lanes have none (checked 22 Nov 2026). Its terms also forbid copying its map into another app.
- **OpenStreetMap** has an older drawing of GHS. It misses newer blocks such as B11 and has no entrances.
- None of them know which door of a building to walk to.

So people ask someone, or walk around a building looking for the right entrance. New residents in their first weeks feel this most, along with anyone going somewhere they haven't been before.

## 4. What makes navigay different

- **Our own lanes.** Routes follow GHS's real lanes, drawn by us, and end at the nearest door of the place.
- **Directions on the path.** Dots on the lane in front of you, with "in __ m" counting down to each turn.
- **The whole campus.** v1 routes through every GHS lane and lists every place. Boys, girls and staff tags are stored for v2 and are not used yet.
- **Nothing installed on campus.** No posters, QR codes or beacons. The only setup is photos we take with a phone at chosen junctions.
- **$0 and offline.** No server, no account, no paid service. Once installed, it needs no internet.
- **Honest.** When the position is poor, it shows less (shorter dots, or only an arrow and text) rather than a confident wrong direction.

## 5. Target users

**Primary: a new GHS resident.** A first-year in their first weeks at GHS. They know the way from their block to the main gate, but not to the warden office, the laundry, or the mess entrance nearest a friend's block. They walk between places several times a day, including after dark, and use an iPhone.

**Secondary: any resident going somewhere new**, such as a restaurant on the other side of campus or the underpass for the first time.

**v1 audience:** the builder plus a few friends, installed from the builder's Mac with Xcode and a free Apple ID (the app must be reinstalled every 7 days). TestFlight needs the $99/year Apple Developer Program, so it's optional. Visitors and parents come later.

## 6. Places in scope

Every place people walk to inside GHS, outdoors. Each place has one or more doors. A route ends at the nearest door. The live list is whatever is in `map/places.csv` / `graph.json` that day.

**In the file on 24 Nov 2026:** B1–B3, B5–B10, G2–G4, Quess Mess, BlueDove Mess, Underpass entrance. Doors are still draft until the survey walk. Main gate, Warden office, Pool, B4, B11, G1, and the restaurants are target places; add them when the walk and drawing catch up. See the [Implementation Plan](06-implementation-plan.md).

| Type | Target places (add when drawn and surveyed) |
|---|---|
| Blocks | B1–B11 (boys); girls' blocks (G1–G4 seen on maps; confirm the rest) |
| Food | BlueDove Mess, Quess Mess; restaurants and shops such as Burger Farm, Amul, Domino's, Nescafe, Belgian Waffle, Tea Post, Dev Sweets and Snacks, Kebab Nation, A One Mart |
| Offices and services | Warden office (one main office), medical centre, laundry, ATMs, security room, guest house |
| Gates | Main gate, underpass entrance |
| Sports | Pool, gyms, courts, futsal ground, jogging track |

A mess has entrances on several sides. Each open side is its own door.

In v1, a new restaurant means a new row in the places sheet, a new door point on the map, a rebuild and a new app install.

## 7. Must-have features (v1)

Feature IDs are used by the other docs.

**F1 · Pick a place.** Home is a search field reading **Where to?**. Tapping it shows suggestions. When the phone has stuck the user to a path, those suggestions are **Near you**: every place, nearest walking route from that stuck point first. A loose location inside a building does not count as near, and it does not show minutes. When the user is not stuck, the suggestions are the type groups (Blocks, Food, Offices and services, Gates, Sports), and empty groups are omitted. Typing searches the whole list by name. Tapping a row opens a place card: name, type, about-minutes (or **Step outside to see the time.** until stuck), **Nearest entrance**, and **Go**. The list can be browsed and searched with no camera permission. Location is asked when the field is focused, so Near you can work. Camera is asked when Go needs it (App Flow S1). If the user is stuck, the card's minutes are the route already used to rank that place, and Go keeps that same route.

**F3 · Ready check.** Go opens the camera Ready screen. The route is already being calculated (F4). Ready does not wait for the camera. Directions start once the user is **stuck**: locked to the centre of one lane, outside every building, reported position error about 25 m or better, and that lock held for about 2 seconds. The app never snaps across a block outline. About 25 m is only the bar for leaving Ready. It is not the bar for dots (F10). A dark, weak, or lost camera still leaves Ready. Back returns to the place card. Pills include **Finding you…**, the inside-block lines, **Step into the open.** when the phone cannot lock a lane, and **Can't find a way there.** if the router has no path. Precise Location off uses the permissions screen (reported accuracy worse than about 1,000 m).

**F4 · Route.** The shortest walking route on GHS lanes from where the user is standing to the nearest door of the place. Not from the map zero, and not from the connectivity root. Every lane is usable, whatever its access tag. Computed on the phone in under a second, without internet.

Calculated once when Go is pressed, before Ready waits for the stuck lock or the camera. The start is a legal lane snap of the position at that press (line of sight, not across a building). If no location fix has arrived yet, calculate once on the first fix during Ready. If the position cannot be snapped, Ready shows the inside or open line and calculates once when the first legal snap exists. It does not calculate on a timer. Ready can say **Can't find a way there.** as soon as that calculation fails. The guide does not calculate the first route.

When the stuck lock completes, the route settles once. If the stuck point is already on the route, keep it. If it is not, replace the route once from that stuck point, still on Ready, before the guide. The place card and the guide then share the minutes from that point. The countdown updates as the user walks. The router does not run again every moment.

**F5 · Dots on the lane.** In the camera view, dots run along the middle of the lane from the user's feet to the next checkpoint. They get smaller and fade with distance, up to about 40 m. When the position is corrected, the dots slide to their new place over about a second instead of jumping.

**F6 · Checkpoints.** A checkpoint at every turn and at the destination door. Its sign shows the step number, the turn, a landmark and the distance, for example "2/4 · Turn left · B2 · in 30 m". It counts as reached within about 8 m, or once the user has passed it. The phone vibrates, and the dots to the next checkpoint appear.

**F7 · Countdown.** "in __ m" is the distance along the route, not in a straight line, from the user to the next checkpoint. It goes down as they walk, never jumps up because of a GPS correction, and restarts at each checkpoint. It goes up only if the user really walks away from the route.

**F8 · Turn card and trip bar.** At the top: the next turn, for example "Turn left at B2 · 30 m". At the bottom: metres left, minutes left and arrival time, plus **End**. End returns straight to the place list. There is no confirm sheet. Minutes are the remaining length of this same route, from this same stuck point, at the same walking speed. The place card and the guide therefore open on one answer. Until a stuck point exists, the card shows **Step outside to see the time.**

**F9 · Arrows when the dots aren't visible.**

- Dots off-screen, because the user is facing the wrong way: an **edge arrow** on the screen edge points to them (Full and Short only).
- Dots hidden in Hold (night, camera trouble, phone lowered, weak fix): a **flat 2D on-screen arrow** points to the next checkpoint using the compass and GPS. Turn card and trip bar stay. This is not the 3D floor arrow in the store reference image.

**F10 · Confidence levels.** The app shows only as much as its position allows. "Error" means the error we actually have: the GPS and compass error the phone reports, or a recent photo-map match (section 8). Starting values, tuned on test walks:

| Level | When | What shows |
|---|---|---|
| Full | Camera is fine, and either a photo map matched in the last few seconds, or position within ~5 m and heading within ~10° | Dots to ~40 m, signs, countdown |
| Short | Camera is fine, position within ~12 m, and heading within ~20° | Dots to ~10 m, signs, countdown |
| Hold | Worse than that, too dark, tracking lost, or the phone is lowered | No dots. Flat arrow and text. Dots wait until the camera is fine and the position reaches Short or Full |

A fine camera means tracking is normal, the scene is not too dark, and the phone is not pointed more than about 60° below the horizon. On Hold, the turn card may add one sentence. If the position error is the weak part: **Walk a few steps.** If the position is already within about 12 m and the heading is the weak part: **Wave the phone in a figure-eight.** A dark camera does not add a sentence and does not send the user back to Ready.

**F11 · Re-route.** After the guide is open, this is the only time a route is calculated again. If the user has left the route (about 20 m off it, after moving at least about 10 m since the last route), the app computes a new route from where they are, without asking. It does not calculate every moment, and it does not calculate again just because the guide opened.

**F12 · Arrival.** At the door: a vibration and a card with the place name, time taken, distance walked, and **Done** (back to the list).

**F13 · Underpass entrance.** The underpass is a destination like any other. Directions end at its entrance. The arrival card adds: `This is the entrance. navigay stops here.` The app does not guide inside or through it.

**F14 · Works offline.** The GHS map, the router and the photo maps all ship inside the app. Routing, position and directions need no internet.

**F15 · Immersal logo.** Immersal's own logo is always visible on the camera screen, bottom-left, above the bottom bar. It never covers the dots or the turn card. This is a condition of Immersal's license (section 8).

## 8. How the app knows where you are

The precise point that matters is the **stuck point**: where the user is on a GHS lane, once they are outside. The route, the minutes, and the guide all use that point. A raw GPS dot is not it. A dot inside a building is not it.

Best precision, within the v1 rules (nothing installed on campus, $0, offline):

1. **Photo map**, when the camera is looking at a mapped corner. This is the tightest fix, and it can put the user on the correct one of two close lanes.
2. **Lane snap** of GPS, compass, and camera motion, with line of sight, held for about 2 seconds. This is the stuck point. It removes the sideways error. It does not need a working camera to count as stuck. Reported error about 25 m or better is the bar for trusting that lock. Short (~12 m) and Full (~5 m) are tighter bars, used only to decide whether dots may be drawn.
3. **Raw GPS** is an input to the snap. It is never the start of a route, the minutes, or the dots.

The user can stand anywhere in GHS. They pick a place. The waypoint is the route from the stuck point to the nearest door, shown as dots when the camera is fine, or as the flat arrow when it is not. The map zero in `origin.json` is only the corner of the metre grid. It is not where the walk begins.

Two layers. Both cost $0 and run on the phone without internet. All position code sits in one part of the app, `WhereAmI`, so the source can be swapped later without touching the rest.

### Layer 1: phone sensors (always on)

GPS, compass and ARKit camera tracking. Camera tracking fills the gaps between GPS updates. The result is snapped to the middle of the nearest lane, but never to a lane with a block outline between the user and that lane.

What it can't do:

- It can't tell two close parallel lanes apart, so it can put the user on the wrong side of a block.
- Along a lane it's only as good as GPS, so "Turn in 5 m" can fire early or late.
- After dark, camera tracking is weak. Ready still opens the guide once the user is stuck and outside. The flat arrow and the distance work. Dots wait until the camera is fine.

### Layer 2: photo maps at chosen spots (Immersal Free)

We photograph chosen junctions with the Immersal Mapper app. Each photo map ships inside the app. When the camera sees a mapped spot, the phone matches the image to the map and puts the user on the correct lane, offline.

What to know:

- One free map is up to 100 photos: about one courtyard or a short stretch of lane. GHS needs many small maps, so we place them where layer 1 fails most: parallel lanes, turns that fire early or late, mess fronts and gates. Test walks decide where.
- GPS chooses which maps to load.
- Where two maps meet, the dot can jump a few metres. It slides over about a second (F5).
- A map shot in the afternoon can fail at night, or after the scene changes. The app then drops back to layer 1 and the flat arrow.
- A match is only as accurate as the map's placement on our lane drawing.
- The free license requires Immersal's logo on the camera screen the whole time (F15). We use Immersal's real logo files from the SDK, unmodified. Immersal's logo instructions say the logo is required on every plan unless Immersal agrees otherwise.

### No accuracy promise

We don't quote a metre figure until we've measured metres off at each turn on real walks (section 12). Other companies' benchmark numbers are not our numbers.

### Upgrade paths (not v1)

- **Immersal Pro:** $99/month, a billing upgrade in the same account. Up to 500 photos per map, commercial use, the same plugin and the same maps, so no rescan.
- **One joined map of the whole campus:** Immersal Enterprise, which is a sales contract.
- **Switching to Niantic:** means scanning GHS again and rewriting only `WhereAmI`.
- **License check:** one Immersal page says the free plan allows commercial use; another says it doesn't. Personal use is fine. Re-read the license before any public release.

### Ruled out for v1

- **Niantic VPS2 precise mode.** It needs a scanned map of GHS and internet. Each position query costs 12 credits, and the free plan's ~1,500 queries is roughly 2 hours of walking. Niantic promises no accuracy number; its precise mode is only as good as the scanned map's alignment to the satellite photo, and a small rotation error grows with distance.
- **ARCore Geospatial**, used by other campus apps. It needs Street View, and GHS's inner lanes have none.
- **ARKit saved world maps.** They drift on long blocks that look alike and fail when the light changes.
- **Apple location anchors.** They may work on Manipal University Marg, but likely stop inside the gates, because Apple's imagery comes from streets a car can drive. A quick availability check at the gate and between B2 and B3 is worth doing later, as a bonus, not as the campus system.

## 9. Nice to have (v2)

- **Access filter (F2).** The app asks Boys, Girls, or Staff. Boys walks lanes tagged `all` or `boys` and sees those places. Girls walks `all` or `girls`. Staff walks every lane and sees every place. The IDs and tags are already in the map files. v1 does not ask and does not filter.
- **2D map.** A small map under the camera view, and a full-screen map when the phone is held flat. See `UI package` images 04–09.
- **Step counter.** The camera measures the distance walked, so the countdown is smoother in daylight.
- **Voice prompts** for turns.
- **Favourites and favourites sync.** Search is already in v1 (F1).
- **Map updates without an app update.** The app downloads `graph.json` at launch from a static host such as Cloudflare R2, so a new block or door needs no new build.
- **Accounts or live edits from a phone**, only if needed. This is the only reason to add a server and database. Routing and the map stay on the phone even then.
- **Android**, built from the same Unity project.
- **Depth.** Hide dots behind people and objects, using depth sensing on newer phones.
- **Step-free routes** that avoid stairs.

## 10. Out of scope (v1 will not do this)

- Indoors: corridors, floors, rooms.
- Guidance inside or through the underpass, or anywhere outside GHS (academic campus, city).
- Driving or cycling directions.
- Posters, QR codes, beacons or any hardware on campus.
- Niantic, ARCore Geospatial, or any positioning that costs money or needs internet.
- Google or Apple map data, imagery or routing inside the app.
- Accounts, login, servers, databases or analytics.
- Live info such as mess menus, crowd levels or opening hours.
- A web version for visitors.
- Filtering routes or the place list by boys, girls, or staff. The tags are stored. The filter is F2 in v2.
- Android.

## 11. User stories

| As a… | I want to… | So that… | Features |
|---|---|---|---|
| new resident | tap Where to?, pick a nearby place or search for one, and press Go | I'm walking within seconds | F1, F4 |
| resident | have the route end at the nearest open door of the mess | I don't walk around the building | F4 |
| resident | be guided across the whole campus, including the other side | the app doesn't stop me at a boys, girls, or staff lane | F4 |
| walker | see dots on the lane ahead | I know which lane to take without reading a map | F5 |
| walker | see "in 30 m" count down to the next turn | I know exactly when to turn | F6, F7 |
| walker | feel a vibration at each checkpoint | I know I'm on track without staring at the screen | F6 |
| walker facing the wrong way | see an arrow at the edge of the screen | I can find the dots again | F9 |
| walker at a junction between parallel lanes | be put on the right lane | I don't end up on the wrong side of a block | Section 8, layer 2 |
| person standing inside a block | be told to step outside | I'm never shown a wrong route | F3 |
| person walking at night | get an arrow and distance even when the camera can't see well | I still get there | F9, F10 |
| person who takes a wrong turn | have the app re-route on its own | I don't have to start over | F11 |
| person heading to the university | be guided to the underpass entrance | I know where to cross | F13 |
| resident with no mobile data | have navigation keep working | weak signal doesn't stop me | F14 |

## 12. Success metrics

Measured on about 10 real test routes, by day and at night. Accuracy targets are goals, not promises; they get firm numbers only after the first walks.

| Metric | Target | How we measure |
|---|---|---|
| Routes follow real lanes | 10/10 routes; 0 through buildings | Walk each route; compare with the ground |
| Arrival without help | 9 of 10 routes reach the right door | Tester walks using only the app |
| Metres off at turns with a photo map, day | Goal: median ≤ 3 m | At each turn, metres between where the app says "turn" and the real junction |
| Metres off at turns without a photo map | Recorded; the worst turns get photo maps | Same |
| Metres off at turns, night | Recorded; flat arrow still points the right way | Same, after dark |
| Wrong side of a block | 0 at photo-mapped spots; recorded elsewhere | Screen recording of each walk |
| Photo-map match, day | Matches at 8 of 10 tries at a mapped spot | App log |
| Photo-map match, night | Recorded | App log |
| Wrong confident direction | 0 times Full-level dots point into the wrong lane | Screen recording |
| Countdown never jumps up while walking forward | 0 times | App log of the countdown |
| Time to first directions, outdoors by day | ≤ 20 s from opening the app | Stopwatch |
| Re-route speed | New route within 10 s of being ~20 m off | App log |
| Immersal logo | Visible on every camera screen; never covers dots or turn card | Screenshots |
| Battery | Recorded per 10-minute walk; investigate if above 15% | iPhone battery level before and after |
| Crashes | 0 across all test walks | Xcode logs |
| Running cost | $0 per month | Accounts used |

## 13. Constraints and assumptions

- **$0.** No paid service, server or subscription in v1.
- **No backend.** The app is C# in Unity. Python runs only on the builder's Mac, to join the lane drawing and the places sheet into `graph.json`. No accounts, server or database.
- **GPS is rough between blocks** and can jump. We don't assume a number; we measure.
- **Lane snap removes the sideways error, not the error along the lane**, and can pick the wrong one of two close parallel lanes. Photo maps fix this where they exist.
- **Night is harder.** Camera tracking and photo-map matching are weaker in the dark. The user still leaves Ready once stuck and outside. The guide uses GPS, the compass, and the flat arrow until the camera is fine.
- **The map is only as good as our drawing.** Satellite photos can be shifted by metres. Door positions are fixed on foot by averaging GPS for about 30 seconds at each door, twice. Photo maps must be placed carefully on the same drawing.
- **GHS is flat.** Dots sit on the ground the camera detects, or about 1.5 m below the phone if no ground is found.
- **Access tags are data for v2.** Every lane and place keeps an `access` value: `all`, `boys`, `girls`, or `staff`. v1 routes through the whole campus and shows every place. F2 is the v2 filter.
- **iPhone only for v1, tested on an iPhone 17.** It's the base model, with no LiDAR, so it finds the ground from the camera image. That takes a moment longer than on Pro models, especially on plain concrete or at night.
- **Unity version follows Immersal**, not Niantic. Immersal SDK 2.5.0 supports Unity 2022.3 and later and is tested with Unity 6.3 LTS. The exact version is set in the TRD.

## 14. Risks and what we do about them

- **Position too jumpy in narrow lanes.** Lane snap, confidence levels and a reset at each checkpoint. Add photo maps where walks show the worst errors.
- **Snapping to the wrong parallel lane.** Never snap to a lane with a block outline in between; prefer the lane the route is on; add a photo map at that spot.
- **Photo map fails at night or after changes.** Drop to layer 1 and the flat arrow; reshoot the map if the place changed.
- **Too many photo maps to manage.** Only map spots where walks show a need; keep a list of which map covers which junction.
- **Missing lane or door in the map.** The build script fails on missing doors, IDs or tags; test walks catch the rest; fix the files and rebuild.
- **Heat and battery in the Jaipur sun.** Lowering the phone switches to the flat arrow; photo-map matching runs only near mapped spots; measure battery on walks.
- **Immersal license or SDK changes.** Personal use only until the license is re-read; all position code sits in `WhereAmI`.
- **Privacy.** No accounts and no data stored by us. Position and photo-map matching stay on the phone. Check during setup whether the Immersal SDK or Unity sends anything over the network, and turn it off.

## 15. Decisions

| # | Decision | Status |
|---|---|---|
| D1 | v1 on iPhone only, or iPhone and Android? | **Locked:** iPhone only. Android is a later build from the same Unity project |
| D2 | Which iPhone will testers use? | **Locked:** iPhone 17 (base model, no LiDAR) |
| D3 | One warden office, or one per block? | **Locked:** one main warden office |
| D4 | Access filter | **Locked:** v1 routes every lane and lists every place. IDs and access tags stay in the files. v2 asks Boys, Girls, or Staff and filters with those tags (F2) |
| D5 | Final list of places for v1 | Open: builder writes it after one walk |
| D6 | Position source | **Locked:** phone sensors everywhere, plus Immersal Free photo maps at chosen spots. No Niantic in v1 |
| D7 | Exact Unity version | **Locked:** 6000.3.29f1. Do not upgrade mid-project. The Implementation Plan uses this same number |
| D10 | When Ready ends | **Locked:** when the user is stuck to a path and outside. The camera does not have to be working. Dots wait for a fine camera on the guide |
| D11 | Where a walk starts | **Locked:** the stuck point where the user is standing. Not the map zero. Not the connectivity root (`start=yes`) |
| D12 | When the route is calculated | **Locked:** at Go, before Ready waits. If there is no fix yet, once, on the first fix or the first legal snap during Ready. Not on the guide. Again only after the user has left the route |
| D13 | Minutes on the place card | **Locked:** **Step outside to see the time.** until stuck. The card and the guide share that stuck point |
| D14 | What origin is | **Locked:** the map zero, pinned to the main gate door once that door is surveyed, then never moved. Users may start a walk anywhere in GHS |
| D15 | First task | **Locked:** AR navigation between two places. Then map the rest of GHS in loops |
| D16 | How a place is chosen | **Locked:** **Where to?** is a search field. Suggestions are places near the stuck point. Typing searches the rest. Type groups show when the user is not stuck yet |
| D8 | Is Apple geotracking available at the GHS gate? | Open: small availability test later; bonus only |
| D9 | Accuracy numbers | Open until measured on real walks |

## 16. Words used in these docs

| Word | Meaning |
|---|---|
| Lane | A walkable path between buildings, drawn as a line along its middle |
| Junction | A point where lanes meet; a checkpoint can sit here |
| Door | A point where a place can be entered |
| Place | A destination (block, mess, office…) with one or more doors |
| Block outline | The shape of a building, used for "step outside" and snapping |
| Checkpoint | A turn or the destination door along the route |
| Lane snap | Placing the user's position on the middle of the lane they're on |
| Stuck | Locked to one lane centre, outside every building, position error about 25 m or better, held for about 2 seconds. Not a loose GPS point |
| Fine camera | Tracking normal, not too dark, phone not pointed more than about 60° below the horizon. Dots wait for this |
| Map zero | The origin lat/lon that metres are measured from. Pinned to the main gate once surveyed. Not where a walk starts |
| Connectivity root | The place with `start=yes`. The build checks that other doors can be reached from it. Not where a walk starts |
| Countdown | "in __ m": distance along the route to the next checkpoint |
| Confidence level | Full, Short or Hold: how much the app shows, based on the error we have |
| Edge arrow | Arrow at the screen edge when the dots are off-screen |
| Flat arrow | Big on-screen arrow used when the dots are hidden |
| Layer 1 | Position from GPS, compass and camera tracking, snapped to a lane |
| Layer 2 / photo map | An Immersal map of one spot, made from up to 100 photos, stored in the app |
| `ghs.geojson` | The map shapes we draw in geojson.io: lanes, doors, junctions, block outlines |
| Places sheet | The table of place IDs, names, types, access and door IDs. v1 stores `access` and does not filter on it |
| Access tag | `all`, `boys`, `girls`, or `staff` on a lane or place. Used by v2 |
| `graph.json` | The route file the build script makes from `ghs.geojson` and the places sheet |
| `WhereAmI` | The one part of the app that works out where the user is |
