from __future__ import annotations

import argparse
import json
from pathlib import Path


# Animal forms should be grouped by natural semantics instead of the legacy
# male/female flag.
ANIMAL_IDS = {
    "神雕",
    "猴子",
    "毒猴子",
    "大蛇",
    "汉家松鼠",
    "土拨鼠主角",
    "松鼠神雕",
}

# Eunuch roles are tracked as a dedicated category.
EUNUCH_IDS = {
    "太监",
    "东方不败",
    "炼狱东方不败",
    "受重伤的东方不败",
    "蒙面黑衣人",
    "岳不群",
    "林平之",
    "神林平之",
    "天关林平之",
    "阉割封不平",
    "疯狂封不平",
    "超级疯狂封不平",
    "炼狱超级疯狂封不平",
}

# Non-human units are better represented as neutral.
NEUTRAL_IDS = {
    "木头人",
    "魔神",
    "九尊殿",
    "九宫阁",
    "九华阁",
    "灵鹫宫后山树林",
    "藏经阁",
    "御虚阁",
    "烧香坊",
    "静念楼",
}

# These roles are clearly female by title or setting, even though the source
# data encoded them as male.
FEMALE_OVERRIDE_IDS = {
    "刀白凤",
    "牧羊女",
    "梦姑",
    "凤姐",
}

CATEGORY_TITLES = {
    "male": "男性",
    "female": "女性",
    "animal": "动物",
    "eunuch": "太监",
    "neutral": "中性",
}


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    parser = argparse.ArgumentParser(
        description="Build a natural-semantic role gender list from characters.json."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "json" / "characters.json"),
        help="Path to characters.json",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(repo_root / "reports" / "role-gender-list.md"),
        help="Path to output markdown report",
    )
    return parser.parse_args()


def classify_role(role: dict[str, object]) -> str:
    role_id = str(role["id"])
    source_gender = role.get("gender")

    if role_id in ANIMAL_IDS:
        return "animal"
    if role_id in EUNUCH_IDS:
        return "eunuch"
    if role_id in NEUTRAL_IDS:
        return "neutral"
    if role_id in FEMALE_OVERRIDE_IDS:
        return "female"
    if source_gender in {"male", "female"}:
        return str(source_gender)
    return "neutral"


def format_role(role: dict[str, object]) -> str:
    role_id = str(role["id"])
    role_name = role.get("name")
    if role_name is None or role_name == role_id:
        return role_id
    return f"{role_id} ({role_name})"


def build_report(payload: dict[str, object]) -> str:
    categories: dict[str, list[str]] = {
        "male": [],
        "female": [],
        "animal": [],
        "eunuch": [],
        "neutral": [],
    }

    for role in payload["characters"]:
        category = classify_role(role)
        categories[category].append(format_role(role))

    lines = [
        "# Role 性别语义清单",
        "",
        f"来源: `{payload['source']}`",
        f"总数: {payload['count']}",
        "",
        "说明:",
        "- 基础数据来自 `json/characters.json`。",
        "- 动物形态和动物化角色统一归入 `动物`。",
        "- 新增 `太监`，根据原著和“阉人”天赋归档。",
        "- 非人物单位和地点化身统一归入 `中性`。",
        "- `刀白凤`、`牧羊女`、`梦姑`、`凤姐` 按自然语义改归 `女性`。",
        "",
    ]

    for key in ("male", "female", "animal", "eunuch", "neutral"):
        lines.append(f"## {CATEGORY_TITLES[key]} ({len(categories[key])})")
        lines.append("")
        lines.extend(f"- {name}" for name in categories[key])
        lines.append("")

    return "\n".join(lines).rstrip() + "\n"


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = json.loads(input_path.read_text(encoding="utf-8"))
    report = build_report(payload)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(report, encoding="utf-8")


if __name__ == "__main__":
    main()
