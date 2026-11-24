"""Join the GHS drawing and the places sheet into graph.json.

The map zero is map/origin.json. If that file is missing, this run uses a
provisional centroid and does not write origin.json. Creating that file is a
later freeze, from the surveyed main gate, and only when asked. A dry run is
not a freeze. start=yes is only the connectivity root.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import sys
from dataclasses import dataclass, field
from pathlib import Path

from shapely.geometry import LineString, Point, Polygon
from shapely.validation import explain_validity

ROOT = Path(__file__).resolve().parent.parent

METRES_PER_DEG = {"x": 111320.0, "y": 110574.0}
JOIN_M = 2.0
DOOR_MAX_M = 15.0
NODE_REUSE_M = 2.0
FAR_M = 1500.0
SHORT_EDGE_M = 1.0
DEAD_END_M = 3.0
CLEAN_M = 0.05

ACCESS_VALUES = ("all", "boys", "girls", "staff")
PLACE_TYPES = ("block", "food", "office", "gate", "sports")
FEATURE_KIND = {
    "lane": "lane",
    "door": "door",
    "junction": "junction",
    "block": "block",
}
KIND_PREFIX = {
    "lane": "lane",
    "door": "door",
    "junction": "junc",
    "block": "block",
    "place": "place",
    "pmap": "pmap",
}
GEOM_TYPE = {
    "lane": "LineString",
    "door": "Point",
    "junction": "Point",
    "block": "Polygon",
}

PLACE_COLUMNS = ("place_id", "name", "type", "access", "door_ids", "start")
PHOTO_COLUMNS = (
    "pmap_id",
    "file",
    "x",
    "y",
    "z",
    "yaw_deg",
    "load_radius_m",
    "junction_ids",
)


class BuildFailure(Exception):
    def __init__(self, errors: list[str]) -> None:
        self.errors = list(errors)
        super().__init__("\n".join(self.errors))


@dataclass
class Origin:
    lat: float
    lon: float


@dataclass
class Vertex:
    x: float
    y: float
    key: str | None = None


@dataclass
class Lane:
    id: str
    access: str
    verts: list[Vertex]


@dataclass
class EndPoint:
    lane_index: int
    which: int
    x: float
    y: float


@dataclass
class Junction:
    id: str
    x: float
    y: float
    lat: float
    lon: float
    landmark: str | None


@dataclass
class Door:
    id: str
    x: float
    y: float
    lat: float
    lon: float
    place_id: str


@dataclass
class Block:
    id: str
    place_id: str | None
    name: str | None
    rings_xy: list[list[tuple[float, float]]]
    rings_ll: list[list[list[float]]]


@dataclass
class Place:
    place_id: str
    name: str
    type: str
    access: str
    door_ids: list[str]
    start: bool


@dataclass
class PhotoMap:
    pmap_id: str
    file: str
    x: float
    y: float
    z: float
    yaw_deg: float
    load_radius_m: float
    junction_ids: list[str]


@dataclass
class EndCluster:
    ends: list[EndPoint] = field(default_factory=list)
    junction: Junction | None = None


@dataclass
class DoorLink:
    door: Door
    lane_key: str
    access: str


def to_xy(lon: float, lat: float, origin: Origin) -> tuple[float, float]:
    scale_x = math.cos(math.radians(origin.lat)) * METRES_PER_DEG["x"]
    x = (lon - origin.lon) * scale_x
    y = (lat - origin.lat) * METRES_PER_DEG["y"]
    return x, y


def to_lonlat(x: float, y: float, origin: Origin) -> tuple[float, float]:
    scale_x = math.cos(math.radians(origin.lat)) * METRES_PER_DEG["x"]
    lon = origin.lon + x / scale_x
    lat = origin.lat + y / METRES_PER_DEG["y"]
    return lon, lat


def _dist(ax: float, ay: float, bx: float, by: float) -> float:
    return math.hypot(ax - bx, ay - by)


def _segment_hit(
    px: float, py: float, ax: float, ay: float, bx: float, by: float
) -> tuple[float, float, float, float]:
    dx = bx - ax
    dy = by - ay
    len2 = dx * dx + dy * dy
    if len2 <= 1e-18:
        return _dist(px, py, ax, ay), 0.0, ax, ay
    t = ((px - ax) * dx + (py - ay) * dy) / len2
    t = max(0.0, min(1.0, t))
    qx = ax + t * dx
    qy = ay + t * dy
    return _dist(px, py, qx, qy), t, qx, qy


def _max_pairwise(pts: list[tuple[float, float]]) -> float:
    worst = 0.0
    for i in range(len(pts)):
        for j in range(i + 1, len(pts)):
            worst = max(worst, _dist(pts[i][0], pts[i][1], pts[j][0], pts[j][1]))
    return worst


def _min_sep(a: EndCluster, b: EndCluster) -> float:
    left = _cluster_points(a)
    right = _cluster_points(b)
    best = math.inf
    for ax, ay in left:
        for bx, by in right:
            best = min(best, _dist(ax, ay, bx, by))
    return best


def _cluster_points(cluster: EndCluster) -> list[tuple[float, float]]:
    points = [(end.x, end.y) for end in cluster.ends]
    if cluster.junction is not None:
        points.append((cluster.junction.x, cluster.junction.y))
    return points


def _can_merge(a: EndCluster, b: EndCluster) -> bool:
    if a.junction is not None and b.junction is not None:
        return False
    junction = a.junction or b.junction
    ends = a.ends + b.ends
    if junction is not None:
        if not ends:
            return False
        return all(
            _dist(end.x, end.y, junction.x, junction.y) <= JOIN_M + 1e-6 for end in ends
        )
    if len(ends) < 2:
        return False
    return _max_pairwise([(end.x, end.y) for end in ends]) <= JOIN_M + 1e-6


def _key_rank(key: str) -> tuple[int, str]:
    if key.startswith("j:"):
        rank = 0
    elif key.startswith("c:"):
        rank = 1
    elif key.startswith("d:"):
        rank = 2
    else:
        rank = 3
    return rank, key


def _read_json(path: Path, errors: list[str]) -> dict | None:
    if not path.exists():
        errors.append(f"{path.name}: file is missing")
        return None
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError):
        errors.append(f"{path.name}: not valid JSON")
        return None
    if not isinstance(data, dict):
        errors.append(f"{path.name}: not valid JSON")
        return None
    return data


def _read_csv(path: Path, required: tuple[str, ...], errors: list[str]) -> list[dict[str, str]] | None:
    if not path.exists():
        errors.append(f"{path.name}: file is missing")
        return None
    try:
        text = path.read_text(encoding="utf-8-sig")
        rows = list(csv.DictReader(text.splitlines()))
    except (OSError, UnicodeError, csv.Error):
        errors.append(f"{path.name}: not valid CSV")
        return None
    if rows:
        fieldnames = list(rows[0].keys())
    else:
        # DictReader on a header-only file yields no rows. Re-read the header.
        try:
            header = text.splitlines()[0] if text.strip() else ""
            fieldnames = next(csv.reader([header])) if header else []
        except (csv.Error, StopIteration):
            errors.append(f"{path.name}: not valid CSV")
            return None
    missing = False
    for column in required:
        if column not in fieldnames:
            errors.append(f"{path.name}: missing column {column}")
            missing = True
    if missing:
        return None
    return rows


def _id_ok(value: str, kind: str) -> bool:
    prefix = KIND_PREFIX[kind]
    if not value.startswith(prefix + "_"):
        return False
    number = value[len(prefix) + 1 :]
    return number.isdigit() and len(number) >= 3


def _feature_id(feature: dict, errors: list[str]) -> str | None:
    props = feature.get("properties") or {}
    prop_id = props.get("id")
    top_id = feature.get("id")
    if prop_id is not None and top_id is not None and str(prop_id) != str(top_id):
        errors.append(f"{prop_id}: properties.id and feature id differ")
    if prop_id is None and top_id is None:
        errors.append("ghs.geojson: a feature is missing an id")
        return None
    return str(prop_id if prop_id is not None else top_id)


def _resolved_id(feature: dict) -> str:
    props = feature.get("properties") or {}
    if props.get("id") is not None:
        return str(props["id"])
    if feature.get("id") is not None:
        return str(feature["id"])
    return ""


def _coord_pair(value: object, owner: str, errors: list[str]) -> tuple[float, float] | None:
    if not isinstance(value, (list, tuple)) or len(value) < 2:
        errors.append(f"{owner}: geometry must be a coordinate pair")
        return None
    try:
        return float(value[0]), float(value[1])
    except (TypeError, ValueError):
        errors.append(f"{owner}: geometry must be a coordinate pair")
        return None


def _clean_xy(points: list[tuple[float, float]]) -> list[tuple[float, float]]:
    if not points:
        return []
    cleaned = [points[0]]
    for point in points[1:]:
        if _dist(point[0], point[1], cleaned[-1][0], cleaned[-1][1]) >= CLEAN_M:
            cleaned.append(point)
    return cleaned


def _load_inputs(
    map_dir: Path, repo_root: Path, errors: list[str]
) -> tuple[dict | None, list[dict[str, str]] | None, list[dict[str, str]] | None, list[dict[str, str]]]:
    drawing = _read_json(map_dir / "ghs.geojson", errors)
    places = _read_csv(map_dir / "places.csv", PLACE_COLUMNS, errors)
    photos = _read_csv(map_dir / "photo-maps.csv", PHOTO_COLUMNS, errors)
    ledger_path = map_dir / "id-ledger.csv"
    ledger: list[dict[str, str]] = []
    if ledger_path.exists():
        loaded = _read_csv(ledger_path, ("id", "kind"), errors)
        if loaded is not None:
            ledger = loaded
    if drawing is not None and drawing.get("type") != "FeatureCollection":
        errors.append("ghs.geojson: not valid JSON")
    if drawing is not None and "crs" in drawing:
        errors.append("ghs.geojson: crs member is not allowed")
    return drawing, places, photos, ledger


def _parse(
    drawing: dict,
    place_rows: list[dict[str, str]],
    photo_rows: list[dict[str, str]],
    ledger_rows: list[dict[str, str]],
    repo_root: Path,
    errors: list[str],
) -> tuple[list[dict], list[Place], list[PhotoMap], dict[str, str]]:
    seen: dict[str, str] = {}
    features = drawing.get("features")
    if not isinstance(features, list):
        errors.append("ghs.geojson: not valid JSON")
        features = []

    for feature in features:
        if not isinstance(feature, dict):
            errors.append("ghs.geojson: not valid JSON")
            continue
        fid = _feature_id(feature, errors)
        props = feature.get("properties") or {}
        feature_name = props.get("feature")
        if fid is None:
            continue
        if feature_name not in FEATURE_KIND:
            if feature_name is None:
                errors.append(f"{fid}: feature is missing")
            else:
                errors.append(f"{fid}: feature '{feature_name}' is not allowed")
            continue
        kind = FEATURE_KIND[str(feature_name)]
        if not _id_ok(fid, kind):
            prefix = KIND_PREFIX[kind]
            if fid.startswith(prefix + "_"):
                errors.append(f"{fid}: id is wrongly shaped")
            else:
                errors.append(f"{fid}: prefix does not match feature {feature_name}")
        if fid in seen:
            errors.append(f"{fid}: duplicated id")
        else:
            seen[fid] = kind
        geom = feature.get("geometry") or {}
        geom_type = geom.get("type")
        if geom_type != GEOM_TYPE[kind]:
            errors.append(f"{fid}: geometry must be a {GEOM_TYPE[kind]}")
            continue
        if kind == "lane":
            access = props.get("access")
            if access is None or str(access).strip() == "":
                errors.append(f"{fid}: access is missing")
            elif str(access) not in ACCESS_VALUES:
                errors.append(f"{fid}: access '{access}' is not allowed")
            coords = geom.get("coordinates")
            if not isinstance(coords, list) or len(coords) < 2:
                errors.append(f"{fid}: geometry must be a LineString")
        elif kind == "block":
            _check_polygon(fid, geom.get("coordinates"), errors)
        elif kind in ("door", "junction"):
            _coord_pair(geom.get("coordinates"), fid, errors)

    places: list[Place] = []
    names: dict[str, str] = {}
    door_owner: dict[str, str] = {}
    for row in place_rows:
        pid = (row.get("place_id") or "").strip()
        if not _id_ok(pid, "place"):
            errors.append(f"{pid or 'places.csv'}: id is wrongly shaped")
            continue
        if pid in seen:
            errors.append(f"{pid}: duplicated id")
        else:
            seen[pid] = "place"
        name = (row.get("name") or "").strip()
        folded = name.casefold()
        if folded in names:
            errors.append(f"{pid}: name '{name}' is already used")
        elif name:
            names[folded] = pid
        ptype = (row.get("type") or "").strip()
        if ptype not in PLACE_TYPES:
            errors.append(f"{pid}: type '{ptype}' is not allowed")
        access = (row.get("access") or "").strip()
        if access == "":
            errors.append(f"{pid}: access is missing")
        elif access not in ACCESS_VALUES:
            errors.append(f"{pid}: access '{access}' is not allowed")
        start_raw = row.get("start")
        if start_raw is None or str(start_raw).strip() == "":
            errors.append(f"{pid}: start is blank")
            start = False
        elif str(start_raw).strip() not in ("yes", "no"):
            errors.append(f"{pid}: start is blank")
            start = False
        else:
            start = str(start_raw).strip() == "yes"
        door_ids = [part.strip() for part in (row.get("door_ids") or "").split(";") if part.strip()]
        if not door_ids:
            errors.append(f"{pid}: place has no door")
        for door_id in door_ids:
            if not _id_ok(door_id, "door"):
                errors.append(f"{door_id}: id is wrongly shaped")
            if door_id in door_owner:
                errors.append(f"{door_id}: listed more than once")
            else:
                door_owner[door_id] = pid
        places.append(
            Place(
                place_id=pid,
                name=name,
                type=ptype,
                access=access,
                door_ids=door_ids,
                start=start,
            )
        )

    if not any(place.start for place in places):
        errors.append("places.csv: there is no start=yes place")

    drawn_doors = {
        _resolved_id(feature)
        for feature in features
        if isinstance(feature, dict) and (feature.get("properties") or {}).get("feature") == "door"
    }
    drawn_junctions = {
        _resolved_id(feature)
        for feature in features
        if isinstance(feature, dict) and (feature.get("properties") or {}).get("feature") == "junction"
    }
    for door_id, place_id in door_owner.items():
        if _id_ok(door_id, "door") and door_id not in drawn_doors:
            errors.append(f"{door_id}: listed door does not exist")
    for door_id in drawn_doors:
        if door_id not in door_owner:
            errors.append(f"{door_id}: drawn door is on no place")

    place_ids = {place.place_id for place in places}
    for feature in features:
        if not isinstance(feature, dict):
            continue
        props = feature.get("properties") or {}
        if props.get("feature") != "block":
            continue
        fid = str(props.get("id") or "")
        place_id = props.get("place_id")
        if place_id is not None and str(place_id).strip() != "" and str(place_id) not in place_ids:
            errors.append(f"{fid}: place_id {place_id} does not exist")

    photos: list[PhotoMap] = []
    photo_dir = repo_root / "app" / "Assets" / "navigay" / "PhotoMaps"
    for row in photo_rows:
        pid = (row.get("pmap_id") or "").strip()
        if not _id_ok(pid, "pmap"):
            errors.append(f"{pid or 'photo-maps.csv'}: id is wrongly shaped")
            continue
        if pid in seen:
            errors.append(f"{pid}: duplicated id")
        else:
            seen[pid] = "pmap"
        filename = (row.get("file") or "").strip()
        if filename == "" or "/" in filename or "\\" in filename:
            errors.append(f"{pid}: file {filename or '(blank)'} is missing")
        elif not (photo_dir / filename).is_file():
            errors.append(f"{pid}: file {filename} is missing")
        numbers: dict[str, float] = {}
        blank = False
        for column in ("x", "y", "z", "yaw_deg", "load_radius_m"):
            raw = (row.get(column) or "").strip()
            if raw == "":
                blank = True
                continue
            try:
                numbers[column] = float(raw)
            except ValueError:
                blank = True
        if blank or len(numbers) != 5:
            errors.append(f"{pid}: placement is blank")
            continue
        junction_ids = [
            part.strip() for part in (row.get("junction_ids") or "").split(";") if part.strip()
        ]
        if not junction_ids:
            errors.append(f"{pid}: junction does not exist")
        for junction_id in junction_ids:
            if junction_id not in drawn_junctions:
                errors.append(f"{pid}: junction {junction_id} does not exist")
        photos.append(
            PhotoMap(
                pmap_id=pid,
                file=filename,
                x=numbers["x"],
                y=numbers["y"],
                z=numbers["z"],
                yaw_deg=numbers["yaw_deg"],
                load_radius_m=numbers["load_radius_m"],
                junction_ids=junction_ids,
            )
        )

    ledger_kind = {row["id"].strip(): row["kind"].strip() for row in ledger_rows if row.get("id")}
    for fid, kind in seen.items():
        if fid in ledger_kind and ledger_kind[fid] != kind:
            errors.append(f"{fid}: ledger kind is {ledger_kind[fid]}")

    return features, places, photos, ledger_kind


def _check_polygon(fid: str, coordinates: object, errors: list[str]) -> None:
    if not isinstance(coordinates, list) or not coordinates:
        errors.append(f"{fid}: geometry must be a Polygon")
        return
    for ring in coordinates:
        if not isinstance(ring, list) or len(ring) < 4:
            errors.append(f"{fid}: geometry must be a closed polygon")
            return
        first = ring[0]
        last = ring[-1]
        if first != last:
            errors.append(f"{fid}: geometry must be a closed polygon")
            return
    try:
        shell = [(float(p[0]), float(p[1])) for p in coordinates[0]]
        holes = [[(float(p[0]), float(p[1])) for p in ring] for ring in coordinates[1:]]
        polygon = Polygon(shell, holes)
    except (TypeError, ValueError, IndexError):
        errors.append(f"{fid}: polygon is self-crossing")
        return
    if not polygon.is_valid:
        errors.append(f"{fid}: polygon is self-crossing ({explain_validity(polygon)})")


def _resolve_origin(map_dir: Path, lonlats: list[tuple[float, float]], errors: list[str]) -> tuple[Origin | None, bool]:
    path = map_dir / "origin.json"
    if not path.exists():
        if not lonlats:
            errors.append("ghs.geojson: a feature is missing an id")
            return None, False
        lon = sum(point[0] for point in lonlats) / len(lonlats)
        lat = sum(point[1] for point in lonlats) / len(lonlats)
        return Origin(lat, lon), False
    data = _read_json(path, errors)
    if data is None:
        return None, True
    if "lat" not in data or "lon" not in data:
        errors.append("origin.json: lat and lon are required")
        return None, True
    try:
        return Origin(float(data["lat"]), float(data["lon"])), True
    except (TypeError, ValueError):
        errors.append("origin.json: lat and lon are required")
        return None, True


def _collect_lonlats(features: list[dict]) -> list[tuple[float, float]]:
    points: list[tuple[float, float]] = []

    def walk(value: object) -> None:
        if isinstance(value, (list, tuple)):
            if len(value) >= 2 and isinstance(value[0], (int, float)) and isinstance(value[1], (int, float)):
                points.append((float(value[0]), float(value[1])))
                return
            for item in value:
                walk(item)

    for feature in features:
        if isinstance(feature, dict):
            walk((feature.get("geometry") or {}).get("coordinates"))
    return points


def _build_geometry(
    features: list[dict],
    places: list[Place],
    origin: Origin,
    errors: list[str],
) -> tuple[list[Lane], list[Junction], list[Door], list[Block]]:
    lanes: list[Lane] = []
    junctions: list[Junction] = []
    doors: list[Door] = []
    blocks: list[Block] = []
    place_of = {door_id: place.place_id for place in places for door_id in place.door_ids}
    place_name = {place.place_id: place.name for place in places}

    for feature in features:
        props = feature.get("properties") or {}
        kind_name = props.get("feature")
        if kind_name not in FEATURE_KIND:
            continue
        fid = _resolved_id(feature)
        geom = feature.get("geometry") or {}
        if geom.get("type") != GEOM_TYPE[FEATURE_KIND[str(kind_name)]]:
            continue
        if kind_name == "lane":
            raw = []
            coords = geom.get("coordinates")
            if not isinstance(coords, list):
                continue
            bad = False
            for pair in coords:
                parsed = _coord_pair(pair, fid, errors)
                if parsed is None:
                    bad = True
                    break
                raw.append(to_xy(parsed[0], parsed[1], origin))
            if bad:
                continue
            cleaned = _clean_xy(raw)
            if len(cleaned) < 2:
                errors.append(f"{fid}: geometry must be a LineString")
                continue
            lanes.append(
                Lane(
                    id=fid,
                    access=str(props.get("access")),
                    verts=[Vertex(x, y) for x, y in cleaned],
                )
            )
        elif kind_name in ("door", "junction"):
            parsed = _coord_pair(geom.get("coordinates"), fid, errors)
            if parsed is None:
                continue
            xy = to_xy(parsed[0], parsed[1], origin)
            if kind_name == "door":
                doors.append(Door(fid, xy[0], xy[1], parsed[1], parsed[0], place_of.get(fid, "")))
            else:
                landmark = props.get("landmark")
                junctions.append(
                    Junction(
                        fid,
                        xy[0],
                        xy[1],
                        parsed[1],
                        parsed[0],
                        None if landmark is None or str(landmark).strip() == "" else str(landmark),
                    )
                )
        elif kind_name == "block":
            rings_ll = geom.get("coordinates")
            if not isinstance(rings_ll, list):
                continue
            rings_xy: list[list[tuple[float, float]]] = []
            keep = True
            for ring in rings_ll:
                converted = []
                for pair in ring:
                    parsed = _coord_pair(pair, fid, errors)
                    if parsed is None:
                        keep = False
                        break
                    converted.append(to_xy(parsed[0], parsed[1], origin))
                if not keep:
                    break
                cleaned = _clean_xy(converted)
                if len(cleaned) >= 2 and _dist(cleaned[0][0], cleaned[0][1], cleaned[-1][0], cleaned[-1][1]) > 1e-6:
                    cleaned.append(cleaned[0])
                rings_xy.append(cleaned)
            if not keep or not rings_xy:
                continue
            place_id = props.get("place_id")
            place_id_s = None if place_id is None or str(place_id).strip() == "" else str(place_id)
            blocks.append(
                Block(
                    id=fid,
                    place_id=place_id_s,
                    name=place_name.get(place_id_s) if place_id_s else None,
                    rings_xy=rings_xy,
                    rings_ll=rings_ll,
                )
            )

    reported: set[str] = set()

    def flag_far(fid: str, x: float, y: float) -> None:
        if fid not in reported and math.hypot(x, y) > FAR_M:
            reported.add(fid)
            errors.append(f"{fid}: point is more than 1500 m from the origin")

    for lane in lanes:
        for vertex in lane.verts:
            flag_far(lane.id, vertex.x, vertex.y)
    for door in doors:
        flag_far(door.id, door.x, door.y)
    for junction in junctions:
        flag_far(junction.id, junction.x, junction.y)
    for block in blocks:
        for ring in block.rings_xy:
            for x, y in ring:
                flag_far(block.id, x, y)
    return lanes, junctions, doors, blocks


def _weld_ends(lanes: list[Lane], junctions: list[Junction]) -> None:
    ends: list[EndPoint] = []
    for index, lane in enumerate(lanes):
        ends.append(EndPoint(index, 0, lane.verts[0].x, lane.verts[0].y))
        ends.append(EndPoint(index, 1, lane.verts[-1].x, lane.verts[-1].y))
    clusters = [EndCluster([end]) for end in ends]
    clusters.extend(EndCluster([], junction) for junction in junctions)
    while True:
        best: tuple[float, int, int, int] | None = None
        for i in range(len(clusters)):
            for j in range(i + 1, len(clusters)):
                if not _can_merge(clusters[i], clusters[j]):
                    continue
                score = _min_sep(clusters[i], clusters[j])
                tie = 0 if (clusters[i].junction or clusters[j].junction) else 1
                rec = (score, tie, i, j)
                if best is None or rec < best:
                    best = rec
        if best is None:
            break
        _, _, i, j = best
        left, right = clusters[i], clusters[j]
        clusters[i] = EndCluster(left.ends + right.ends, left.junction or right.junction)
        clusters.pop(j)

    cluster_number = 0
    for cluster in clusters:
        if not cluster.ends:
            continue
        if cluster.junction is not None:
            key = f"j:{cluster.junction.id}"
            xy = (cluster.junction.x, cluster.junction.y)
        else:
            cluster_number += 1
            key = f"c:{cluster_number}"
            xy = (
                sum(end.x for end in cluster.ends) / len(cluster.ends),
                sum(end.y for end in cluster.ends) / len(cluster.ends),
            )
        for end in cluster.ends:
            vertex = lanes[end.lane_index].verts[0 if end.which == 0 else -1]
            vertex.x, vertex.y = xy
            vertex.key = key


def _iter_cross_points(geom: object) -> list[tuple[float, float]]:
    points: list[tuple[float, float]] = []
    if geom is None or getattr(geom, "is_empty", True):
        return points
    kind = geom.geom_type
    if kind == "Point":
        points.append((float(geom.x), float(geom.y)))
    elif kind in ("MultiPoint", "GeometryCollection", "MultiLineString"):
        for part in geom.geoms:
            points.extend(_iter_cross_points(part))
    elif kind == "LineString":
        coords = list(geom.coords)
        if coords:
            points.append((float(coords[0][0]), float(coords[0][1])))
            if len(coords) > 1:
                points.append((float(coords[-1][0]), float(coords[-1][1])))
    return points


def _insert_vertex(lane: Lane, x: float, y: float, key: str) -> None:
    for vertex in lane.verts:
        if _dist(vertex.x, vertex.y, x, y) <= CLEAN_M:
            if vertex.key is None:
                vertex.key = key
            return
    best_i = 0
    best_d = math.inf
    for index in range(len(lane.verts) - 1):
        dist, _, _, _ = _segment_hit(
            x,
            y,
            lane.verts[index].x,
            lane.verts[index].y,
            lane.verts[index + 1].x,
            lane.verts[index + 1].y,
        )
        if dist < best_d:
            best_d = dist
            best_i = index
    if best_d > CLEAN_M:
        return
    lane.verts.insert(best_i + 1, Vertex(x, y, key))


def _split_crossings(lanes: list[Lane]) -> None:
    lines = []
    for lane in lanes:
        if len(lane.verts) < 2:
            lines.append(None)
            continue
        lines.append(LineString([(v.x, v.y) for v in lane.verts]))
    for i in range(len(lanes)):
        if lines[i] is None:
            continue
        for j in range(i + 1, len(lanes)):
            if lines[j] is None:
                continue
            for x, y in _iter_cross_points(lines[i].intersection(lines[j])):
                key_i = _key_near(lanes[i], x, y)
                key_j = _key_near(lanes[j], x, y)
                if key_i and key_j:
                    continue
                shared = key_i or key_j or f"x:{x:.4f}:{y:.4f}"
                if key_i is None:
                    _insert_vertex(lanes[i], x, y, shared)
                if key_j is None:
                    _insert_vertex(lanes[j], x, y, shared)


def _key_near(lane: Lane, x: float, y: float) -> str | None:
    for vertex in lane.verts:
        if vertex.key and _dist(vertex.x, vertex.y, x, y) <= CLEAN_M:
            return vertex.key
    return None


def _attached_junction_ids(lanes: list[Lane]) -> set[str]:
    found = set()
    for lane in lanes:
        for vertex in lane.verts:
            if vertex.key and vertex.key.startswith("j:"):
                found.add(vertex.key[2:])
    return found


def _split_junctions(lanes: list[Lane], junctions: list[Junction], errors: list[str]) -> None:
    already = _attached_junction_ids(lanes)
    for junction in junctions:
        if junction.id in already:
            continue
        best: tuple[float, int, int] | None = None
        for lane_index, lane in enumerate(lanes):
            for seg in range(len(lane.verts) - 1):
                dist, _, _, _ = _segment_hit(
                    junction.x,
                    junction.y,
                    lane.verts[seg].x,
                    lane.verts[seg].y,
                    lane.verts[seg + 1].x,
                    lane.verts[seg + 1].y,
                )
                if best is None or dist < best[0]:
                    best = (dist, lane_index, seg)
        if best is None or best[0] > JOIN_M + 1e-6:
            errors.append(f"{junction.id}: drawn junction is more than 2 m from every lane")
            continue
        _, lane_index, seg = best
        lane = lanes[lane_index]
        reused = False
        for vertex in lane.verts:
            if _dist(vertex.x, vertex.y, junction.x, junction.y) <= CLEAN_M:
                vertex.x, vertex.y = junction.x, junction.y
                vertex.key = f"j:{junction.id}"
                reused = True
                break
        if not reused:
            # Weld the split point to the junction, so the node keeps the drawn id.
            lane.verts.insert(seg + 1, Vertex(junction.x, junction.y, f"j:{junction.id}"))
        for other in lanes:
            for vertex in other.verts:
                if _dist(vertex.x, vertex.y, junction.x, junction.y) <= CLEAN_M:
                    vertex.x, vertex.y = junction.x, junction.y
                    vertex.key = f"j:{junction.id}"


def _attach_doors(lanes: list[Lane], doors: list[Door], errors: list[str]) -> list[DoorLink]:
    links: list[DoorLink] = []
    for door in doors:
        best: tuple[float, int, float, float] | None = None
        for lane_index, lane in enumerate(lanes):
            for seg in range(len(lane.verts) - 1):
                dist, _, qx, qy = _segment_hit(
                    door.x,
                    door.y,
                    lane.verts[seg].x,
                    lane.verts[seg].y,
                    lane.verts[seg + 1].x,
                    lane.verts[seg + 1].y,
                )
                if best is None or dist < best[0]:
                    best = (dist, lane_index, qx, qy)
        if best is None or best[0] > DOOR_MAX_M + 1e-6:
            errors.append(f"{door.id}: door is more than 15 m from every lane")
            continue
        _, lane_index, qx, qy = best
        reuse: tuple[float, Vertex, Lane] | None = None
        for lane in lanes:
            for vertex in lane.verts:
                gap = _dist(vertex.x, vertex.y, qx, qy)
                if gap <= NODE_REUSE_M + 1e-6 and (reuse is None or gap < reuse[0]):
                    reuse = (gap, vertex, lane)
        if reuse is not None:
            _, vertex, lane = reuse
            if vertex.key is None:
                vertex.key = f"x:{vertex.x:.4f}:{vertex.y:.4f}"
            links.append(DoorLink(door, vertex.key, lane.access))
            continue
        lane = lanes[lane_index]
        key = f"x:{qx:.4f}:{qy:.4f}"
        _insert_vertex(lane, qx, qy, key)
        links.append(DoorLink(door, key, lane.access))
    return links


def _canonicalize(lanes: list[Lane], links: list[DoorLink]) -> None:
    keyed: list[Vertex] = []
    for lane in lanes:
        for vertex in lane.verts:
            if vertex.key:
                keyed.append(vertex)
    parent = {vertex.key: vertex.key for vertex in keyed if vertex.key}

    def find(key: str) -> str:
        while parent[key] != key:
            parent[key] = parent[parent[key]]
            key = parent[key]
        return key

    def union(a: str, b: str) -> None:
        ra, rb = find(a), find(b)
        if ra == rb:
            return
        if _key_rank(ra) <= _key_rank(rb):
            parent[rb] = ra
        else:
            parent[ra] = rb

    for i in range(len(keyed)):
        for j in range(i + 1, len(keyed)):
            if _dist(keyed[i].x, keyed[i].y, keyed[j].x, keyed[j].y) <= CLEAN_M:
                union(keyed[i].key or "", keyed[j].key or "")
    anchor: dict[str, Vertex] = {}
    for vertex in keyed:
        root = find(vertex.key or "")
        current = anchor.get(root)
        if current is None or _key_rank(vertex.key or "") < _key_rank(current.key or ""):
            anchor[root] = vertex
    for vertex in keyed:
        root = find(vertex.key or "")
        chosen = anchor[root]
        vertex.key = chosen.key
        if chosen.key and chosen.key.startswith("j:"):
            vertex.x, vertex.y = chosen.x, chosen.y
    for link in links:
        if link.lane_key in parent:
            root = find(link.lane_key)
            chosen = anchor.get(root)
            if chosen and chosen.key:
                link.lane_key = chosen.key


def _polyline_length(points: list[tuple[float, float]]) -> float:
    if len(points) < 2:
        return 0.0
    return float(LineString(points).length)


def _graph_node(
    nid: str,
    kind: str,
    x: float,
    y: float,
    lat: float,
    lon: float,
    place_id: str | None,
    landmark: str | None,
) -> dict:
    return {
        "id": nid,
        "kind": kind,
        "x": round(x, 3),
        "y": round(y, 3),
        "lat": round(lat, 7),
        "lon": round(lon, 7),
        "place_id": place_id,
        "landmark": landmark,
        "edge_ids": [],
    }


def _assemble(
    lanes: list[Lane],
    junctions: list[Junction],
    doors: list[Door],
    links: list[DoorLink],
    origin: Origin,
) -> tuple[list[dict], list[dict]]:
    junction_by_id = {junction.id: junction for junction in junctions}
    door_by_id = {door.id: door for door in doors}
    raw_edges: list[dict] = []
    for lane in lanes:
        indexes = [i for i, vertex in enumerate(lane.verts) if vertex.key]
        for left, right in zip(indexes, indexes[1:]):
            if lane.verts[left].key == lane.verts[right].key:
                continue
            poly = [(lane.verts[i].x, lane.verts[i].y) for i in range(left, right + 1)]
            raw_edges.append(
                {
                    "kind": "lane",
                    "from_key": lane.verts[left].key,
                    "to_key": lane.verts[right].key,
                    "lane_id": lane.id,
                    "access": lane.access,
                    "polyline": poly,
                }
            )
    for link in links:
        door = link.door
        target = _vertex_xy(lanes, link.lane_key)
        if target is None:
            continue
        raw_edges.append(
            {
                "kind": "door",
                "from_key": link.lane_key,
                "to_key": f"d:{door.id}",
                "lane_id": None,
                "access": link.access,
                "polyline": [target, (door.x, door.y)],
            }
        )

    keys: set[str] = set()
    for edge in raw_edges:
        keys.add(edge["from_key"])
        keys.add(edge["to_key"])
    for link in links:
        keys.add(f"d:{link.door.id}")

    autos = []
    for key in keys:
        if key.startswith("j:") or key.startswith("d:"):
            continue
        xy = _vertex_xy(lanes, key)
        if xy is None:
            continue
        autos.append((round(xy[0], 4), round(xy[1], 4), key))
    autos.sort()
    auto_id = {key: f"auto_{index:04d}" for index, (_, _, key) in enumerate(autos, start=1)}

    def node_id(key: str) -> str:
        if key.startswith("j:"):
            return key[2:]
        if key.startswith("d:"):
            return key[2:]
        return auto_id[key]

    node_xy: dict[str, tuple[float, float]] = {}
    nodes: dict[str, dict] = {}
    for key in keys:
        nid = node_id(key)
        if key.startswith("j:"):
            junction = junction_by_id[key[2:]]
            xy = (junction.x, junction.y)
            nodes[nid] = _graph_node(nid, "junction", xy[0], xy[1], junction.lat, junction.lon, None, junction.landmark)
        elif key.startswith("d:"):
            door = door_by_id[key[2:]]
            xy = (door.x, door.y)
            nodes[nid] = _graph_node(nid, "door", xy[0], xy[1], door.lat, door.lon, door.place_id, None)
        else:
            xy = _vertex_xy(lanes, key) or (0.0, 0.0)
            lon, lat = to_lonlat(xy[0], xy[1], origin)
            nodes[nid] = _graph_node(nid, "auto", xy[0], xy[1], lat, lon, None, None)
        node_xy[nid] = xy

    sortable = []
    for edge in raw_edges:
        poly = edge["polyline"]
        sortable.append(
            (
                round(poly[0][0], 4),
                round(poly[0][1], 4),
                round(poly[-1][0], 4),
                round(poly[-1][1], 4),
                edge["lane_id"] or "",
                edge["kind"],
                edge,
            )
        )
    sortable.sort()
    edges = []
    for index, item in enumerate(sortable, start=1):
        edge = item[-1]
        eid = f"edge_{index:04d}"
        record = {
            "id": eid,
            "kind": edge["kind"],
            "from": node_id(edge["from_key"]),
            "to": node_id(edge["to_key"]),
            "lane_id": edge["lane_id"],
            "access": edge["access"],
            "length_m": round(_polyline_length(edge["polyline"]), 3),
            "polyline": [[round(x, 3), round(y, 3)] for x, y in edge["polyline"]],
        }
        edges.append(record)
        nodes[record["from"]]["edge_ids"].append(eid)
        nodes[record["to"]]["edge_ids"].append(eid)
    for node in nodes.values():
        node["edge_ids"] = sorted(node["edge_ids"])
    return [nodes[key] for key in sorted(nodes)], edges


def _vertex_xy(lanes: list[Lane], key: str) -> tuple[float, float] | None:
    for lane in lanes:
        for vertex in lane.verts:
            if vertex.key == key:
                return vertex.x, vertex.y
    return None


def _reachable(edges: list[dict], sources: list[str]) -> set[str]:
    neighbours: dict[str, set[str]] = {}
    for edge in edges:
        neighbours.setdefault(edge["from"], set()).add(edge["to"])
        neighbours.setdefault(edge["to"], set()).add(edge["from"])
    seen: set[str] = set()
    stack = list(sources)
    while stack:
        current = stack.pop()
        if current in seen:
            continue
        seen.add(current)
        stack.extend(neighbours.get(current, ()))
    return seen


def _warnings(
    lanes: list[Lane],
    blocks: list[Block],
    nodes: list[dict],
    edges: list[dict],
    origin: Origin,
) -> list[dict]:
    features: list[dict] = []
    degree: dict[str, int] = {}
    for edge in edges:
        degree[edge["from"]] = degree.get(edge["from"], 0) + 1
        degree[edge["to"]] = degree.get(edge["to"], 0) + 1
    lane_lines = {
        lane.id: LineString([(v.x, v.y) for v in lane.verts])
        for lane in lanes
        if len(lane.verts) >= 2
    }
    for node in nodes:
        if node["kind"] == "door" or degree.get(node["id"], 0) != 1:
            continue
        own = {
            edge["lane_id"]
            for edge in edges
            if edge["lane_id"] and node["id"] in (edge["from"], edge["to"])
        }
        point = Point(node["x"], node["y"])
        for lane_id, line in lane_lines.items():
            if lane_id in own:
                continue
            if line.distance(point) <= DEAD_END_M + 1e-6:
                lon, lat = to_lonlat(node["x"], node["y"], origin)
                features.append(
                    _problem(
                        "dead_end",
                        f"{node['id']}: dead end within 3 m of {lane_id}",
                        [node["id"], lane_id],
                        {"type": "Point", "coordinates": [round(lon, 7), round(lat, 7)]},
                    )
                )
                break
    for edge in edges:
        if edge["kind"] == "lane" and edge["length_m"] < SHORT_EDGE_M:
            features.append(
                _problem(
                    "short_lane",
                    f"{edge['lane_id']}: lane edge {edge['id']} is shorter than 1 m",
                    [edge["lane_id"], edge["id"]],
                    _line_ll(edge["polyline"], origin),
                )
            )
    for lane in lanes:
        line = lane_lines.get(lane.id)
        if line is None:
            continue
        for block in blocks:
            if len(block.rings_xy[0]) < 4:
                continue
            try:
                polygon = Polygon(block.rings_xy[0], block.rings_xy[1:])
            except (ValueError, TypeError):
                continue
            if not polygon.is_valid or polygon.is_empty:
                continue
            inset = polygon.buffer(-CLEAN_M)
            if inset.is_empty or not line.intersects(inset):
                continue
            features.append(
                _problem(
                    "crosses_block",
                    f"{lane.id}: lane cuts through {block.id}",
                    [lane.id, block.id],
                    {"type": "LineString", "coordinates": [[lon, lat] for lon, lat in _lane_ll(lane, origin)]},
                )
            )
    for block in blocks:
        if block.place_id is None:
            features.append(
                _problem(
                    "unnamed_block",
                    f"{block.id}: block has no place_id",
                    [block.id],
                    {"type": "Polygon", "coordinates": block.rings_ll},
                )
            )
    features.sort(key=lambda item: (item["properties"]["problem"], item["properties"]["ids"]))
    return features


def _lane_ll(lane: Lane, origin: Origin) -> list[tuple[float, float]]:
    return [tuple(round(v, 7) for v in to_lonlat(vertex.x, vertex.y, origin)) for vertex in lane.verts]


def _line_ll(polyline: list[list[float]], origin: Origin) -> dict:
    coords = []
    for x, y in polyline:
        lon, lat = to_lonlat(x, y, origin)
        coords.append([round(lon, 7), round(lat, 7)])
    return {"type": "LineString", "coordinates": coords}


def _problem(problem: str, message: str, ids: list[str], geometry: dict) -> dict:
    return {
        "type": "Feature",
        "properties": {"problem": problem, "message": message, "ids": ids},
        "geometry": geometry,
    }


def _write_ledger(path: Path, seen: dict[str, str], existing: list[dict[str, str]]) -> None:
    known = {row["id"].strip() for row in existing if row.get("id")}
    fresh = [(fid, kind) for fid, kind in sorted(seen.items(), key=lambda item: (item[1], item[0])) if fid not in known]
    if not path.exists():
        with path.open("w", encoding="utf-8", newline="") as handle:
            writer = csv.writer(handle)
            writer.writerow(["id", "kind", "note"])
            for fid, kind in sorted(seen.items(), key=lambda item: (item[1], item[0])):
                writer.writerow([fid, kind, ""])
        return
    if not fresh:
        return
    text = path.read_text(encoding="utf-8")
    separator = "" if text.endswith("\n") or text == "" else "\n"
    with path.open("a", encoding="utf-8", newline="") as handle:
        handle.write(separator)
        writer = csv.writer(handle)
        for fid, kind in fresh:
            writer.writerow([fid, kind, ""])


def build(map_dir: Path, *, repo_root: Path | None = None, install: bool = False) -> dict:
    map_dir = Path(map_dir)
    repo_root = Path(repo_root) if repo_root is not None else map_dir.parent
    errors: list[str] = []
    drawing, place_rows, photo_rows, ledger_rows = _load_inputs(map_dir, repo_root, errors)
    if drawing is None or place_rows is None or photo_rows is None or errors:
        raise BuildFailure(errors or ["ghs.geojson: file is missing"])

    features, places, photos, _ledger_kind = _parse(
        drawing, place_rows, photo_rows, ledger_rows, repo_root, errors
    )
    if errors:
        raise BuildFailure(errors)

    origin, _from_file = _resolve_origin(map_dir, _collect_lonlats(features), errors)
    if origin is None or errors:
        raise BuildFailure(errors)

    lanes, junctions, doors, blocks = _build_geometry(features, places, origin, errors)
    if errors:
        raise BuildFailure(errors)

    _weld_ends(lanes, junctions)
    _split_crossings(lanes)
    _split_junctions(lanes, junctions, errors)
    links = _attach_doors(lanes, doors, errors)
    if errors:
        raise BuildFailure(errors)
    _canonicalize(lanes, links)
    nodes, edges = _assemble(lanes, junctions, doors, links, origin)

    start_doors = [door_id for place in places if place.start for door_id in place.door_ids]
    reached = _reachable(edges, start_doors)
    for door in doors:
        if door.id not in reached:
            errors.append(f"{door.id}: cannot be reached from a start door")
    if errors:
        raise BuildFailure(errors)

    graph = {
        "schema": 1,
        "origin": {"lat": round(origin.lat, 7), "lon": round(origin.lon, 7)},
        "metres_per_deg": {"x": METRES_PER_DEG["x"], "y": METRES_PER_DEG["y"]},
        "nodes": nodes,
        "edges": edges,
        "places": [
            {
                "place_id": place.place_id,
                "name": place.name,
                "type": place.type,
                "access": place.access,
                "door_ids": place.door_ids,
                "start": place.start,
            }
            for place in places
        ],
        "blocks": [
            {
                "id": block.id,
                "place_id": block.place_id,
                "name": block.name,
                "rings": [[[round(x, 3), round(y, 3)] for x, y in ring] for ring in block.rings_xy],
            }
            for block in blocks
        ],
        "photo_maps": [
            {
                "pmap_id": photo.pmap_id,
                "file": photo.file,
                "x": photo.x,
                "y": photo.y,
                "z": photo.z,
                "yaw_deg": photo.yaw_deg,
                "load_radius_m": photo.load_radius_m,
                "junction_ids": photo.junction_ids,
            }
            for photo in photos
        ],
    }
    problems = {
        "type": "FeatureCollection",
        "features": _warnings(lanes, blocks, nodes, edges, origin),
    }
    out_dir = map_dir / "build"
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "graph.json").write_text(json.dumps(graph, indent=2) + "\n", encoding="utf-8")
    (out_dir / "problems.geojson").write_text(json.dumps(problems, indent=2) + "\n", encoding="utf-8")
    seen = {lane.id: "lane" for lane in lanes}
    seen.update({door.id: "door" for door in doors})
    seen.update({junction.id: "junction" for junction in junctions})
    seen.update({block.id: "block" for block in blocks})
    seen.update({place.place_id: "place" for place in places})
    seen.update({photo.pmap_id: "pmap" for photo in photos})
    _write_ledger(map_dir / "id-ledger.csv", seen, ledger_rows)
    if install:
        streaming = repo_root / "app" / "Assets" / "StreamingAssets"
        streaming.mkdir(parents=True, exist_ok=True)
        (streaming / "graph.json").write_text(json.dumps(graph, indent=2) + "\n", encoding="utf-8")
    if (map_dir / "origin.json").exists():
        print("origin.json unchanged")
    else:
        print("origin.json not written (provisional map zero, not frozen)")
    print(f"nodes={len(nodes)} edges={len(edges)} warnings={len(problems['features'])}")
    return graph


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Build navigay graph.json")
    parser.add_argument("--map", type=Path, default=ROOT / "map")
    parser.add_argument("--install", action="store_true")
    args = parser.parse_args(argv)
    try:
        build(args.map, repo_root=ROOT, install=args.install)
    except BuildFailure as failure:
        print(str(failure), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
