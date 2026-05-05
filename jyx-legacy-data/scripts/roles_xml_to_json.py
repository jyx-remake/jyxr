from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from build_role_gender_list import classify_role


INT_ATTRIBUTE_MAP: dict[str, str] = {
    "bili": "bili",
    "dingli": "dingli",
    "fuyuan": "fuyuan",
    "gengu": "gengu",
    "jianfa": "jianfa",
    "daofa": "daofa",
    "quanzhang": "quanzhang",
    "qimen": "qimen",
    "shenfa": "shenfa",
    "wuxing": "wuxing",
    "wuxue": "wuxue",
    "maxhp": "max_hp",
    "maxmp": "max_mp",
}


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert jyx legacy roles.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "roles.xml"),
        help="Path to roles.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "characters.json"),
        help="Path to output characters.json",
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


def parse_arena(value: str | None) -> bool:
    text = parse_optional_text(value)
    return text != "no"


def parse_legacy_gender(value: str | None) -> str | None:
    match parse_int(value, default=-99):
        case 0:
            return "male"
        case 1:
            return "female"
        case -1:
            return "neutral"
        case _:
            return parse_optional_text(value)


def parse_talent_ids(value: str | None) -> list[str]:
    text = parse_optional_text(value)
    if text is None:
        return []
    return [part.strip() for part in text.split("#") if part.strip()]


def build_stats(element: ET.Element) -> dict[str, int]:
    return {
        target_key: parse_int(element.get(source_key))
        for source_key, target_key in INT_ATTRIBUTE_MAP.items()
    }


def build_special_skills(role: ET.Element) -> list[str]:
    container = role.find("special_skills")
    if container is None:
        return []
    return [
        skill_name
        for skill in container.findall("skill")
        if (skill_name := parse_optional_text(skill.get("name"))) is not None
    ]


def build_internal_skills(role: ET.Element) -> list[dict[str, object]]:
    container = role.find("internal_skills")
    if container is None:
        return []
    return [
        {
            "id": skill_name,
            "level": parse_int(skill.get("level"), default=1),
            "maxLevel": parse_int(skill.get("maxlevel"), default=10),
            "equipped": skill.get("equipped") == "1",
        }
        for skill in container.findall("internal_skill")
        if (skill_name := parse_optional_text(skill.get("name"))) is not None
    ]


def build_equipment_ids(role: ET.Element) -> list[str]:
    container = role.find("items")
    if container is None:
        return []
    return [
        item_name
        for item in container.findall("item")
        if (item_name := parse_optional_text(item.get("name"))) is not None
    ]


def build_external_skills(role: ET.Element) -> list[dict[str, object]]:
    container = role.find("skills")
    if container is None:
        return []
    return [
        {
            "id": skill_name,
            "level": parse_int(skill.get("level"), default=1),
            "maxLevel": parse_int(skill.get("maxlevel"), default=10),
        }
        for skill in container.findall("skill")
        if (skill_name := parse_optional_text(skill.get("name"))) is not None
    ]


def build_role(role: ET.Element) -> dict[str, object]:
    legacy_gender = parse_legacy_gender(role.get("female"))
    payload = {
        "id": role.get("key"),
        "name": role.get("name"),
        "level": parse_int(role.get("level"), default=1),
        "portrait": parse_optional_text(role.get("head")),
        "model": parse_optional_text(role.get("animation")),
        "gender": legacy_gender,
        "growTemplate": parse_optional_text(role.get("grow_template")),
        "arenaEnabled": parse_arena(role.get("arena")),
        "talentIds": parse_talent_ids(role.get("talent")),
        "stats": build_stats(role),
        "specialSkillIds": build_special_skills(role),
        "internalSkills": build_internal_skills(role),
        "equipmentIds": build_equipment_ids(role),
        "externalSkills": build_external_skills(role),
    }
    payload["gender"] = classify_role(payload)
    return payload


def convert_roles(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_role(role) for role in root.findall("role")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_roles(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
