"""Small fake maps for the graph build. The campus file is not a fixture."""

import csv
import json
import math
import re
from pathlib import Path

import pytest

from build_graph import METRES_PER_DEG, BuildFailure, Origin, build, main, to_xy, to_lonlat

ORIGIN = Origin(26.0, 75.0)


def ll(x: float, y: float) -> list[float]:
    lon, lat = to_lonlat(x, y, ORIGIN)
    return [lon, lat]


def line(fid: str, access: str, points: list[tuple[float, float]]) -> dict:
    return {
        "type": "Feature",
        "properties": {"id": fid, "feature": "lane", "access": access},
        "geometry": {"type": "LineString", "coordinates": [ll(x, y) for x, y in points]},
    }


def point(fid: str, feature: str, x: float, y: float, **extra: str) -> dict:
    props = {"id": fid, "feature": feature}
    props.update(extra)
    return {
        "type": "Feature",
        "properties": props,
        "geometry": {"type": "Point", "coordinates": ll(x, y)},
    }


def square(x0: float, y0: float, x1: float, y1: float) -> list[tuple[float, float]]:
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1), (x0, y0)]


def block(fid: str, rings: list[list[tuple[float, float]]], place_id: str | None = None) -> dict:
    props: dict[str, str] = {"id": fid, "feature": "block"}
    if place_id is not None:
        props["place_id"] = place_id
    return {
        "type": "Feature",
        "properties": props,
        "geometry": {
            "type": "Polygon",
            "coordinates": [[ll(x, y) for x, y in ring] for ring in rings],
        },
    }


def base() -> tuple[list[dict], list[tuple[str, ...]]]:
    features = [
        line("lane_001", "all", [(0, 0), (30, 0)]),
        point("door_001", "door", 15, 3),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    return features, places


def write_map(
    root: Path,
    features: list[dict],
    places: list[tuple[str, ...]],
    photos: list[tuple[str, ...]] | None = None,
    ledger: str | None = None,
    origin: bool = True,
) -> Path:
    map_dir = root / "map"
    map_dir.mkdir(parents=True, exist_ok=True)
    (map_dir / "ghs.geojson").write_text(
        json.dumps({"type": "FeatureCollection", "features": features}),
        encoding="utf-8",
    )
    with (map_dir / "places.csv").open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["place_id", "name", "type", "access", "door_ids", "start"])
        writer.writerows(places)
    with (map_dir / "photo-maps.csv").open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["pmap_id", "file", "x", "y", "z", "yaw_deg", "load_radius_m", "junction_ids"])
        writer.writerows(photos or [])
    if ledger is not None:
        (map_dir / "id-ledger.csv").write_text(ledger, encoding="utf-8")
    if origin:
        (map_dir / "origin.json").write_text(
            json.dumps({"lat": ORIGIN.lat, "lon": ORIGIN.lon, "note": "test"}),
            encoding="utf-8",
        )
    return map_dir


def build_ok(root: Path, features: list[dict], places: list[tuple[str, ...]], **kwargs: object) -> dict:
    map_dir = write_map(root, features, places, **kwargs)  # type: ignore[arg-type]
    before = (map_dir / "ghs.geojson").read_bytes()
    origin_path = map_dir / "origin.json"
    origin_before = origin_path.read_bytes() if origin_path.exists() else None
    graph = build(map_dir, repo_root=root)
    assert (map_dir / "ghs.geojson").read_bytes() == before
    if origin_before is None:
        assert not origin_path.exists()
    else:
        assert origin_path.read_bytes() == origin_before
    return graph


def build_fail(root: Path, features: list[dict], places: list[tuple[str, ...]], **kwargs: object) -> BuildFailure:
    map_dir = write_map(root, features, places, **kwargs)  # type: ignore[arg-type]
    before = (map_dir / "ghs.geojson").read_bytes()
    origin_path = map_dir / "origin.json"
    origin_before = origin_path.read_bytes() if origin_path.exists() else None
    with pytest.raises(BuildFailure) as caught:
        build(map_dir, repo_root=root)
    assert (map_dir / "ghs.geojson").read_bytes() == before
    if origin_before is None:
        assert not origin_path.exists()
    else:
        assert origin_path.read_bytes() == origin_before
    assert not (map_dir / "build" / "graph.json").exists()
    return caught.value


def test_scale_uses_the_constants_written_into_graph() -> None:
    origin = Origin(26.0, 75.0)
    _, north = to_xy(75.0, 26.0 + 10 / METRES_PER_DEG["y"], origin)
    east, _ = to_xy(
        75.0 + 10 / (math.cos(math.radians(26.0)) * METRES_PER_DEG["x"]),
        26.0,
        origin,
    )
    assert north == pytest.approx(10)
    assert east == pytest.approx(10)


def test_straight_lane_splits_for_a_door_and_keeps_schema(tmp_path: Path) -> None:
    features, places = base()
    graph = build_ok(tmp_path, features, places)
    assert graph["schema"] == 1
    assert graph["metres_per_deg"] == METRES_PER_DEG
    assert graph["origin"] == {"lat": 26.0, "lon": 75.0}
    assert graph["places"][0]["start"] is True
    assert graph["photo_maps"] == []
    door_edges = [edge for edge in graph["edges"] if edge["kind"] == "door"]
    assert len(door_edges) == 1
    assert door_edges[0]["lane_id"] is None
    assert door_edges[0]["access"] == "all"
    assert door_edges[0]["length_m"] == pytest.approx(3, abs=0.02)
    assert door_edges[0]["id"].startswith("edge_")
    door = next(node for node in graph["nodes"] if node["id"] == "door_001")
    assert door["kind"] == "door"
    assert door["place_id"] == "place_001"
    assert all(re.fullmatch(r"auto_\d{4}", node["id"]) for node in graph["nodes"] if node["kind"] == "auto")
    assert (tmp_path / "map" / "build" / "problems.geojson").exists()


def test_bend_stays_in_the_lane_polyline(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (10, 4), (20, 0)]),
        point("door_001", "door", -2, 1),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    lane_edges = [edge for edge in graph["edges"] if edge["kind"] == "lane"]
    assert len(lane_edges) == 1
    assert len(lane_edges[0]["polyline"]) == 3


def test_two_doors_on_one_place(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (30, 0)]),
        point("door_001", "door", 8, 2),
        point("door_002", "door", 22, 2),
    ]
    places = [("place_001", "Quess Mess", "food", "all", "door_001;door_002", "yes")]
    graph = build_ok(tmp_path, features, places)
    assert graph["places"][0]["door_ids"] == ["door_001", "door_002"]
    assert len([edge for edge in graph["edges"] if edge["kind"] == "door"]) == 2


def test_place_name_may_contain_a_comma(tmp_path: Path) -> None:
    features, places = base()
    map_dir = write_map(tmp_path, features, places)
    with (map_dir / "places.csv").open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["place_id", "name", "type", "access", "door_ids", "start"])
        writer.writerow(["place_001", "Dev Sweets, Snacks", "food", "all", "door_001", "yes"])
    graph = build(map_dir, repo_root=tmp_path)
    assert graph["places"][0]["name"] == "Dev Sweets, Snacks"


def test_places_bom_and_crlf(tmp_path: Path) -> None:
    features, places = base()
    map_dir = write_map(tmp_path, features, places)
    text = (map_dir / "places.csv").read_text(encoding="utf-8")
    (map_dir / "places.csv").write_bytes(b"\xef\xbb\xbf" + text.replace("\n", "\r\n").encode("utf-8"))
    graph = build(map_dir, repo_root=tmp_path)
    assert graph["places"][0]["name"] == "Main gate"


def test_gaps_farther_than_2m_stay_apart(tmp_path: Path) -> None:
    features: list[dict] = []
    gaps = (3, 19, 28, 42)
    for index, gap in enumerate(gaps):
        y = index * 100
        features.append(line(f"lane_{index * 2 + 1:03d}", "all", [(0, y), (20, y)]))
        features.append(line(f"lane_{index * 2 + 2:03d}", "all", [(20 + gap, y), (50 + gap, y)]))
    features.append(point("door_001", "door", 10, 3))
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    for index, gap in enumerate(gaps):
        y = index * 100
        left = [node for node in graph["nodes"] if abs(node["y"] - y) < 0.2 and abs(node["x"] - 20) < 0.2]
        right = [
            node for node in graph["nodes"] if abs(node["y"] - y) < 0.2 and abs(node["x"] - (20 + gap)) < 0.2
        ]
        assert len(left) == 1 and len(right) == 1
        assert left[0]["id"] != right[0]["id"]
        assert math.hypot(left[0]["x"] - right[0]["x"], left[0]["y"] - right[0]["y"]) == pytest.approx(gap, abs=0.05)
    problems = json.loads((tmp_path / "map" / "build" / "problems.geojson").read_text(encoding="utf-8"))
    assert any(feature["properties"]["problem"] == "dead_end" for feature in problems["features"])


def test_chain_of_short_gaps_does_not_collapse(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(-20, 0), (0, 0)]),
        line("lane_002", "all", [(1.5, 0), (1.5, 20)]),
        line("lane_003", "all", [(3.0, 0), (23, 0)]),
        point("door_001", "door", -10, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    inner = [node for node in graph["nodes"] if -0.2 <= node["x"] <= 3.2 and abs(node["y"]) < 0.5]
    assert len(inner) == 2


def test_three_ends_at_a_corner_become_one_point(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (-20, 0)]),
        line("lane_002", "all", [(1, 0), (21, 0)]),
        line("lane_003", "all", [(0.5, 0.8), (0.5, 21)]),
        point("door_001", "door", -10, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    corner = [node for node in graph["nodes"] if -0.2 <= node["x"] <= 1.2 and -0.2 <= node["y"] <= 1.0]
    assert len(corner) == 1


def test_ends_exactly_2m_apart_weld(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(-20, 0), (0, 0)]),
        line("lane_002", "all", [(2, 0), (22, 0)]),
        point("door_001", "door", -10, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    shared = _shared_lane_node(graph, "lane_001", "lane_002")
    assert shared is not None


def test_ends_just_over_2m_stay_apart(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(-20, 0), (0, 0)]),
        line("lane_002", "all", [(2.05, 0), (22, 0)]),
        point("door_001", "door", -10, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    assert _shared_lane_node(graph, "lane_001", "lane_002") is None


def test_end_does_not_snap_to_a_mid_edge(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (20, 0)]),
        line("lane_002", "all", [(10, 1), (10, 20)]),
        point("door_001", "door", 0, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    assert _node_ids(graph, "lane_001").isdisjoint(_node_ids(graph, "lane_002"))
    problems = json.loads((tmp_path / "map" / "build" / "problems.geojson").read_text(encoding="utf-8"))
    assert any(feature["properties"]["problem"] == "dead_end" for feature in problems["features"])


def test_crossing_splits_both_lanes(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (10, 0)]),
        line("lane_002", "all", [(5, -5), (5, 5)]),
        point("door_001", "door", 0, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    cross = [node for node in graph["nodes"] if abs(node["x"] - 5) < 0.15 and abs(node["y"]) < 0.15]
    assert len(cross) == 1
    assert cross[0]["kind"] == "auto"
    assert len(cross[0]["edge_ids"]) == 4


def test_t_junction_connects_at_the_touching_end(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (10, 0)]),
        line("lane_002", "all", [(5, 0), (5, 10)]),
        point("door_001", "door", 0, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    joint = [node for node in graph["nodes"] if abs(node["x"] - 5) < 0.15 and abs(node["y"]) < 0.15]
    assert len(joint) == 1
    assert len(joint[0]["edge_ids"]) == 3


def test_door_reuses_a_vertex_within_2m(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (20, 0)]),
        point("door_001", "door", 1, 1),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    assert len([edge for edge in graph["edges"] if edge["kind"] == "lane"]) == 1
    door_edge = next(edge for edge in graph["edges"] if edge["kind"] == "door")
    assert door_edge["length_m"] == pytest.approx(math.hypot(1, 1), abs=0.05)


def test_door_splits_the_lane_when_the_spot_is_clear(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (20, 0)]),
        point("door_001", "door", 10, 4),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    assert len([edge for edge in graph["edges"] if edge["kind"] == "lane"]) == 2
    door_edge = next(edge for edge in graph["edges"] if edge["kind"] == "door")
    assert door_edge["length_m"] == pytest.approx(4, abs=0.05)
    assert any(abs(node["x"] - 10) < 0.1 and abs(node["y"]) < 0.1 for node in graph["nodes"])


def test_door_at_15m_is_allowed(tmp_path: Path) -> None:
    features, places = base()
    features[1] = point("door_001", "door", 15, 15)
    graph = build_ok(tmp_path, features, places)
    door_edge = next(edge for edge in graph["edges"] if edge["kind"] == "door")
    assert door_edge["length_m"] == pytest.approx(15, abs=0.05)


def test_short_door_connector_is_not_a_short_lane(tmp_path: Path) -> None:
    features, places = base()
    features[1] = point("door_001", "door", 15, 0.4)
    graph = build_ok(tmp_path, features, places)
    problems = json.loads((tmp_path / "map" / "build" / "problems.geojson").read_text(encoding="utf-8"))
    assert all(feature["properties"]["problem"] != "short_lane" for feature in problems["features"])
    door_edge = next(edge for edge in graph["edges"] if edge["kind"] == "door")
    assert door_edge["length_m"] == pytest.approx(0.4, abs=0.05)


def test_ends_on_either_side_of_a_junction_use_its_id(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(-30, 0), (-1.9, 0)]),
        line("lane_002", "all", [(1.9, 0), (30, 0)]),
        point("junc_001", "junction", 0, 0, landmark="B2"),
        point("door_001", "door", -20, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    lane_edges = [edge for edge in graph["edges"] if edge["kind"] == "lane"]
    assert sum("junc_001" in (edge["from"], edge["to"]) for edge in lane_edges) == 2
    junction = next(node for node in graph["nodes"] if node["id"] == "junc_001")
    assert junction["kind"] == "junction"
    assert junction["landmark"] == "B2"
    assert junction["x"] == pytest.approx(0, abs=0.02)
    assert junction["y"] == pytest.approx(0, abs=0.02)


def test_junction_on_a_lane_splits_it(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (20, 0)]),
        point("junc_001", "junction", 8, 0, landmark="B2"),
        point("door_001", "door", 2, 2),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    assert any(edge["kind"] == "lane" and "junc_001" in (edge["from"], edge["to"]) for edge in graph["edges"])
    assert len([edge for edge in graph["edges"] if edge["kind"] == "lane"]) >= 2


def test_junction_2m_off_the_lane_welds(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (20, 0)]),
        point("junc_001", "junction", 8, 2),
        point("door_001", "door", 2, 1),
    ]
    places = [("place_001", "Main gate", "gate", "all", "door_001", "yes")]
    graph = build_ok(tmp_path, features, places)
    junction = next(node for node in graph["nodes"] if node["id"] == "junc_001")
    assert junction["y"] == pytest.approx(2, abs=0.05)


def test_staff_lane_keeps_every_access_tag_walkable(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(0, 0), (10, 0)]),
        line("lane_002", "staff", [(10, 0), (20, 0)]),
        line("lane_003", "girls", [(20, 0), (30, 0)]),
        line("lane_004", "boys", [(30, 0), (40, 0)]),
        point("door_001", "door", 5, 2),
        point("door_002", "door", 35, 2),
    ]
    features.append(point("door_003", "door", 25, 2))
    places = [
        ("place_001", "Underpass entrance", "gate", "all", "door_001", "yes"),
        ("place_002", "G2", "block", "girls", "door_002", "no"),
        ("place_003", "Warden", "office", "staff", "door_003", "no"),
    ]
    graph = build_ok(tmp_path, features, places)
    assert {edge["access"] for edge in graph["edges"] if edge["kind"] == "lane"} >= {"all", "staff", "girls", "boys"}
    assert {place["access"] for place in graph["places"]} == {"all", "girls", "staff"}
    assert {place["place_id"] for place in graph["places"]} == {"place_001", "place_002", "place_003"}


def test_crosses_block_warns_in_lon_lat(tmp_path: Path) -> None:
    features = [
        line("lane_001", "all", [(-5, 5), (15, 5)]),
        block("block_001", [square(0, 0, 10, 10)], "place_001"),
        point("door_001", "door", -5, 8),
    ]
    places = [("place_001", "B3", "block", "boys", "door_001", "yes")]
    build_ok(tmp_path, features, places)
    problems = json.loads((tmp_path / "map" / "build" / "problems.geojson").read_text(encoding="utf-8"))
    hit = next(feature for feature in problems["features"] if feature["properties"]["problem"] == "crosses_block")
    assert "lane_001" in hit["properties"]["ids"]
    assert "block_001" in hit["properties"]["ids"]
    assert abs(hit["geometry"]["coordinates"][0][0]) > 20


def test_unnamed_block_warns(tmp_path: Path) -> None:
    features, places = base()
    features.append(block("block_009", [square(40, 40, 50, 50)]))
    build_ok(tmp_path, features, places)
    problems = json.loads((tmp_path / "map" / "build" / "problems.geojson").read_text(encoding="utf-8"))
    assert any(feature["properties"]["problem"] == "unnamed_block" for feature in problems["features"])


def test_missing_origin_is_not_written(tmp_path: Path) -> None:
    features, places = base()
    graph = build_ok(tmp_path, features, places, origin=False)
    assert not (tmp_path / "map" / "origin.json").exists()
    assert "lat" in graph["origin"] and "lon" in graph["origin"]


def test_same_drawing_builds_the_same_file(tmp_path: Path) -> None:
    features, places = base()
    map_dir = write_map(tmp_path, features, places)
    build(map_dir, repo_root=tmp_path)
    first = (map_dir / "build" / "graph.json").read_text(encoding="utf-8")
    build(map_dir, repo_root=tmp_path)
    assert (map_dir / "build" / "graph.json").read_text(encoding="utf-8") == first


def test_install_copies_graph(tmp_path: Path) -> None:
    features, places = base()
    map_dir = write_map(tmp_path, features, places)
    build(map_dir, repo_root=tmp_path, install=True)
    copied = tmp_path / "app" / "Assets" / "StreamingAssets" / "graph.json"
    assert copied.read_text(encoding="utf-8") == (map_dir / "build" / "graph.json").read_text(encoding="utf-8")


def test_ledger_gains_ids_and_keeps_retired_rows(tmp_path: Path) -> None:
    features, places = base()
    ledger = "id,kind,note\nlane_999,lane,retired on purpose\n"
    map_dir = write_map(tmp_path, features, places, ledger=ledger)
    build(map_dir, repo_root=tmp_path)
    text = (map_dir / "id-ledger.csv").read_text(encoding="utf-8")
    assert "lane_999,lane,retired on purpose" in text
    assert "lane_001,lane" in text
    assert "door_001,door" in text
    assert "place_001,place" in text


def test_photo_map_row_is_copied_as_numbers(tmp_path: Path) -> None:
    features, places = base()
    features.append(point("junc_001", "junction", 10, 0))
    photos = [("pmap_001", "pmap_001.bytes", "1.5", "2", "0.5", "90", "60", "junc_001")]
    photo_dir = tmp_path / "app" / "Assets" / "navigay" / "PhotoMaps"
    photo_dir.mkdir(parents=True)
    (photo_dir / "pmap_001.bytes").write_bytes(b"map")
    map_dir = write_map(tmp_path, features, places, photos=photos)
    graph = build(map_dir, repo_root=tmp_path)
    photo = graph["photo_maps"][0]
    assert photo["x"] == 1.5
    assert photo["load_radius_m"] == 60
    assert photo["junction_ids"] == ["junc_001"]
    assert isinstance(photo["x"], float)


def test_cli_failure_names_the_id(tmp_path: Path, capsys: pytest.CaptureFixture[str]) -> None:
    features, places = base()
    features[1] = point("door_001", "door", 15, 40)
    map_dir = write_map(tmp_path, features, places)
    before = (map_dir / "ghs.geojson").read_bytes()
    assert main(["--map", str(map_dir)]) == 1
    assert "door_001" in capsys.readouterr().err
    assert (map_dir / "ghs.geojson").read_bytes() == before
    assert not (map_dir / "build" / "graph.json").exists()


def test_missing_file(tmp_path: Path) -> None:
    features, places = base()
    map_dir = write_map(tmp_path, features, places)
    (map_dir / "places.csv").unlink()
    with pytest.raises(BuildFailure) as caught:
        build(map_dir, repo_root=tmp_path)
    assert any("places.csv" in error and "missing" in error for error in caught.value.errors)


def test_invalid_json(tmp_path: Path) -> None:
    features, places = base()
    map_dir = write_map(tmp_path, features, places)
    (map_dir / "ghs.geojson").write_text("{", encoding="utf-8")
    with pytest.raises(BuildFailure) as caught:
        build(map_dir, repo_root=tmp_path)
    assert any("not valid JSON" in error for error in caught.value.errors)


def test_invalid_csv(tmp_path: Path) -> None:
    features, places = base()
    map_dir = write_map(tmp_path, features, places)
    (map_dir / "places.csv").write_bytes(b"place_id,name\n\xff\xfe")
    with pytest.raises(BuildFailure) as caught:
        build(map_dir, repo_root=tmp_path)
    assert any("not valid CSV" in error for error in caught.value.errors)


def test_crs_member_fails(tmp_path: Path) -> None:
    features, places = base()
    map_dir = write_map(tmp_path, features, places)
    drawing = json.loads((map_dir / "ghs.geojson").read_text(encoding="utf-8"))
    drawing["crs"] = {"type": "name", "properties": {"name": "EPSG:4326"}}
    (map_dir / "ghs.geojson").write_text(json.dumps(drawing), encoding="utf-8")
    with pytest.raises(BuildFailure) as caught:
        build(map_dir, repo_root=tmp_path)
    assert any("crs" in error for error in caught.value.errors)


def test_feature_ids_that_differ(tmp_path: Path) -> None:
    features, places = base()
    features[0]["id"] = "lane_009"
    failure = build_fail(tmp_path, features, places)
    assert any("lane_001" in error and "differ" in error for error in failure.errors)


def test_missing_id(tmp_path: Path) -> None:
    features, places = base()
    features.append(
        {
            "type": "Feature",
            "properties": {"feature": "junction"},
            "geometry": {"type": "Point", "coordinates": ll(4, 0)},
        }
    )
    failure = build_fail(tmp_path, features, places)
    assert any("missing an id" in error for error in failure.errors)


def test_duplicate_id(tmp_path: Path) -> None:
    features, places = base()
    features.append(line("lane_001", "all", [(0, 50), (10, 50)]))
    failure = build_fail(tmp_path, features, places)
    assert any("lane_001" in error and "duplicated" in error for error in failure.errors)


def test_wrongly_shaped_id(tmp_path: Path) -> None:
    features, places = base()
    features[1]["properties"]["id"] = "door_01"
    places[0] = ("place_001", "Main gate", "gate", "all", "door_01", "yes")
    failure = build_fail(tmp_path, features, places)
    assert any("door_01" in error and "wrongly shaped" in error for error in failure.errors)


def test_prefix_does_not_match_feature(tmp_path: Path) -> None:
    features, places = base()
    features[1]["properties"]["id"] = "lane_050"
    failure = build_fail(tmp_path, features, places)
    assert any("lane_050" in error and "prefix" in error for error in failure.errors)


def test_ledger_kind_mismatch_does_not_rewrite_the_ledger(tmp_path: Path) -> None:
    features, places = base()
    ledger = "id,kind,note\ndoor_001,lane,wrong\n"
    map_dir = write_map(tmp_path, features, places, ledger=ledger)
    before = (map_dir / "id-ledger.csv").read_bytes()
    with pytest.raises(BuildFailure) as caught:
        build(map_dir, repo_root=tmp_path)
    assert any("door_001" in error and "ledger kind" in error for error in caught.value.errors)
    assert (map_dir / "id-ledger.csv").read_bytes() == before


def test_missing_access(tmp_path: Path) -> None:
    features, places = base()
    del features[0]["properties"]["access"]
    failure = build_fail(tmp_path, features, places)
    assert any("lane_001" in error and "access is missing" in error for error in failure.errors)


def test_bad_access(tmp_path: Path) -> None:
    features, places = base()
    features[0]["properties"]["access"] = "public"
    failure = build_fail(tmp_path, features, places)
    assert any("lane_001" in error and "public" in error for error in failure.errors)


def test_bad_place_access(tmp_path: Path) -> None:
    features, places = base()
    places[0] = ("place_001", "Main gate", "gate", "public", "door_001", "yes")
    failure = build_fail(tmp_path, features, places)
    assert any("place_001" in error and "public" in error for error in failure.errors)


def test_bad_place_type(tmp_path: Path) -> None:
    features, places = base()
    places[0] = ("place_001", "Main gate", "shop", "all", "door_001", "yes")
    failure = build_fail(tmp_path, features, places)
    assert any("place_001" in error and "shop" in error for error in failure.errors)


def test_blank_start(tmp_path: Path) -> None:
    features, places = base()
    places[0] = ("place_001", "Main gate", "gate", "all", "door_001", "")
    failure = build_fail(tmp_path, features, places)
    assert any("place_001" in error and "start is blank" in error for error in failure.errors)


def test_duplicate_names_ignore_case(tmp_path: Path) -> None:
    features, places = base()
    features.append(point("door_002", "door", 5, 3))
    places.append(("place_002", "main gate", "gate", "all", "door_002", "no"))
    failure = build_fail(tmp_path, features, places)
    assert any("place_002" in error and "already used" in error for error in failure.errors)


def test_place_with_no_door(tmp_path: Path) -> None:
    features = [line("lane_001", "all", [(0, 0), (30, 0)])]
    places = [("place_001", "Main gate", "gate", "all", "", "yes")]
    failure = build_fail(tmp_path, features, places)
    assert any("place_001" in error and "no door" in error for error in failure.errors)


def test_listed_door_does_not_exist(tmp_path: Path) -> None:
    features = [line("lane_001", "all", [(0, 0), (30, 0)])]
    places = [("place_001", "Main gate", "gate", "all", "door_999", "yes")]
    failure = build_fail(tmp_path, features, places)
    assert any("door_999" in error and "does not exist" in error for error in failure.errors)


def test_door_listed_on_two_places(tmp_path: Path) -> None:
    features, places = base()
    places.append(("place_002", "Side gate", "gate", "all", "door_001", "no"))
    failure = build_fail(tmp_path, features, places)
    assert any("door_001" in error and "more than once" in error for error in failure.errors)


def test_drawn_door_on_no_place(tmp_path: Path) -> None:
    features, places = base()
    features.append(point("door_002", "door", 4, 3))
    failure = build_fail(tmp_path, features, places)
    assert any("door_002" in error and "no place" in error for error in failure.errors)


def test_block_place_id_missing(tmp_path: Path) -> None:
    features, places = base()
    features.append(block("block_001", [square(40, 40, 48, 48)], "place_999"))
    failure = build_fail(tmp_path, features, places)
    assert any("block_001" in error and "place_999" in error for error in failure.errors)


def test_geometry_type_mismatch(tmp_path: Path) -> None:
    features, places = base()
    features[0]["geometry"] = {"type": "Point", "coordinates": ll(0, 0)}
    failure = build_fail(tmp_path, features, places)
    assert any("lane_001" in error and "LineString" in error for error in failure.errors)


def test_polygon_is_not_closed(tmp_path: Path) -> None:
    features, places = base()
    features.append(block("block_001", [[(40, 40), (48, 40), (48, 48), (40, 48)]]))
    failure = build_fail(tmp_path, features, places)
    assert any("block_001" in error and "closed" in error for error in failure.errors)


def test_polygon_is_self_crossing(tmp_path: Path) -> None:
    features, places = base()
    features.append(block("block_001", [[(0, 40), (5, 45), (0, 45), (5, 40), (0, 40)]]))
    failure = build_fail(tmp_path, features, places)
    assert any("block_001" in error and "self-crossing" in error for error in failure.errors)


def test_point_farther_than_1500m(tmp_path: Path) -> None:
    features, places = base()
    features.append(
        {
            "type": "Feature",
            "properties": {"id": "junc_050", "feature": "junction"},
            "geometry": {"type": "Point", "coordinates": [0, 0]},
        }
    )
    failure = build_fail(tmp_path, features, places)
    assert any("junc_050" in error and "1500" in error for error in failure.errors)


def test_no_start_place(tmp_path: Path) -> None:
    features, places = base()
    places[0] = ("place_001", "Main gate", "gate", "all", "door_001", "no")
    failure = build_fail(tmp_path, features, places)
    assert any("no start=yes" in error for error in failure.errors)


def test_unreachable_door(tmp_path: Path) -> None:
    features, places = base()
    features.append(line("lane_002", "girls", [(0, 80), (20, 80)]))
    features.append(point("door_002", "door", 10, 83))
    places.append(("place_002", "G2", "block", "girls", "door_002", "no"))
    failure = build_fail(tmp_path, features, places)
    assert any("door_002" in error and "cannot be reached" in error for error in failure.errors)


def test_door_farther_than_15m(tmp_path: Path) -> None:
    features, places = base()
    features[1] = point("door_001", "door", 15, 16)
    failure = build_fail(tmp_path, features, places)
    assert any("door_001" in error and "15 m" in error for error in failure.errors)


def test_junction_farther_than_2m(tmp_path: Path) -> None:
    features, places = base()
    features.append(point("junc_008", "junction", 8, 5))
    failure = build_fail(tmp_path, features, places)
    assert any("junc_008" in error and "2 m" in error for error in failure.errors)


def test_photo_map_missing_bytes(tmp_path: Path) -> None:
    features, places = base()
    features.append(point("junc_001", "junction", 10, 0))
    photos = [("pmap_001", "pmap_001.bytes", "1", "2", "0", "10", "60", "junc_001")]
    failure = build_fail(tmp_path, features, places, photos=photos)
    assert any("pmap_001" in error and "pmap_001.bytes" in error for error in failure.errors)


def test_photo_map_unknown_junction(tmp_path: Path) -> None:
    features, places = base()
    features.append(point("junc_001", "junction", 10, 0))
    photos = [("pmap_001", "pmap_001.bytes", "1", "2", "0", "10", "60", "junc_999")]
    photo_dir = tmp_path / "app" / "Assets" / "navigay" / "PhotoMaps"
    photo_dir.mkdir(parents=True)
    (photo_dir / "pmap_001.bytes").write_bytes(b"map")
    map_dir = write_map(tmp_path, features, places, photos=photos)
    with pytest.raises(BuildFailure) as caught:
        build(map_dir, repo_root=tmp_path)
    assert any("pmap_001" in error and "junc_999" in error for error in caught.value.errors)


def test_blank_photo_placement(tmp_path: Path) -> None:
    features, places = base()
    features.append(point("junc_001", "junction", 10, 0))
    photos = [("pmap_001", "pmap_001.bytes", "", "2", "0", "10", "60", "junc_001")]
    photo_dir = tmp_path / "app" / "Assets" / "navigay" / "PhotoMaps"
    photo_dir.mkdir(parents=True)
    (photo_dir / "pmap_001.bytes").write_bytes(b"map")
    map_dir = write_map(tmp_path, features, places, photos=photos)
    with pytest.raises(BuildFailure) as caught:
        build(map_dir, repo_root=tmp_path)
    assert any("pmap_001" in error and "placement is blank" in error for error in caught.value.errors)


def _node_ids(graph: dict, lane_id: str) -> set[str]:
    found: set[str] = set()
    for edge in graph["edges"]:
        if edge["lane_id"] == lane_id:
            found.add(edge["from"])
            found.add(edge["to"])
    return found


def _shared_lane_node(graph: dict, left: str, right: str) -> str | None:
    shared = _node_ids(graph, left) & _node_ids(graph, right)
    if not shared:
        return None
    return next(iter(shared))
