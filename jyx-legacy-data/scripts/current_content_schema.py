from __future__ import annotations

import xml.etree.ElementTree as ET


PASSIVE_TRIGGER_NAME_MAP: dict[str, str] = {
    "powerup_skill": "powerup_external_skill",
    "powerup_internalskill": "powerup_internal_skill",
    "powerup_uniqueskill": "powerup_form_skill",
    "powerup_aoyi": "powerup_legend_skill",
    "powerup_quanzhang": "powerup_quanzhang",
    "powerup_jianfa": "powerup_jianfa",
    "powerup_daofa": "powerup_daofa",
    "powerup_qimen": "powerup_qimen",
    "attack": "attack",
    "defence": "defence",
    "critical": "crit_mult",
    "criticalp": "crit_chance",
    "sp": "speed",
    "mingzhong": "accuracy",
    "xi": "lifesteal",
    "anti_debuff": "anti_debuff",
    "animation": "animation",
    "talent": "talent",
    "eq_talent": "equipped_talent",
    "attribute": "stat",
}

ATTRIBUTE_NAME_MAP: dict[str, str] = {
    "臂力": "bili",
    "定力": "dingli",
    "福缘": "fuyuan",
    "福源": "fuyuan",
    "根骨": "gengu",
    "剑法": "jianfa",
    "刀法": "daofa",
    "拳掌": "quanzhang",
    "奇门": "qimen",
    "身法": "shenfa",
    "悟性": "wuxing",
    "生命": "max_hp",
    "内力": "max_mp",
    "搏击格斗": "quanzhang",
    "使剑技巧": "jianfa",
    "耍刀技巧": "daofa",
    "奇门兵器": "qimen",
}

RATIO_STAT_IDS: set[str] = {
    "accuracy",
    "crit_chance",
    "crit_mult",
    "anti_crit_chance",
    "lifesteal",
    "anti_debuff",
}

WEAPON_TYPE_MAP: dict[str, str] = {
    "powerup_quanzhang": "quanzhang",
    "powerup_jianfa": "jianfa",
    "powerup_daofa": "daofa",
    "powerup_qimen": "qimen",
}

EQUIPMENT_SLOT_TYPE_MAP: dict[int, str] = {
    1: "weapon",
    2: "armor",
    3: "accessory",
}


def parse_int(value: str | None, *, default: int = 0) -> int:
    if value is None or value == "":
        return default
    return int(value)


def parse_optional_text(value: str | None) -> str | None:
    if value is None:
        return None
    value = value.strip()
    return value or None


def split_csv(value: str | None) -> list[str]:
    text = parse_optional_text(value)
    if text is None:
        return []
    return [part.strip() for part in text.split(",") if part.strip()]


def try_parse_number(value: str) -> int | float | None:
    try:
        return int(value)
    except ValueError:
        try:
            return float(value)
        except ValueError:
            return None


def parse_numeric_csv(value: str | None) -> list[int | float] | None:
    parts = split_csv(value)
    if not parts:
        return None
    numbers = [try_parse_number(part) for part in parts]
    if any(number is None for number in numbers):
        return None
    return [number for number in numbers if number is not None]


def build_modifier_value(delta: int | float) -> dict[str, object]:
    return {
        "op": "add",
        "delta": delta,
    }


def build_increase_modifier_value(delta: int | float) -> dict[str, object]:
    return {
        "op": "increase",
        "delta": delta,
    }


def normalize_real_ratio(delta: int | float) -> int | float:
    normalized = round(float(delta) / 100.0, 6)
    if normalized == 0:
        return 0
    if normalized.is_integer():
        return int(normalized)
    return normalized


def build_stat_affix(stat_id: str, delta: int | float) -> dict[str, object]:
    if stat_id in RATIO_STAT_IDS:
        delta = normalize_real_ratio(delta)

    return {
        "type": "stat_modifier",
        "stat": stat_id,
        "value": build_modifier_value(delta),
    }


def build_grant_talent_affix(talent_id: str) -> dict[str, object]:
    return {
        "type": "grant_talent",
        "talentId": talent_id,
    }


def build_grant_model_affix(model_id: str, *, priority: int = 0, description: str = "") -> dict[str, object]:
    return {
        "type": "grant_model",
        "modelId": model_id,
        "priority": priority,
        "description": description,
    }


def build_skill_bonus_affix(skill_id: str, delta: int | float) -> dict[str, object]:
    return {
        "type": "skill_bonus_modifier",
        "skillId": skill_id,
        "value": build_modifier_value(normalize_real_ratio(delta)),
    }


def build_weapon_bonus_affix(weapon_type: str, delta: int | float) -> dict[str, object]:
    return {
        "type": "weapon_bonus_modifier",
        "weaponType": weapon_type,
        "value": build_modifier_value(normalize_real_ratio(delta)),
    }


def build_legend_skill_chance_affix(skill_id: str, delta: int | float) -> dict[str, object]:
    return {
        "type": "legend_skill_chance_modifier",
        "skillId": skill_id,
        "value": build_modifier_value(normalize_real_ratio(delta)),
    }


def parse_passive_affixes(trigger: ET.Element) -> list[dict[str, object]]:
    original_name = trigger.get("name")
    if original_name not in PASSIVE_TRIGGER_NAME_MAP:
        return []

    effect_type = PASSIVE_TRIGGER_NAME_MAP[original_name]
    argvs = parse_optional_text(trigger.get("argvs"))
    numeric_values = parse_numeric_csv(argvs)

    if effect_type == "stat":
        parts = split_csv(argvs)
        stat_name = parts[0] if parts else None
        stat_id = ATTRIBUTE_NAME_MAP.get(stat_name or "", stat_name or "")
        value = parse_int(parts[1]) if len(parts) > 1 else 0
        return [build_stat_affix(stat_id, value)]

    if effect_type in {"talent", "equipped_talent"}:
        return [] if argvs is None else [build_grant_talent_affix(argvs)]

    if effect_type == "animation":
        parts = split_csv(argvs)
        if not parts:
            return []
        description = parts[0]
        model_id = parts[1] if len(parts) > 1 else parts[0]
        return [build_grant_model_affix(model_id, description=description)]

    if effect_type == "attack":
        values = parse_numeric_csv(argvs) or []
        if len(values) >= 2:
            return [
                build_stat_affix("attack", values[0]),
                build_stat_affix("crit_chance", values[1]),
            ]
        return []

    if effect_type == "defence":
        values = parse_numeric_csv(argvs) or []
        if len(values) >= 2:
            return [
                build_stat_affix("defence", values[0]),
                build_stat_affix("anti_crit_chance", values[1]),
            ]
        return []

    if effect_type in {"powerup_external_skill", "powerup_internal_skill", "powerup_form_skill"}:
        parts = split_csv(argvs)
        if len(parts) >= 2 and try_parse_number(parts[1]) is not None:
            return [build_skill_bonus_affix(parts[0], try_parse_number(parts[1]))]
        return []

    if effect_type == "powerup_legend_skill":
        parts = split_csv(argvs)
        if len(parts) >= 3:
            power_value = try_parse_number(parts[1])
            chance_value = try_parse_number(parts[2])
            affixes: list[dict[str, object]] = []
            if power_value is not None:
                affixes.append(build_skill_bonus_affix(parts[0], power_value))
            if chance_value is not None:
                affixes.append(build_legend_skill_chance_affix(parts[0], chance_value))
            return affixes
        return []

    if effect_type in WEAPON_TYPE_MAP and numeric_values:
        return [build_weapon_bonus_affix(WEAPON_TYPE_MAP[effect_type], numeric_values[0])]

    if numeric_values:
        return [build_stat_affix(effect_type, numeric_values[0])]

    return []


def build_skill_affixes(trigger: ET.Element) -> list[dict[str, object]]:
    affixes: list[dict[str, object]] = []
    minimum_level = parse_int(trigger.get("lv"), default=0)
    requires_equipped = trigger.get("name") == "eq_talent"

    for effect in parse_passive_affixes(trigger):
        entry: dict[str, object] = {
            "effect": effect,
        }
        if minimum_level > 0:
            entry["minimumLevel"] = minimum_level
        if requires_equipped:
            entry["requiresEquippedInternalSkill"] = True
        affixes.append(entry)

    return affixes
