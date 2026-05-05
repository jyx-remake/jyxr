from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


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
    "hp": "max_hp",
    "mp": "max_mp",
}


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert jyx legacy grow_templates.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "grow_templates.xml"),
        help="Path to grow_templates.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "grow-templates.json"),
        help="Path to output grow-templates.json",
    )
    return parser.parse_args()


def parse_int(value: str | None, *, default: int = 0) -> int:
    if value is None or value == "":
        return default
    return int(value)


def build_stats(element: ET.Element) -> dict[str, int]:
    return {
        target_key: parse_int(element.get(source_key))
        for source_key, target_key in INT_ATTRIBUTE_MAP.items()
    }


def build_grow_template(element: ET.Element) -> dict[str, object]:
    template_id = element.get("name")
    if template_id is None or template_id.strip() == "":
        raise ValueError("grow_template entry is missing name.")

    return {
        "id": template_id,
        "name": template_id,
        "statGrowth": build_stats(element),
    }


def convert_grow_templates(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_grow_template(element) for element in root.findall("grow_template")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_grow_templates(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
