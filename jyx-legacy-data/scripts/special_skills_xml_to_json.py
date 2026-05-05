from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


COVER_TYPE_MAP: dict[int, str] = {
    0: "single",
    1: "plus",
    2: "star",
    3: "line",
    4: "square",
    5: "fan",
    6: "ring",
    7: "x",
    8: "cleave",
}


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert jyx legacy special_skills.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "special_skills.xml"),
        help="Path to special_skills.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "special-skills.json"),
        help="Path to output special-skills.json",
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


def parse_cover_type(value: str | None) -> str | None:
    if value is None or value == "":
        return None
    return COVER_TYPE_MAP.get(int(value), value)


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
            buff["args"] = [part for part in parts[4:] if part]
        buffs.append(buff)

    return buffs


def build_skill(skill: ET.Element) -> dict[str, object]:
    return {
        "id": skill.get("name"),
        "name": skill.get("name"),
        "description": parse_optional_text(skill.get("info")),
        "icon": parse_optional_text(skill.get("icon")),
        "cooldown": parse_int(skill.get("cd")),
        "cost": {
            "mp": parse_int(skill.get("costMp")),
            "rage": parse_int(skill.get("costball")),
        },
        "targeting": {
            "canTargetSelf": skill.get("hitself") == "1",
            "castType": None,
            "castSize": parse_int(skill.get("castsize")),
            "impactType": parse_cover_type(skill.get("covertype")),
            "impactSize": parse_int(skill.get("coversize")),
        },
        "animation": parse_optional_text(skill.get("animation")),
        "audio": parse_optional_text(skill.get("audio")),
        "buffs": parse_buffs(skill.get("buff")),
    }


def convert_special_skills(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_skill(skill) for skill in root.findall("special_skill")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_special_skills(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
