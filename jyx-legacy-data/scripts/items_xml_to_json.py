from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from current_content_schema import (
    EQUIPMENT_SLOT_TYPE_MAP,
    build_grant_talent_affix,
    parse_passive_affixes,
)


ACTIVE_TRIGGER_NAME_MAP: dict[str, str] = {
    "AddBuff": "add_buff",
    "Balls": "add_rage",
    "解毒": "detoxify",
    "AddMaxHp": "add_maxhp",
    "AddMaxMp": "add_maxmp",
    "AddHp": "add_hp",
    "AddMp": "add_mp",
    "RecoverHp": "add_hp_percent",
    "RecoverMp": "add_mp_percent",
    "skill": "external_skill",
    "internalskill": "internal_skill",
    "specialskill": "special_skill",
}

ITEM_TYPE_MAP: dict[int, str] = {
    0: "consumable",
    4: "skill_book",
    5: "quest_item",
    6: "special_skill_book",
    7: "talent_book",
    8: "booster",
    9: "utility",
}

REQUIRE_STAT_NAMES = {
    "bili",
    "dingli",
    "fuyuan",
    "gengu",
    "jianfa",
    "daofa",
    "quanzhang",
    "qimen",
    "shenfa",
    "wuxing",
}


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert jyx legacy items.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "items.xml"),
        help="Path to items.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "items.json"),
        help="Path to output items.json",
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


def parse_bool(value: str | None) -> bool:
    return (value or "").strip().lower() == "true"


def parse_optional_text(value: str | None) -> str | None:
    if value is None:
        return None
    value = value.strip()
    return value or None


def split_hash_list(value: str | None) -> list[str]:
    text = parse_optional_text(value)
    if text is None:
        return []
    return [part.strip() for part in text.split("#") if part.strip()]


def parse_active_effect(trigger: ET.Element) -> dict[str, object]:
    original_name = trigger.get("name")
    effect_type = ACTIVE_TRIGGER_NAME_MAP[original_name]
    argvs = parse_optional_text(trigger.get("argvs"))

    effect: dict[str, object] = {"type": effect_type}

    if effect_type in {"add_rage", "add_maxhp", "add_maxmp", "add_hp", "add_mp", "add_hp_percent", "add_mp_percent"}:
        effect["value"] = parse_int(argvs)
        return effect

    if effect_type == "add_buff":
        parts = (argvs or "").split(".")
        effect["buffId"] = parts[0] if parts else ""
        if len(parts) > 1:
            effect["level"] = parse_int(parts[1])
        if len(parts) > 2:
            effect["duration"] = parse_int(parts[2])
        if len(parts) > 3:
            property_value = parse_int(parts[3], default=-1)
            if property_value >= 0:
                effect["property"] = property_value
        return effect

    if effect_type == "detoxify":
        effect["values"] = parse_numeric_values(argvs)
        return effect

    if effect_type in {"external_skill", "internal_skill"}:
        parts = split_csv_values(argvs)
        effect["skillId"] = parts[0] if parts else None
        if len(parts) > 1:
            effect["level"] = parse_int(parts[1])
        return effect

    if effect_type == "special_skill":
        effect["skillId"] = argvs
        return effect

    raise ValueError(f"Unsupported active trigger: {original_name}")


def split_csv_values(value: str | None) -> list[str]:
    text = parse_optional_text(value)
    if text is None:
        return []
    return [part.strip() for part in text.split(",") if part.strip()]


def parse_numeric_values(value: str | None) -> list[int]:
    parts = split_csv_values(value)
    return [parse_int(part) for part in parts]


def normalize_requirement(name: str | None, argvs: str | None) -> tuple[str | None, str | None]:
    name = parse_optional_text(name)
    argvs = parse_optional_text(argvs)

    if name in REQUIRE_STAT_NAMES or name == "talent":
        return name, argvs

    if argvs in REQUIRE_STAT_NAMES or argvs == "talent":
        return argvs, name

    return name, argvs


def parse_requirement(requirement: ET.Element) -> dict[str, object] | None:
    name, argvs = normalize_requirement(requirement.get("name"), requirement.get("argvs"))
    if name is None:
        return None

    if name == "talent":
        return {
            "type": "talent",
            "talentId": argvs,
        }

    if argvs is None:
        return None

    return {
        "type": "stat",
        "statId": name,
        "value": parse_int(argvs),
    }


def build_item(item: ET.Element) -> dict[str, object]:
    item_id = parse_optional_text(item.get("name"))
    if item_id is None:
        raise ValueError("item entry is missing name.")
    legacy_type = parse_int(item.get("type"))

    use_effects = [
        parse_active_effect(trigger)
        for trigger in item.findall("trigger")
        if trigger.get("name") in ACTIVE_TRIGGER_NAME_MAP
    ]
    talent_ids = split_hash_list(item.get("talent"))
    slot_type = EQUIPMENT_SLOT_TYPE_MAP.get(legacy_type)
    item_type = "equipment" if slot_type is not None else ITEM_TYPE_MAP.get(legacy_type, "unknown")
    passive_triggers = item.findall("trigger")
    if item_type == "talent_book":
        passive_triggers = [
            trigger
            for trigger in passive_triggers
            if trigger.get("name") != "talent"
        ]

    affixes = [
        affix
        for trigger in passive_triggers
        for affix in parse_passive_affixes(trigger)
    ]
    if item_type == "talent_book":
        use_effects.extend(
            {
                "type": "grant_talent",
                "talentId": parse_optional_text(trigger.get("argvs")),
            }
            for trigger in item.findall("trigger")
            if trigger.get("name") == "talent" and parse_optional_text(trigger.get("argvs")) is not None
        )
        use_effects.extend(
            {
                "type": "grant_talent",
                "talentId": talent_id,
            }
            for talent_id in talent_ids
        )
    else:
        affixes.extend(build_grant_talent_affix(talent_id) for talent_id in talent_ids)
    requirements = [
        parsed
        for require in item.findall("require")
        if (parsed := parse_requirement(require)) is not None
    ]
    category = "equipment" if slot_type is not None or affixes else "normal"

    payload = {
        "category": category,
        "id": item_id,
        "name": item_id,
        "type": item_type,
        "level": parse_int(item.get("level"), default=1),
        "price": parse_int(item.get("price")),
        "cooldown": parse_int(item.get("cd")),
        "canDrop": parse_bool(item.get("drop")),
        "description": parse_optional_text(item.get("desc")),
        "picture": parse_optional_text(item.get("pic")),
        "requirements": requirements,
        "useEffects": use_effects,
    }
    if category == "equipment":
        if slot_type is None:
            raise ValueError(f"equipment item '{item_id}' is missing a slot type.")
        payload["slotType"] = slot_type
        payload["affixes"] = affixes
    return payload


def convert_items(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_item(item) for item in root.findall("item")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_items(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
