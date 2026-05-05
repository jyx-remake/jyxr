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

SKILL_TYPE_MAP: dict[int, str] = {
    0: "quanzhang",
    1: "jianfa",
    2: "daofa",
    3: "qimen",
}


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert jyx legacy skills.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "skills.xml"),
        help="Path to skills.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "external-skills.json"),
        help="Path to output external-skills.json",
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


def parse_optional_int(value: str | None) -> int | None:
    if value is None or value == "":
        return None
    return int(value)


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

def build_targeting(node: ET.Element) -> dict[str, object] | None:
    targeting: dict[str, object] = {
        "castType": None,
        "castSize": parse_optional_int(node.get("castsize")),
        "impactType": parse_cover_type(node.get("covertype")),
        "impactSize": parse_optional_int(node.get("coversize")),
    }
    return None if all(value is None for value in targeting.values()) else targeting


def build_form_skill(unique: ET.Element) -> dict[str, object]:
    form_id = parse_optional_text(unique.get("name"))
    if form_id is None:
        raise ValueError("unique entry is missing name.")

    form_skill = {
        "id": form_id,
        "name": form_id,
        "description": parse_optional_text(unique.get("info")),
        "hard": parse_float(unique.get("hard"), default=1.0),
        "cooldown": parse_int(unique.get("cd")),
        "cost": {
            "rage": parse_int(unique.get("costball")),
        },
        "powerExtra": parse_float(unique.get("poweradd")),
        "animation": parse_optional_text(unique.get("animation")),
        "audio": parse_optional_text(unique.get("audio")),
        "unlockLevel": parse_int(unique.get("requirelv"), default=1),
        "buffs": parse_buffs(unique.get("buff")),
    }
    targeting = build_targeting(unique)
    if targeting is not None:
        form_skill["targeting"] = targeting
    return form_skill


def build_level_override(level: ET.Element) -> dict[str, object]:
    level_override = {
        "level": parse_int(level.get("level"), default=1),
        "powerOverride": parse_float(level.get("power")),
        "cooldown": parse_int(level.get("cd")),
        "animation": parse_optional_text(level.get("animation")),
    }
    targeting = build_targeting(level)
    if targeting is not None:
        level_override["targeting"] = targeting
    return level_override


def build_external_skill(skill: ET.Element) -> dict[str, object]:
    skill_id = parse_optional_text(skill.get("name"))
    if skill_id is None:
        raise ValueError("skill entry is missing name.")

    affixes = [
        affix
        for trigger in skill.findall("trigger")
        for affix in build_skill_affixes(trigger)
    ]

    external_skill = {
        "id": skill_id,
        "name": skill_id,
        "description": parse_optional_text(skill.get("info")),
        "icon": parse_optional_text(skill.get("icon")) or "",
        "type": SKILL_TYPE_MAP.get(parse_int(skill.get("type")), "unknown"),
        "isHarmony": parse_int(skill.get("tiaohe")) == 1,
        "affinity": parse_float(skill.get("suit")),
        "hard": parse_float(skill.get("hard"), default=1.0),
        "cooldown": parse_int(skill.get("cd")),
        "powerBase": parse_float(skill.get("basepower")),
        "powerStep": parse_float(skill.get("step")),
        "animation": parse_optional_text(skill.get("animation")),
        "audio": parse_optional_text(skill.get("audio")),
        "buffs": parse_buffs(skill.get("buff")),
        "levelOverrides": [build_level_override(level) for level in skill.findall("level")],
        "formSkills": [build_form_skill(unique) for unique in skill.findall("unique")],
        "affixes": affixes,
    }
    targeting = build_targeting(skill)
    if targeting is not None:
        external_skill["targeting"] = targeting
    return external_skill


def convert_skills(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_external_skill(skill) for skill in root.findall("skill")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_skills(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
