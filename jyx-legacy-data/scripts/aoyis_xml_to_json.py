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
        description="Convert jyx legacy aoyis.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "aoyis.xml"),
        help="Path to aoyis.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "legend-skills.json"),
        help="Path to output legend-skills.json",
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
    text = value.strip()
    return text or None


def parse_buffs(value: str | None) -> list[dict[str, object]]:
    text = parse_optional_text(value)
    if text is None:
        return []

    buffs: list[dict[str, object]] = []
    for chunk in text.split("#"):
        parts = [part.strip() for part in chunk.split(".")]
        if not parts or not parts[0]:
            continue

        buff: dict[str, object] = {
            "id": parts[0],
            "level": 1,
            "duration": 3,
        }
        if len(parts) > 1 and parts[1] != "":
            buff["level"] = parse_int(parts[1])
        if len(parts) > 2 and parts[2] != "":
            buff["duration"] = parse_int(parts[2])
        if len(parts) > 3 and parts[3] != "":
            chance = parse_int(parts[3])
            if chance >= 0:
                buff["chance"] = chance
        if len(parts) > 4:
            args = [part for part in parts[4:] if part]
            if args:
                buff["args"] = args
        buffs.append(buff)

    return buffs


def parse_condition(condition: ET.Element) -> dict[str, object]:
    payload: dict[str, object] = {}

    condition_type = parse_optional_text(condition.get("type"))
    if condition_type is not None:
        if condition_type == "specialskill":
            condition_type = "special_skill"
        elif condition_type == "internalskill":
            condition_type = "internal_skill"
        payload["type"] = condition_type

    target_id = parse_optional_text(condition.get("value"))
    if target_id is not None:
        payload["targetId"] = target_id

    level = parse_optional_text(condition.get("level") or condition.get("levelValue"))
    if level is not None:
        payload["level"] = parse_int(level)

    return payload


def make_base_id(name: str, start_skill: str) -> str:
    return f"{name}-{start_skill}"


def assign_unique_ids(legend_skills: list[dict[str, object]]) -> None:
    counts: dict[str, int] = {}
    for legend_skill in legend_skills:
        base_id = make_base_id(
            str(legend_skill["name"]),
            str(legend_skill["startSkill"]),
        )
        index = counts.get(base_id, 0) + 1
        counts[base_id] = index
        legend_skill["id"] = base_id if index == 1 else f"{base_id}-{index}"


def build_legend_skill(aoyi: ET.Element) -> dict[str, object]:
    start = parse_optional_text(aoyi.get("start"))
    name = parse_optional_text(aoyi.get("name"))
    if start is None or name is None:
        raise ValueError("Each <aoyi> must define both 'start' and 'name'.")

    legend_skill: dict[str, object] = {
        "name": name,
        "startSkill": start,
        "probability": parse_float(aoyi.get("probability")),
        "requiredLevel": parse_int(aoyi.get("level"), default=1),
        "buffs": parse_buffs(aoyi.get("buff")),
        "conditions": [parse_condition(condition) for condition in aoyi.findall("condition")],
        "powerExtra": parse_float(aoyi.get("addPower")),
        "animation": parse_optional_text(aoyi.get("animation")),
    }

    return legend_skill


def convert_aoyis(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    legend_skills = [build_legend_skill(aoyi) for aoyi in root.findall("aoyi")]
    assign_unique_ids(legend_skills)
    return legend_skills


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_aoyis(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
