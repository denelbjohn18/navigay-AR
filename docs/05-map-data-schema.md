# navigay: Map Data Schema

Doc 5 of 6 · Draft 4 · 24 Nov 2026

The template calls this the Backend Schema. navigay v1 has no server, no database, no accounts and no API. The map files are the data model. This doc is the source of truth for those files. The build steps in the [TRD](02-TRD.md) follow the rules here. Who does the survey and when: [Implementation Plan](06-implementation-plan.md). Screens never show IDs or access tags: [App Flow](03-app-flow.md).

Two files hold what you edit, joined by ID:

- `map/ghs.geojson` holds the shapes: lane centrelines, doors, junctions and block outlines.
- `map/places.csv` holds the names: which doors belong to which place.

The build script joins them into `graph.json`, the only map file the app reads.

## Access rule

IDs never change, and every lane and place keeps an `access` tag for v2. The values are `all`, `boys`, `girls`, and `staff`.

v1 ignores the tag. A route may use every lane, and the place list shows every place. The build treats the whole campus as one walkable network.

v2 reads the same tags, with no new file and no new ID:

| Choice | Lanes and places it may use |
|---|---|
| Boys | `all`, `boys` |
| Girls | `all`, `girls` |
| Staff | `all`, `boys`, `girls`, `staff` |

---

## 1. Files

| File | Who edits it | What it is |
|---|---|---|
| `map/ghs.geojson` | You, in [geojson.io](https://geojson.io) | Shapes, in longitude and latitude |
| `map/places.csv` | You, in a spreadsheet, saved as CSV | Place names, types, who may go there, door IDs |
| `map/photo-maps.csv` | You, after a spot is photographed | Where each Immersal photo map sits. Header only until then |
| `map/origin.json` | The script, once | The map zero: the fixed point metres are measured from. Not where a walk starts |
| `map/id-ledger.csv` | The script | Every ID ever used. Stops an old ID coming back as something else |
| `map/build/graph.json` | The script | The route file. Copied into the app |
| `map/build/problems.geojson` | The script | Warnings to look at in geojson.io. The app never reads it |
| `app/Assets/navigay/PhotoMaps/*.bytes` | You, downloaded from Immersal | The photo maps themselves |
| `app/Assets/StreamingAssets/graph.json` | The script, with `--install` | The copy the app loads |

Nothing here is secret. There are no user rows, no passwords and no precise trail of where a person walked.

## 2. IDs

An ID is a permanent label. A rename changes the name, never the ID. An ID is never given to a second thing, even after the first thing is deleted.

| Kind | ID looks like | Lives in |
|---|---|---|
| Lane | `lane_001` | `ghs.geojson` |
| Door | `door_001` | `ghs.geojson` |
| Junction | `junc_001` | `ghs.geojson` |
| Block outline | `block_001` | `ghs.geojson` |
| Place | `place_001` | `places.csv` |
| Photo map | `pmap_001` | `photo-maps.csv` |

Rules:

- The pattern is the prefix, an underscore, then at least three digits: `lane_014`, not `lane_14` and not `lane-014`.
- The prefix must match the kind. `door_004` cannot be a lane.
- You pick the next free number. Gaps are fine.
- The same ID cannot appear twice.
- Generated points (`auto_0001`, `edge_0007`) exist only inside `graph.json`. They are rebuilt every run, they are not written back into `ghs.geojson`, and they never go in the ledger. Places and photo maps must point at your IDs, not at `auto_` IDs.

### The ledger

`map/id-ledger.csv`:

| Column | Required | Values |
|---|---|---|
| `id` | yes | an ID from the table above |
| `kind` | yes | `lane`, `door`, `junction`, `block`, `place`, `pmap` |
| `note` | no | why it was retired, or anything you want to remember |

- The first build creates the file from the IDs it finds.
- Later builds add new IDs and never delete a row.
- An ID still in the files must keep the kind written in the ledger. A change of kind is a reuse, and the build fails.
- An ID in the ledger but missing from the files is retired. Leave the row. Write the reason in `note`.
- Deleting the ledger wipes the reuse check. Don't.

## 3. `ghs.geojson`

A GeoJSON `FeatureCollection` drawn in geojson.io. Coordinates are `[longitude, latitude]` in WGS84, as in RFC 7946. Altitude is left out. A `crs` member is a failure, because it means the coordinates may not be plain WGS84.

Each feature is one shape. Its `properties` are:

| Property | Lane | Door | Junction | Block |
|---|---|---|---|---|
| `id` | required | required | required | required |
| `feature` | `lane` | `door` | `junction` | `block` |
| `access` | required | | | |
| `landmark` | | | optional | |
| `place_id` | | | | optional |
| `needs_survey` | | optional bool | | |
| `draft` | | optional bool | | |
| `name` / `old_id` | | optional notes | | optional |

Geometry must match the feature:

| `feature` | Geometry | Shape rule |
|---|---|---|
| `lane` | `LineString` | At least two positions. The line runs along the middle of the walkable path |
| `door` | `Point` | The entrance itself, not the middle of the lane |
| `junction` | `Point` | Where lanes meet, or where a checkpoint sign should name a landmark |
| `block` | `Polygon` | The building outline. The first ring is the outside; further rings are holes. The ring is closed: the first and last position are the same |

`access` is one of `all`, `boys`, `girls`, `staff`. It is required. v1 stores it and routes through every value. v2 uses it as the access rule at the top of this doc.

`landmark` is the short name painted on a checkpoint sign, such as `B2`. Leave it out and the app uses the nearest place name.

`place_id` on a block links the outline to a place. The app uses it for "You're inside B3." A block with no `place_id` still blocks lane snap; the prompt then says "You're inside a building."

`needs_survey` / `draft` mark a door that was guessed, not GPS-averaged. The build still accepts them so code can be tested. Do not show friends a route that ends on a draft door. Clear both flags only after the survey walk (Implementation Plan §2 B).

Other unknown properties are ignored, so geojson.io's own metadata does no harm. The ID is read from `properties.id`. If that is missing and the feature has a top-level `id`, that value is used. If both are set and they differ, the build fails.

### How to draw

- Draw lanes yourself while looking at satellite imagery. Don't import a Google or Apple export, and don't paste in the old OpenStreetMap extract. It is missing newer blocks.
- One line can run through several junctions. The script splits it where lines cross and where a junction point sits on it.
- Stop a lane at a door's path. Don't draw the lane through the building.
- Don't draw a lane into or through the underpass. The underpass is a place of type `gate` with a door at the GHS entrance. Directions end at that door.
- A mess with several open sides gets one door point per side, all listed on the same place.
- Draw a junction where a sign should say a name (`B2`, `mess corner`). A crossing with no name still gets split by the script into an automatic point (`auto_`). That point is a real node the route can turn on. It is not a drawn junction, it is not written back into `ghs.geojson`, and it does not enter the ID ledger. Only a drawn junction keeps a stable ID. The TRD uses this same `auto_` rule.
- Keep each block outline tight to the building. A sloppy outline makes the app refuse the lane behind it.

Door positions are measured on foot: stand at the door, average the GPS for about 30 seconds, do it twice, and use the mean. Satellite imagery can be several metres off, so a door dropped by eye on the photo is not good enough.

## 4. `places.csv`

UTF-8 CSV with a header row. The script accepts a byte-order mark and both line endings. Lists inside a cell use `;`, so a name may contain a comma if the cell is quoted. The script reads it with Python's CSV rules.

| Column | Required | Values |
|---|---|---|
| `place_id` | yes | `place_001` |
| `name` | yes | what the app shows, such as `Warden office` |
| `type` | yes | `block`, `food`, `office`, `gate`, `sports` |
| `access` | yes | `all`, `boys`, `girls`, `staff` |
| `door_ids` | yes | one or more door IDs, separated by `;` |
| `start` | yes | `yes` or `no` |

A name is compared with spaces trimmed and letter case ignored. Two places cannot share one.

`type` chooses the group in the place list: Blocks, Food, Offices and services, Gates, Sports. The underpass entrance is a `gate`.

`access` is stored for v2. v1 lists every place, including `boys`, `girls`, and `staff`.

`start=yes` marks the **connectivity root**: the door the build uses to check that every other door can be reached. It is usually the main gate. At least one is required. One is enough for the whole campus. A root needs a door, like every other place.

This flag is not where a walk starts. The user can open the app anywhere in GHS. The route starts at the lane the phone has stuck them to. The map zero in `origin.json` is also not where a walk starts.

A door belongs to one place. A door in the drawing with no place, or a door listed twice, fails the build.

## 5. `photo-maps.csv`

Same CSV rules. The file may contain only the header until the first spot is photographed.

| Column | Required | Values |
|---|---|---|
| `pmap_id` | yes | `pmap_001` |
| `file` | yes | filename only, such as `pmap_001.bytes` |
| `x`, `y`, `z` | yes | position in map metres. `x` is east, `y` is north, `z` is up |
| `yaw_deg` | yes | turn of the map, degrees clockwise from north |
| `load_radius_m` | yes | GPS distance at which the app loads this map. Start at `60` |
| `junction_ids` | yes | the drawn junctions this photo map covers, separated by `;` |

The `.bytes` file lives in `app/Assets/navigay/PhotoMaps/`. A row whose file is missing fails the build, so a half-finished map cannot ship. Delete the row until the file is in place.

`x`, `y`, `z` and `yaw_deg` are the alignment you save in the Unity editor after lining the point cloud up with the lanes. They are metres from `origin.json`, not longitude and latitude. A blank placement fails the build.

`junction_ids` must be drawn junctions. That list is how a test walk knows which corners this photo map is responsible for.

## 6. `origin.json`

This file is the **map zero**, the fixed lat/lon that x = 0, y = 0 is measured from. It is not the start of a walk, and it is not the connectivity root (`start=yes`). A user can stand anywhere in GHS, pick a place, and be routed from the lane they are stuck to. The zero stays put so that a photo map aligned today still lines up after a lane is redrawn. We pin it to the main gate's surveyed door once that door exists. Calling that pin the entry gate only names where the zero was measured. It does not make the gate the start of navigation.

```json
{
  "lat": 26.843210,
  "lon": 75.565430,
  "note": "main gate, set once"
}
```

The numbers above are a shape example, not the surveyed gate.

- **Freeze rule (locked with the Implementation Plan):** the script may create a local `origin.json` on a dry run. Creating it is not freezing it. While the underpass is still the only `start=yes`, do **not** commit that file. Freeze once, after the main gate door is surveyed, from that door. Commit when asked. Do not freeze it from the underpass, and do not plan to move a committed zero later.
- Later builds read it and never move it. Moving it shifts every metre value and every photo map, including a two-point test that had already aligned a photo map.
- Every drawn point must fall within 1,500 m of the origin. Anything farther fails. That catches a point dropped at `0,0` or in the wrong city.

The script writes `metres_per_deg` into `graph.json` as `x` = 111320 and `y` = 110574. The app reads those numbers back. C# does not keep a second copy. After the write, the script converts with the same stored fields:

- `x = (lon − lon0) × cos(lat0) × metres_per_deg.x`
- `y = (lat − lat0) × metres_per_deg.y`

`x` is east, `y` is north, both in metres. Across GHS this is within a few centimetres of the true distance.

## 7. How the files join

| From | To | Rule |
|---|---|---|
| `places.door_ids` | a door feature | every ID exists, and each door is listed once |
| `places.place_id` | itself | unique |
| `block.place_id` | `places.place_id` | optional; if set, the place exists |
| `photo-maps.junction_ids` | a junction feature | every ID exists |
| `photo-maps.file` | `PhotoMaps/<file>` | the file exists |
| a lane, door, junction, block, place or photo map | `id-ledger.csv` | same ID, same kind |
| a graph edge | the lane it was cut from | lane edges carry that lane's ID and access |
| a door node | its place | one place |

## 8. What the build does to the drawing

Numbers used while joining:

| Name | Value | Effect |
|---|---|---|
| `JOIN_M` | 2 m | lane ends, and a lane passing a drawn junction, weld together |
| `DOOR_MAX_M` | 15 m | a door farther than this from a lane fails the build |
| `NODE_REUSE_M` | 2 m | a door uses an existing point if the closest spot on the lane is within this |
| `FAR_M` | 1,500 m | a point farther from the origin fails the build |
| `SHORT_EDGE_M` | 1 m | warning, not a failure |
| `DEAD_END_M` | 3 m | warning, not a failure |

The script may change geometry only in the ways below. It writes the result into `graph.json` and leaves `ghs.geojson` untouched.

1. **Convert.** Every position becomes `x`, `y` metres from the origin.
2. **Clean.** Drop a repeated position that sits within 0.05 m of the one before it.
3. **Weld ends.** Each lane end welds at most once, to the nearest drawn junction or other lane end within 2 m. Nearest pairs go first. Three ends meeting at a corner still become one point, because the third welds to the pair. Ends farther apart stay apart, so a chain of short gaps cannot collapse a whole lane.
4. **Split crossings.** Where two lane interiors cross, both split and an `auto_` point is inserted. Touching at an already shared end is not a crossing.
5. **Split at drawn junctions.** A drawn junction within 2 m of a lane, but not already an end, splits that lane and welds to the junction. A drawn junction farther than 2 m from every lane fails the build.
6. **Attach doors.** The closest point on any lane must be within 15 m. If that spot is within 2 m of a point that already exists, the door connects to it. Otherwise the lane splits there and the door connects to the new `auto_` point. The door stays on its surveyed coordinate. The connector's length is the real gap from the lane to the door, and it inherits that lane's `access`.
7. **Measure.** Each edge length is the length of its line in metres, computed once.
8. **Check.** Any failure stops the build with exit code 1 and a message that names the ID. Nothing is written to the app on a failed build.
9. **Write.** `map/build/graph.json` and `map/build/problems.geojson`. With `--install`, also copy `graph.json` to `app/Assets/StreamingAssets/`.

`auto_` numbers and `edge_` numbers come from sorting the generated points, so the same drawing always produces the same file.

## 9. Failures

The build fails when any of these is true:

- A file is missing, unreadable, or not valid JSON or CSV. `photo-maps.csv` may be header-only. `origin.json` and `id-ledger.csv` may be created on the first run. A created `origin.json` is still uncommitted until the main-gate freeze above.
- An ID is missing, duplicated, wrongly shaped, or has a prefix that disagrees with its kind.
- The ledger has that ID with a different kind.
- A lane or a place has no `access`, or a value outside `all`, `boys`, `girls`, `staff`.
- A place has a `type` outside `block`, `food`, `office`, `gate`, `sports`, or a blank `start` value.
- Two places have the same name.
- A place has no door. A listed door does not exist. A door is listed on two places. A drawn door is on no place.
- A block's `place_id` does not exist. A photo map's junction does not exist, or its `.bytes` file is missing.
- Geometry doesn't match the feature, a polygon is self-crossing, or a point is more than 1,500 m from the origin.
- There is no `start=yes` place.
- Any door cannot be reached from a start door when every edge is walkable, whatever its `access` tag.

v1 does not check boys, girls, and staff as separate networks. That check belongs to v2, using the table in the access rule.

## 10. Warnings

These are written to `problems.geojson` and the build still succeeds. Open that file in geojson.io and fix anything that is a real mistake.

| `problem` | When |
|---|---|
| `dead_end` | a point that is not a door has only one edge, and another lane passes within 3 m without connecting |
| `short_lane` | a lane edge is shorter than 1 m. A short door connector is normal and is not warned |
| `crosses_block` | a lane cuts through a block outline rather than only touching its edge |
| `unnamed_block` | a block has no `place_id` |

Each warning feature keeps the offending geometry and has `problem`, `message` and `ids` in its properties.

## 11. `graph.json`

Schema version 1. The app refuses any other `schema` number. Field names stay `snake_case`. Unknown fields are ignored, so a later addition doesn't break an older app.

```json
{
  "schema": 1,
  "origin": { "lat": 26.843210, "lon": 75.565430 },
  "metres_per_deg": { "x": 111320.0, "y": 110574.0 },
  "nodes": [],
  "edges": [],
  "places": [],
  "blocks": [],
  "photo_maps": []
}
```

### `nodes`

| Field | Type | Meaning |
|---|---|---|
| `id` | string | a drawn ID, or `auto_0001` |
| `kind` | string | `junction`, `door`, `auto` |
| `x`, `y` | number | metres from the origin |
| `lat`, `lon` | number | the original coordinate, for checking on a map |
| `place_id` | string or null | set on doors |
| `landmark` | string or null | copied from a drawn junction |
| `edge_ids` | string array | the edges that touch this point, so routing doesn't scan the whole file |

### `edges`

Walking is allowed both ways. Each edge is stored once.

| Field | Type | Meaning |
|---|---|---|
| `id` | string | `edge_0007` |
| `kind` | string | `lane` or `door` |
| `from`, `to` | string | node IDs |
| `lane_id` | string or null | the drawn lane. Null on a door connector |
| `access` | string | `all`, `boys`, `girls`, `staff` |
| `length_m` | number | length of `polyline`, used by the router |
| `polyline` | array of `[x, y]` | the line the dots follow |

### `places`

A copy of `places.csv` with the same field names: `place_id`, `name`, `type`, `access`, `door_ids`, `start`. `start` is a JSON boolean.

### `blocks`

| Field | Type | Meaning |
|---|---|---|
| `id` | string | `block_001` |
| `place_id` | string or null | |
| `name` | string or null | the place name, so the prompt can say "B3" |
| `rings` | array of rings of `[x, y]` | ring 0 is the outside; later rings are holes |

The app treats a point inside ring 0 and outside any hole as inside the building.

### `photo_maps`

A copy of `photo-maps.csv`: `pmap_id`, `file`, `x`, `y`, `z`, `yaw_deg`, `load_radius_m`, `junction_ids`. Numbers are JSON numbers, not strings.

## 12. Template rows that don't apply to v1

| Template row | navigay v1 |
|---|---|
| Auth provider | None. The app doesn't know who the person is |
| Row level security | None. The files contain no personal rows |
| User roles | v1 stores no side and does not filter. `access` stays on each lane and place for v2, when the phone will store Boys, Girls, or Staff |
| File storage | The files in section 1, in the repo. Photo maps are `.bytes` files in the Unity project. No uploads |
| Sensitive fields | None. No account, payment or location history |
| Webhooks | None |
| API | None. The app reads `StreamingAssets/graph.json` and the `.bytes` files in the build |

## 13. Worked example

A corner, one building and one door. Coordinates are fake. They only show the shape.

`places.csv`:

```csv
place_id,name,type,access,door_ids,start
place_001,Main gate,gate,all,door_001,yes
place_002,B2,block,boys,door_002,no
```

`ghs.geojson`, with the lines shortened:

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "properties": { "id": "lane_001", "feature": "lane", "access": "all" },
      "geometry": { "type": "LineString", "coordinates": [[75.56500, 26.84300], [75.56520, 26.84300]] }
    },
    {
      "type": "Feature",
      "properties": { "id": "junc_001", "feature": "junction", "landmark": "B2 corner" },
      "geometry": { "type": "Point", "coordinates": [75.56520, 26.84300] }
    },
    {
      "type": "Feature",
      "properties": { "id": "door_002", "feature": "door" },
      "geometry": { "type": "Point", "coordinates": [75.56521, 26.84302] }
    },
    {
      "type": "Feature",
      "properties": { "id": "block_001", "feature": "block", "place_id": "place_002" },
      "geometry": { "type": "Polygon", "coordinates": [[[75.56522, 26.84301], [75.56530, 26.84301], [75.56530, 26.84308], [75.56522, 26.84308], [75.56522, 26.84301]]] }
    }
  ]
}
```

What the build makes of `door_002`: the point is a few metres north of `lane_001`, so the lane splits near `junc_001` and a short `door` edge runs from the lane up to the surveyed door. The router can end there. The dots follow the lane, then that short edge.

Adding a restaurant later, in v1, is a new `place_` row, a new `door_` point, a rebuild, a new app build and a new install. The old IDs stay as they are.

## 14. What you do

Full checklist with done-when lines: [Implementation Plan](06-implementation-plan.md) §2. Short form:

1. Import `map/ghs.geojson` into geojson.io (not an old tab). Move draft doors to real entrances.
2. Add the main gate; set gate `start=yes`, underpass `start=no`.
3. GPS ~30 s twice at each door; clear `needs_survey` / `draft` only for doors you stood on.
4. After the main gate door is surveyed, let one build freeze the map zero in `origin.json` from that door. This is not where walks start. Commit when asked. Never move it. The first two-place walk does not wait on this freeze.
5. Run the build. Fix every failure. Open `problems.geojson` for real gaps.
6. Photo-map rows only after a spot is photographed, aligned, and saved as `.bytes`.

**In the file on 24 Nov 2026:** places are B1–B3, B5–B10, G2–G4, Quess Mess, BlueDove Mess, Underpass entrance. Underpass is still the placeholder `start=yes`. Columns above do not change because of that.

## 15. v2

v2 filters with the access tags already in this file. Boys uses `all` and `boys`. Girls uses `all` and `girls`. Staff uses every tag. IDs stay. `schema` stays `1`.

v2 can also host this same `graph.json` at a static URL such as Cloudflare R2. The schema number stays the contract: a new field is safe, a renamed field or a new `schema` number is an app change.

A database appears only if people who don't edit these files need to change a place from a phone and see it at once. The phone would still route on a `graph.json` built from that database. The columns in this doc are the columns that database would need.
