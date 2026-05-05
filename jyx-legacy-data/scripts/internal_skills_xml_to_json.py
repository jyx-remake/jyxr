from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from current_content_schema import build_skill_affixes


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
        description="Convert jyx legacy internal_skills.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "internal_skills.xml"),
        help="Path to internal_skills.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "internal-skills.json"),
        help="Path to output internal-skills.json",
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
            args = [part for part in parts[4:] if part]
            if args:
                buff["args"] = args
        buffs.append(buff)

    return buffs


def build_form_skill(unique: ET.Element) -> dict[str, object]:
    form_id = parse_optional_text(unique.get("name"))
    if form_id is None:
        raise ValueError("unique entry is missing name.")

    return {
        "id": form_id,
        "name": form_id,
        "description": parse_optional_text(unique.get("info")),
        "cooldown": parse_int(unique.get("cd")),
        "cost": {
            "rage": parse_int(unique.get("costball")),
        },
        "targeting": {
            "castType": None,
            "castSize": parse_int(unique.get("castsize")),
            "impactType": parse_cover_type(unique.get("covertype")),
            "impactSize": parse_int(unique.get("coversize")),
        },
        "powerExtra": parse_float(unique.get("poweradd")),
        "animation": parse_optional_text(unique.get("animation")),
        "audio": parse_optional_text(unique.get("audio")),
        "unlockLevel": parse_int(unique.get("requirelv"), default=1),
        "buffs": parse_buffs(unique.get("buff")),
    }


def build_internal_skill(skill: ET.Element) -> dict[str, object]:
    skill_id = parse_optional_text(skill.get("name"))
    if skill_id is None:
        raise ValueError("internal_skill entry is missing name.")

    affixes = [
        affix
        for trigger in skill.findall("trigger")
        for affix in build_skill_affixes(trigger)
    ]
    form_skills = [build_form_skill(unique) for unique in skill.findall("unique")]

    return {
        "id": skill_id,
        "name": skill_id,
        "description": parse_optional_text(skill.get("info")) or "",
        "icon": parse_optional_text(skill.get("icon")) or "",
        "hard": parse_float(skill.get("hard"), default=1.0),
        "yin": parse_int(skill.get("yin")),
        "yang": parse_int(skill.get("yang")),
        "attackScale": parse_float(skill.get("attack")),
        "criticalScale": parse_float(skill.get("critical")),
        "defenceScale": parse_float(skill.get("defence")),
        "formSkills": form_skills,
        "affixes": affixes,
    }


def convert_internal_skills(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_internal_skill(skill) for skill in root.findall("internal_skill")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_internal_skills(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
