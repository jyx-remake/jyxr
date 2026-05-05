from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


MAP_KIND_BY_ID: dict[str, str] = {
    "大地图": "large",
}

GLOBAL_TRIGGER_MAP_ID = "大地图"


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert jyx legacy maps.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "maps.xml"),
        help="Path to maps.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "maps.json"),
        help="Path to output maps.json",
    )
    parser.add_argument(
        "--globaltrigger",
        default=str(repo_root / "globaltrigger.xml"),
        help="Path to globaltrigger.xml",
    )
    return parser.parse_args()


def parse_int(value: str | None, *, default: int = 0) -> int:
    if value is None or value == "":
        return default
    return int(value)


def parse_optional_text(value: str | None) -> str | None:
    if value is None:
        return None
    value = value.strip()
    return value or None


def parse_repeat_mode(value: str | None) -> str | None:
    text = parse_optional_text(value)
    return None if text is None else text


def build_musics(map_element: ET.Element) -> list[str]:
    musics: list[str] = []
    musics_element = map_element.find("musics")
    if musics_element is None:
        return musics

    for music in musics_element.findall("music"):
        music_id = parse_optional_text(music.get("name"))
        if music_id is None:
            raise ValueError("map music entry is missing name.")
        musics.append(music_id)

    return musics


def build_condition(condition: ET.Element) -> dict[str, object]:
    condition_type = parse_optional_text(condition.get("type"))
    if condition_type is None:
        raise ValueError("map event condition is missing type.")

    return {
        "type": condition_type,
        "value": parse_optional_text(condition.get("value")),
    }


def build_event(event: ET.Element) -> dict[str, object]:
    event_type = parse_optional_text(event.get("type"))
    if event_type is None:
        raise ValueError("map event is missing type.")

    event_data: dict[str, object] = {
        "type": event_type,
        "targetId": parse_optional_text(event.get("value")),
        "probability": parse_int(event.get("probability"), default=100),
        "image": parse_optional_text(event.get("image")),
        "description": parse_optional_text(event.get("description")),
        "conditions": [build_condition(condition) for condition in event.findall("condition")],
    }

    repeat_mode = parse_repeat_mode(event.get("repeat"))
    if repeat_mode is not None:
        event_data["repeatMode"] = repeat_mode

    return event_data


def build_map_unit(unit: ET.Element) -> dict[str, object]:
    unit_id = parse_optional_text(unit.get("name"))
    if unit_id is None:
        raise ValueError("mapunit is missing name.")

    return {
        "id": unit_id,
        "position": {
            "x": parse_int(unit.get("x")),
            "y": parse_int(unit.get("y")),
        },
        "description": parse_optional_text(unit.get("description")),
        "picture": parse_optional_text(unit.get("pic")),
        "events": [build_event(event) for event in unit.findall("event")],
    }


def build_map(map_element: ET.Element) -> dict[str, object]:
    map_id = parse_optional_text(map_element.get("name"))
    if map_id is None:
        raise ValueError("map entry is missing name.")

    map_data: dict[str, object] = {
        "id": map_id,
        "name": map_id,
        "description": parse_optional_text(map_element.get("desc")),
        "picture": parse_optional_text(map_element.get("pic")),
    }

    map_kind = MAP_KIND_BY_ID.get(map_id)
    if map_kind is not None:
        map_data["kind"] = map_kind

    map_data["musics"] = build_musics(map_element)
    map_data["locations"] = [build_map_unit(unit) for unit in map_element.findall("mapunit")]

    return map_data


def build_global_trigger_event(trigger: ET.Element) -> dict[str, object]:
    story_id = parse_optional_text(trigger.get("story"))
    if story_id is None:
        raise ValueError("global trigger is missing story.")

    return {
        "type": "story",
        "targetId": story_id,
        "probability": 100,
        "repeatMode": "once",
        "conditions": [build_condition(condition) for condition in trigger.findall("condition")],
    }


def load_global_trigger_events(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_global_trigger_event(trigger) for trigger in root.findall("trigger")]


def attach_global_trigger_events(
    maps: list[dict[str, object]],
    global_trigger_events: list[dict[str, object]],
) -> None:
    if not global_trigger_events:
        return

    for map_data in maps:
        if map_data.get("id") == GLOBAL_TRIGGER_MAP_ID:
            map_data["enterEvents"] = global_trigger_events
            return

    raise ValueError(f"map '{GLOBAL_TRIGGER_MAP_ID}' was not found while attaching global triggers.")


def convert_maps(input_path: Path, global_trigger_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    maps = [build_map(map_element) for map_element in root.findall("map")]
    attach_global_trigger_events(maps, load_global_trigger_events(global_trigger_path))
    return maps


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()
    global_trigger_path = Path(args.globaltrigger).resolve()

    payload = convert_maps(input_path, global_trigger_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
