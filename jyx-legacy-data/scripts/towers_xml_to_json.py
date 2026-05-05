from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert jyx legacy towers.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "towers.xml"),
        help="Path to towers.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "towers.json"),
        help="Path to output towers.json",
    )
    return parser.parse_args()


def parse_int(value: str | None, *, default: int = 0) -> int:
    if value is None or value == "":
        return default
    return int(value)


def parse_float(value: str | None, *, default: float = 0.0) -> float:
    if value is None or value == "":
        return default
    return float(value)


def parse_optional_text(value: str | None) -> str | None:
    if value is None:
        return None
    value = value.strip()
    return value or None


def parse_optional_count(value: str | None) -> int | None:
    number = parse_int(value, default=-1)
    return number if number >= 0 else None


def build_reward(item: ET.Element) -> dict[str, object]:
    reward_id = parse_optional_text(item.get("key"))
    if reward_id is None:
        raise ValueError("tower item entry is missing key.")

    return {
        "contentId": reward_id,
        "probability": parse_float(item.get("probability"), default=1.0),
        "count": parse_optional_count(item.get("number")),
    }


def build_stage(stage: ET.Element) -> dict[str, object]:
    stage_id = parse_optional_text(stage.get("key"))
    if stage_id is None:
        raise ValueError("tower map entry is missing key.")

    return {
        "id": stage_id,
        "name": stage_id,
        "battleId": stage_id,
        "index": parse_int(stage.get("index")),
        "rewards": [build_reward(item) for item in stage.findall("item")],
    }


def build_condition(condition: ET.Element) -> dict[str, object]:
    condition_type = parse_optional_text(condition.get("type"))
    if condition_type is None:
        raise ValueError("tower condition entry is missing type.")

    return {
        "type": condition_type,
        "value": parse_optional_text(condition.get("value")),
    }


def build_tower(tower: ET.Element) -> dict[str, object]:
    tower_id = parse_optional_text(tower.get("key"))
    if tower_id is None:
        raise ValueError("tower entry is missing key.")

    return {
        "id": tower_id,
        "name": tower_id,
        "description": parse_optional_text(tower.get("desc")),
        "stages": [build_stage(stage) for stage in tower.findall("map")],
        "unlockConditions": [build_condition(condition) for condition in tower.findall("condition")],
    }


def convert_towers(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_tower(tower) for tower in root.findall("tower")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_towers(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
