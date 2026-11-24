# navigay: Technical Requirements Document (TRD)

Doc 2 of 6 · Draft 4 · 24 Nov 2026

This doc turns the [PRD](01-PRD.md) into technical choices. Feature IDs (F1–F15) and decisions (D1–D9) refer to the PRD. Screens and on-screen words: [App Flow](03-app-flow.md) and [UI/UX Brief](04-ui-ux-brief.md). Map files: [Map Data Schema](05-map-data-schema.md). Build order: [Implementation Plan](06-implementation-plan.md).

When this doc and an older stack note disagree, this doc wins for tech. When this doc and App Flow disagree on a screen or a button, App Flow wins.

---

## 1. Stack at a glance

| Part | Choice |
|---|---|
| App (frontend) | Unity 6.3 LTS, C#, Universal Render Pipeline (URP), AR Foundation with the Apple ARKit XR Plugin. iPhone only |
| Backend | None in v1 |
| Database | None in v1. All data is files shipped inside the app |
| Auth | None. No accounts |
| Hosting and install | No hosting. Code on GitHub (`denelbjohn18/navigay-AR`). The app is installed from the builder's Mac with Xcode and a free Apple ID |
| Third-party services | Immersal SDK 2.5.0 and the Immersal Mapper app (Free plan, $0), for photo maps. geojson.io (free web editor), for drawing the map |
| Key libraries | AR Foundation, Apple ARKit XR Plugin, Immersal SDK Core, Newtonsoft JSON, TextMeshPro, Unity Test Framework. Python: shapely, pytest |
| Environment variables | None. No secrets in the app or the repo |
| Constraints | $0 per month. Works offline. No Google or Apple map data. No Niantic. No network calls in v1 |

## 2. Languages and where they run

| Language | Where | What it does |
|---|---|---|
| C# | Unity app on the iPhone | Everything the user sees and everything that runs: screens, camera view, dots, position, routing, guidance |
| Python | Builder's Mac only | Joins `ghs.geojson` and `places.csv` into `graph.json`, and checks them for mistakes |
| None other in v1 | | There is no website and no server |

In v2, a server language appears only if we add accounts or live edits. That would be Convex in TypeScript, called from C# over plain HTTP, because Unity has no official Convex SDK. Routing and the map stay on the phone even then.

## 3. Versions and setup

### Tools on the Mac

| Tool | Version | Notes |
|---|---|---|
| Unity Hub + Unity Editor | **6000.3.29f1** (Unity 6.3 LTS, Apple silicon) with iOS Build Support | Chosen 24 Nov 2026. Immersal SDK 2.5.0 supports Unity 2022.3 and later and is tested with Unity 6.3 LTS. Keep this same number in `ProjectSettings/ProjectVersion.txt`. Don't upgrade mid-project. Unity Personal is activated on this Mac |
| Xcode | 27.2 | Installed. The terminal points at this Xcode. The iPhone 17 on iOS 27.0.1 is paired, with Developer Mode on |
| Apple ID | Free | Installs from Xcode last 7 days; then reinstall. TestFlight needs the $99/year Apple Developer Program, so it's optional and not v1 |
| Immersal account | Free plan | Used in the Immersal Mapper app (iPhone) and the developer portal (to download map files). Not needed inside our app |
| Python | 3.12 or later (the Mac has 3.14) | In a virtual environment at `tools/.venv` |

### Unity packages

| Package | Version | Why |
|---|---|---|
| AR Foundation (`com.unity.xr.arfoundation`) | The 6.x version Unity 6.3 offers | Camera, tracking, ground detection |
| Apple ARKit XR Plugin (`com.unity.xr.arkit`) | Same version as AR Foundation | ARKit on iPhone |
| Universal RP | Comes with the template | Immersal's project validator checks for a URP renderer |
| Immersal SDK Core (`com.immersal.core`) | **2.5.0**, added from `https://github.com/immersal/imdk-unity.git#2.5.0` | Photo-map matching on the phone |
| Input System | Comes with Unity 6 | Touch input |
| Newtonsoft JSON | Pulled in by Immersal | Reading `graph.json` |
| TextMeshPro | Part of Unity 6's UI package | Text on screens and signs |
| Unity Test Framework | Comes with Unity | Edit Mode and Play Mode tests |

Immersal also pulls in OpenXR and AI Navigation as dependencies. Leave them installed; we don't use them.

### Project and Player settings

- **Template:** Universal 3D, then add AR Foundation and the Apple ARKit XR Plugin. Turn on ARKit under XR Plug-in Management for iOS.
- **Run Immersal's project validator** after import and fix what it reports.
- **Platform:** iOS. Scripting backend IL2CPP, ARM64.
- **Minimum iOS version:** 17.0.
- **Orientation:** portrait only.
- **Requires ARKit:** on, so the app installs only on ARKit iPhones.
- **Info.plist strings** (same sentences as App Flow S1):
  - Camera: "navigay uses the camera to show directions on the path."
  - Location when in use: "navigay uses your location to guide you around GHS."
- **Permissions timing:** S2 can be browsed and searched with no permission. Focusing **Where to?** requests location when it is missing, so **Near you** can rank places once the user is stuck. Camera is requested when Go is pressed and it is missing. The first ask is the iOS dialog. S1 is the screen after a denial, and the screen for Precise Location off (reported accuracy worse than about 1,000 m).
- **Unity services:** Unity Analytics and Cloud Diagnostics off. The app makes no network calls.
- **Bundle ID:** `com.<builder>.navigay`.

## 4. How the app is built

The app is one Unity scene. Screen IDs are App Flow S1–S6. There is no separate Loading screen: if `graph.json` fails, the app stops (broken build). Each part has one job and talks to the others through plain C# data types.

| Part | Folder | Job | Reads | Gives |
|---|---|---|---|---|
| Map data | `Data/` | Load `graph.json` and the photo-map list at start | `StreamingAssets/graph.json` | `GraphData` (lanes, nodes, doors, places, block outlines). Nodes include drawn junctions and automatic crossing points |
| Router | `Routing/` | A* from the user's lane position to the nearest door of a place. v1 uses every edge | `GraphData` | `Route` (lane points, checkpoints, lengths) |
| WhereAmI | `Position/` | Work out where the user is and which way the phone faces, in map coordinates | GPS, compass, ARKit pose, Immersal matches | `PositionEstimate` |
| Lane snap | `Position/` | Put the position on the right lane | `PositionEstimate`, `GraphData`, `Route` | Snapped position, distance along the route |
| Guide | `Guidance/` | Next checkpoint, countdown, reached, re-route, arrival, confidence level | Snapped position, `Route` | `GuidanceState` |
| AR view | `AR/` | Dots, checkpoint signs, ground height | `GuidanceState`, map-to-AR transform | What's drawn in the camera view |
| Screens | `UI/` | Place list, place card, turn card, trip bar, edge arrow, flat arrow, prompts, Immersal logo | `GuidanceState`, `GraphData` | Taps, what's drawn on screen |
| Config | `Config/` | Every tunable number in one `NavConfig` asset | | Values for all parts |
| Dev tools | `Dev/` | Walk recorder and replay (dev builds only) | Everything above | Log files |

Data moves one way each frame: sensors → WhereAmI → lane snap → Guide → AR view and screens. Only WhereAmI talks to the sensors and to Immersal.

## 5. Coordinates

The origin is the **map zero**: the lat/lon that x = 0, y = 0 is measured from. It is pinned to the main gate door once that door is surveyed, then never moved. A user does not start a walk there. A walk starts at the stuck point where they are standing. `start=yes` is a third, separate thing: the connectivity root the build uses to check that doors can be reached. See the Map Data Schema.

- **One local map frame in metres.** `graph.json` stores the map zero. Every map point is stored as x (east) and y (north), in metres from it.
- **One stored copy of the scale.** The build script writes `metres_per_deg` into `graph.json` (`x` 111320, `y` 110574). The app reads those numbers from `graph.json`. C# does not hard-code 111320 or 110574. The script uses the same fields it writes:
  - x = (lon − lon₀) × cos(lat₀) × metres_per_deg.x
  - y = (lat − lat₀) × metres_per_deg.y
  - Across GHS (under 1 km) this adds less than a few centimetres of error.
- **Unity axes:** map x → Unity x, map y → Unity z, height → Unity y.
- **Map-to-AR transform.** ARKit tracks the phone in its own frame, which starts wherever the app opened. WhereAmI keeps one transform (a turn about the vertical axis plus a shift) between the AR frame and the map frame. Every dot and sign is placed with this one transform, so the whole route moves as one piece. When the transform changes, it slides to the new value over about 1 second (F5).

## 6. Position (`WhereAmI`)

All position code lives in `Position/`. Nothing else reads sensors. The output is:

```csharp
public readonly struct PositionEstimate
{
    public readonly Vector2 MapPosition;   // metres, map frame
    public readonly float HeadingDeg;      // 0 = north, clockwise
    public readonly float PositionErrorM;  // the error we actually have
    public readonly float HeadingErrorDeg;
    public readonly PositionSource Source; // Gps, PhotoMap
    public readonly TrackingState Tracking;
    public readonly double Timestamp;
}
```

### Layer 1: phone sensors (always on)

- **GPS.** Immersal's `GPSLocationProvider` (native, double precision) if it gives the accuracy figure we need; otherwise Unity `Input.location`, started with the best accuracy and a 0.1 m update distance. About 1 update per second. Its reported horizontal accuracy becomes `PositionErrorM`. Setup check: confirm which fields `GPSLocationProvider` exposes.
- **Reduced location accuracy.** If the reported accuracy stays above 1,000 m, iOS Precise Location is off. Show a prompt to turn it on in Settings.
- **Compass.** Unity `Input.compass`: `trueHeading` and `headingAccuracy`. Used to set the first turn of the map-to-AR transform, averaged over the 2 seconds the lane lock is held. A lost camera does not cancel that average.
- **Camera tracking.** ARKit gives the phone's movement between GPS updates, smoothly and with little drift over short distances.
- **Combining them.** Camera tracking moves the position every frame. Each GPS fix pulls it toward the GPS position, more strongly when GPS reports a small error and gently when it reports a big one.
- **Heading from walking.** After the user walks about 15 m in a straight line on a snapped lane, the transform's turn is corrected so the walking direction matches the lane's direction.
- **Reset at turns.** When the user turns more than about 60° within 3 seconds and a route junction is within about 15 m, the distance along the route is set to that junction. This removes the error along the lane that GPS leaves.

### Lane snap

- **Candidates:** lane segments within max(25 m, 2 × `PositionErrorM`) of the position. The 25 m here is a search radius. It is not, by itself, permission to open the guide.
- **Line of sight:** drop any candidate where the straight line from the position to the snap point crosses a block outline.
- **Score:** distance to the lane, plus a penalty if the lane isn't on the current route, plus a penalty if the lane's direction disagrees with the walking direction.
- **Stickiness:** switch to a different lane only if it scores clearly better (about 3 m) for about 2 seconds.
- **Off the lanes:** if no candidate is within range, stop snapping and use the raw position. The Guide decides whether to re-route.
- **Known limit:** it can't tell two close parallel lanes apart. Photo maps fix this where they exist.

### Layer 2: photo maps (Immersal)

- **Making a map.** Photograph a spot with the Immersal Mapper app (up to 100 photos on the Free plan). Immersal builds the map in its cloud; download the map file from the developer portal. This needs internet on the Mac; the app doesn't.
- **Shipping it.** Map files go in `Assets/navigay/PhotoMaps/` as `.bytes` files. `map/photo-maps.csv` lists each map's ID, file, placement in the map frame (x, y, height, turn), load radius and the junctions it covers. The build script copies this list into `graph.json`.
- **Placing it.** Start from the GPS position the Mapper app recorded. Then line up the map's point cloud with the lane drawing in the Unity editor (top-down view), and save the alignment (Immersal 2.2.1 and later can save and load map alignment in the editor). Check it on a walk by standing on a known door. A match is only as good as this placement.
- **Loading by GPS.** Using `MapManager`, load maps within about 60 m of the GPS position and unload maps beyond about 100 m. At most 3 are loaded at once.
- **Matching.** `ImmersalSession` with the `Localizer` set to on-phone localization (`DeviceLocalization`) runs only while a map is loaded and tracking is normal. It tries about once a second until it gets a match, then about every 3 seconds.
- **Using a match.** WhereAmI reads the match as a measurement and sets the map-to-AR transform from it, then slides to it over about 1 second. Immersal's own `XRSpace` doesn't move our content; WhereAmI owns the transform. While a match is less than a few seconds old, `Source` is `PhotoMap` and the confidence level can be Full (F10).
- **No developer token in the app.** Immersal says on-phone matching with maps stored in the app needs no token and no internet. Setup check: SDK 2.1.0 added a user check when the SDK starts, so confirm the app starts and matches in airplane mode with the token field empty.
- **Logo (F15).** Use Immersal's logo files unmodified, from the SDK package's `Samples~/Core/ImmersalLogos` folder (import the Core Samples in Package Manager). Use the white version over the camera. Place it bottom-left, above the trip bar, on its own screen layer, where it never covers the dots or the turn card. Immersal's instructions allow it as a watermark with at least 30% transparency; follow the guidelines in their package. The generated mock (`navar-ar-ui-immersal-logo.jpg`, kept outside the repo) only shows the position; never ship it.

### Confidence level (F10)

Worked out every frame from `PositionEstimate`:

Ready and the confidence level are different gates. Names in `NavConfig`:

| Name | Starting value | Effect |
|---|---|---|
| `stuck_error_m` | 25 | With an outside lane lock held for `stuck_hold_s` (2 s), leave Ready. Not the dot bar |
| `full_error_m` / `full_heading_deg` | 5 m / 10° | Full, and only if the camera is fine |
| `short_error_m` / `short_heading_deg` | 12 m / 20° | Short, and only if the camera is fine |

A fine camera means `Tracking` is normal, the scene is not too dark, and the phone is not pointed more than about 60° below the horizon.

| Level | Rule |
|---|---|
| Full | Camera is fine, and (`Source == PhotoMap` with the match less than ~5 s old, **or** position error ≤ `full_error_m` and heading error ≤ `full_heading_deg`) |
| Short | Camera is fine, position error ≤ `short_error_m`, and heading error ≤ `short_heading_deg` |
| Hold | Anything worse, tracking lost, too dark, or the phone lowered |

To move up a level, the condition must hold for 2 seconds. To move down, it changes at once. Hold does not return the user to Ready. Dots are drawn only in Full and Short. Hold draws the flat arrow. If position error is the reason for Hold, the turn card may add `Walk a few steps.` If position is already within `short_error_m` and heading is the reason, it may add `Wave the phone in a figure-eight.` A dark camera adds no sentence.

## 7. Routing (F4, F11)

- **Graph:** nodes are drawn junctions, doors, and automatic crossing points (`auto_`). Lane edges have length in metres and an access tag (`all`, `boys`, `girls`, `staff`). The tag is stored for v2 (F2). v1 does not read it. An `auto_` point is not a drawn junction and does not enter the ID ledger.
- **v1 routes:** every edge is walkable, and every place is listed.
- **Nearest door:** A* runs to a virtual end node joined at zero cost to every door of the place, so it finds the closest door in one search. The heuristic is the straight-line distance to the nearest door.
- **Start:** a legal snap of where the user is standing when the route is calculated. A temporary node splits the lane there. Not the map zero, and not the connectivity root. The minutes are then measured from the stuck point along this route.
- **When:** once, when Go is pressed, before Ready waits for the stuck lock or the camera. Start from a legal lane snap of the position at that press. Do not start from a raw point inside a building. If no fix has arrived, calculate once on the first fix during Ready. If the position cannot be snapped, calculate once when the first legal snap exists. Not on a timer. When the stuck lock completes, settle once: keep the route if the stuck point is on it; otherwise replace it once from that point, still on Ready. Entering the guide does not calculate. Distance along the existing route updates every frame. A* does not. Minutes are read from the stuck point along the settled route.
- **Near you:** when a stuck point is set, the router runs once to every place, nearest door, and stores those routes. The suggestion list sorts by that length. It does not run again every frame. A new stuck point replaces the set once. Tapping a row reuses the stored route for the card and for Go. The whole ranking should finish in under a second on the iPhone 17.
- **Speed:** the graph has at most a few thousand nodes, so one route takes well under 50 ms.
- **Checkpoints:** a checkpoint at every route node where the direction changes by more than about 30°, including an `auto_` point, and at the door. The name comes from a drawn junction's landmark field, falling back to the nearest place name.
- **No path:** Ready shows `Can't find a way there.` as soon as that one calculation fails. The app does not draw a straight line across a building.
- **Re-route:** the only later calculation. When the user has left the route (snapped position more than about 20 m from it, and they have walked at least about 10 m since the last route), route again from the current stuck point. No confirm. No extra vibration. Turn card and dots update in place. The new minutes use that new point.

## 8. Guidance (F6, F7, F8, F12)

- **Distance along the route** is the main number. It moves with camera tracking, is corrected by GPS, and is reset at turns and by photo-map matches.
- **Countdown:** the distance along the route to the next checkpoint, rounded to 5 m above 50 m and to 1 m below 50 m. The shown value never goes up, unless camera tracking shows the user walking backwards along the route by more than about 3 m.
- **Checkpoint reached:** within about 8 m, or once the distance along the route has passed it. Then vibrate and move to the next checkpoint.
- **Minutes left:** distance left along this route ÷ walking speed. Start at 1.2 m/s, then use the user's own average pace on this walk, kept between 0.8 and 1.8 m/s. The place card uses the same route, the same stuck point, and the same speed, so both show one answer when the walk opens. Until the user is stuck, the card shows `Step outside to see the time.` A GPS fix inside a building does not unlock the minutes.
- **Arrival:** at the last checkpoint (the door). Show the place name, vibrate, show time taken and distance walked, Done → place list. Underpass adds `This is the entrance. navigay stops here.`
- **End:** returns to the place list immediately. No confirm sheet (App Flow).
- **Vibration:** `Handheld.Vibrate()` in v1. A small native plugin for lighter taps is optional later. Checkpoint and arrival vibrate once. Re-route, End, and Done do not.

## 9. AR view and screens (F5, F9, F15)

- **Ground:** use the ARKit horizontal plane under the user. If none is found yet, put dots 1.5 m below the camera. The iPhone 17 has no LiDAR, so plain concrete often uses that fallback first. GHS is flat, so one ground height per stretch is enough.
- **Dots:** spaced about 1.5 m apart along the lane centre, from the user to the next checkpoint, at most about 40 m (Full) or 10 m (Short), at most about 30 dots. Colour `#0A84FF` (UI Brief). Drawn as one instanced mesh, smaller and fainter with distance. No dots in Hold.
- **Checkpoint sign:** a glass pill facing the camera, showing step number, turn, landmark and distance.
- **Edge arrow:** Full and Short, when the next dots are off-screen; points along the screen edge toward them. Hidden in Hold.
- **Flat arrow:** a 2D UI arrow on the guide in Hold (tracking lost, night, phone lowered more than about 60° below the horizon, or weak fix). Points to the next checkpoint using the compass and position. Turn card and trip bar stay. Not a 3D floor mesh; not the store reference 3D arrow. Night does not keep the user on Ready. Dots are not drawn until the camera is fine and the level is Short or Full.
- **Turn card, trip bar, prompts, place list and place card:** Unity UI (uGUI) with TextMeshPro. Layout and style are in the UI/UX Brief. Ready uses **Back** (to the place card), not Cancel.
- **Screen layers, back to front:** camera, dots and signs, edge arrow and flat arrow, turn card and trip bar, Immersal logo, prompts. The logo has a fixed safe area that no other screen element overlaps (S4–S6).

## 10. Map build pipeline (Python, Mac only)

The full file formats, join rules and checks are in the [Map Data Schema](05-map-data-schema.md). In short:

**Inputs, all in `map/`:**

- `ghs.geojson`: shapes, drawn in geojson.io. Lane centrelines (LineStrings), doors and junctions (Points), and block outlines (Polygons). Each has an `id`. Lanes have an `access` tag.
- `places.csv`: `place_id`, `name`, `type`, `access`, `door_ids` (separated by `;`), `start`. Edited in any spreadsheet app and saved as CSV.
- `photo-maps.csv`: one row per Immersal map (section 6).
- `id-ledger.csv`: every ID ever used, with its kind. The build script adds new IDs and never removes rows.

**Steps (`tools/build_graph.py`):**

1. Read the inputs and convert every point to map-frame metres (section 5).
2. Join lane ends that are within about 2 m of each other. Where two lanes cross, split both and insert an automatic point (`auto_`). That point is not a drawn junction, it is not written back into `ghs.geojson`, and it does not enter `id-ledger.csv`.
3. Join each door to its lane, by the rules in the Map Data Schema doc.
4. Compute every edge length in metres, once.
5. Check everything (below). Any failure stops the build with a clear message and exit code 1.
6. Write `map/build/graph.json` and `map/build/problems.geojson`. With `--install`, also copy `graph.json` to `app/Assets/StreamingAssets/`.

**The build fails if:**

- a place has no door
- a door ID in `places.csv` doesn't exist in `ghs.geojson`
- two places have the same name
- a place or a lane has no access tag, or an access tag isn't `all`, `boys`, `girls` or `staff`
- two features share an ID
- an ID from `id-ledger.csv` comes back as a different kind of thing, so an ID is reused
- the lanes, counting every access tag, aren't one connected piece
- a door cannot be reached from a start place when every lane is walkable (the Map Data Schema has the exact rule)
- a photo map covers a junction ID that doesn't exist

**Warnings, written to `problems.geojson` for viewing in geojson.io:** a dead end within about 3 m of another lane, a lane shorter than 1 m, a lane that crosses a block outline, or a block outline with no place (`unnamed_block`). The full list and the exact meanings are in the Map Data Schema.

**ID rules:** never reuse an ID. A rename changes the name, not the ID. ID formats are in the Map Data Schema doc.

**Map-zero freeze:** `origin.json` is the map zero, not the place a walk starts. Do not commit it while the underpass is still the only `start=yes`. The script may write a local file on a dry run; that is not a freeze. Freeze only after the main gate door is surveyed, from that door, whether or not the gate is also the connectivity root. Later builds never move it. A walk can still start on any stuck lane. Same rule as the Map Data Schema and the Implementation Plan.

**Changing the map in v1:** a new restaurant means a new row in `places.csv`, a new door point in `ghs.geojson`, a rebuild with `--install`, a new Unity build and a new install on each phone.

**Python packages** (`tools/requirements.txt`, versions pinned): `shapely` 2.x, `pytest`. CSV and JSON use the standard library.

## 11. Folder structure and naming

```
navigay/
  docs/                      the 6 docs
  UI package/                reference images
  map/
    ghs.geojson              shapes, drawn in geojson.io
    places.csv               place names, types, access, door IDs
    photo-maps.csv           Immersal maps and where they sit
    id-ledger.csv            every ID ever used
    build/                   script output (graph.json, problems.geojson)
  tools/
    build_graph.py
    requirements.txt
    tests/                   pytest, with small sample maps
  app/                       the Unity project
    Assets/
      navigay/
        Scripts/
          Config/            NavConfig
          Data/              GraphData, GraphLoader
          Routing/           Router
          Position/          WhereAmI, GpsCompassSource, PhotoMapSource, LaneSnap
          Guidance/          Guide, Countdown, Confidence
          AR/                DotTrail, CheckpointSign, GroundHeight
          UI/                PlaceList, PlaceCard, TurnCard, TripBar, EdgeArrow, FlatArrow, Prompts, ImmersalLogo
          Dev/               WalkRecorder, WalkReplay
        Tests/
          EditMode/
          PlayMode/
        PhotoMaps/           Immersal .bytes map files
        Prefabs/
        Scenes/
        Art/
      StreamingAssets/
        graph.json           copied in by the build script
    Packages/
    ProjectSettings/
```

**Naming:**

- C#: namespaces `Navigay.Config`, `Navigay.Data`, `Navigay.Routing`, `Navigay.Position`, `Navigay.Guidance`, `Navigay.AR`, `Navigay.UI`, `Navigay.Dev`. One class per file, file named after the class, PascalCase.
- Python: snake_case.
- Map files: lowercase with hyphens.
- `graph.json` field names: snake_case.

## 12. Testing and logging

- **Python:** pytest on small hand-made maps: join, split, door joining, and every build-failure rule from section 10.
- **Unity Edit Mode tests:** router (nearest door, every edge including `boys`, `girls` and `staff`, virtual end node), lane snap (line-of-sight rule, stickiness), countdown (never goes up from a GPS jump), confidence level (thresholds and the 2-second rule), checkpoint reached (8 m or passed). The access filter is a v2 test.
- **Walk recorder (dev builds only):** about 10 times a second, writes one line of JSON per sample to the app's Documents folder: time, GPS and its accuracy, compass and its accuracy, ARKit pose and tracking state, photo-map match result, snapped position, distance along the route, shown countdown, confidence level. File sharing is on in dev builds so logs can be copied off with the Files app. Camera images aren't recorded.
- **Walk replay (Unity editor):** feeds a recorded log into WhereAmI in place of the live sensors, so lane snap, the Guide and the countdown can be fixed at a desk and re-checked against the same walk. Photo-map matches replay as recorded results.
- **Field tests:** about 10 routes, day and night, with the metrics in PRD section 12. Record metres off at every turn.

## 13. Performance and battery

| Item | Budget |
|---|---|
| Frame rate | 60 fps on the iPhone 17 |
| Route computation | Under 50 ms |
| Time to first directions, outdoors by day | Under 20 s |
| Photo maps loaded at once | At most 3 |
| Photo-map matching | Only near a loaded map: about once a second until a match, then about every 3 s |
| Dots on screen | At most about 30 |
| Battery | Measured per 10-minute walk; investigate if above 15% |

## 14. Privacy and network

- v1 code makes no network calls. Map, router and photo maps are all inside the app.
- Position, camera images and logs stay on the phone.
- Unity Analytics and Cloud Diagnostics are off.
- Setup check: watch network traffic during a test walk to confirm neither Unity nor the Immersal SDK sends anything. If something does, turn it off or report it here.

## 15. Rules for anyone writing code (including AI agents)

- Never add Google, Apple Maps, Niantic or ARCore Geospatial packages or data.
- No network calls in v1.
- Only `Position/` reads GPS, compass, ARKit pose or Immersal. Everything else uses `PositionEstimate`.
- No place names, coordinates or IDs written into C#. Everything comes from `graph.json`.
- Every tunable number lives in `NavConfig`.
- A `switch` over an enum handles every value, and its `default` throws `ArgumentOutOfRangeException`, so a new value can't be silently ignored.
- The build script fails loudly. It only fixes data in the documented ways (joining ends, splitting crossings, joining doors).
- Never remove, hide, cover or modify the Immersal logo.
- Keep the Unity version and Immersal version in section 3 in sync with the project.

## 16. v2 technical notes

- **Access filter (F2):** the router and the place list start reading `access`. Boys uses `all` and `boys`. Girls uses `all` and `girls`. Staff uses every tag. No change to `graph.json`.
- **Map updates without an app update:** host `graph.json` and a small version file on a static host such as Cloudflare R2. At launch the app checks the version with `UnityWebRequest`, downloads a newer `graph.json`, keeps it in `Application.persistentDataPath`, and falls back to the copy inside the app.
- **Accounts or live edits:** only then add a server and database (Convex in TypeScript, called over HTTP). A database is only for non-developers editing from a phone and seeing the change at once.
- **Android:** add the ARCore XR Plugin and Immersal's Android plugin, and set Unity 6's Application Entry Point to include Activity (Immersal's scripts expect it).
- **Immersal Pro:** a billing change; same plugin, same maps.
- **Niantic:** scan GHS again and write a new position source inside `Position/`. Nothing else changes.
- **Apple location anchors test:** AR Foundation doesn't expose them, so test with a tiny separate Swift app that calls `ARGeoTrackingConfiguration.checkAvailability` at the gate and between B2 and B3. A bonus only.

## 17. Open items

| Item | How it closes |
|---|---|
| Exact Unity 6.3 patch | Closed: **6000.3.29f1**. Copy that into `ProjectSettings/ProjectVersion.txt` when the Unity project is created |
| App starts and matches photo maps in airplane mode with no Immersal token | Setup check on the iPhone 17 |
| Which fields Immersal's `GPSLocationProvider` gives | Setup check; otherwise use `Input.location` |
| Nothing sent over the network | Setup check during a test walk |
| Apple geotracking at the GHS gate | Small Swift test later; bonus only |
| Real accuracy | Measure metres off at turns on real walks; no number is a promise until then |
| Immersal license for public release | Re-read the license before any public release |
