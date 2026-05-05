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
        description="Convert jyx legacy menpai.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "menpai.xml"),
        help="Path to menpai.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "sects.json"),
        help="Path to output sects.json",
    )
    return parser.parse_args()


def parse_optional_text(value: str | None) -> str | None:
    if value is None:
        return None
    value = value.strip()
    return value or None


def split_values(value: str | None) -> list[str]:
    text = parse_optional_text(value)
    if text is None or text == "?":
        return []

    normalized = (
        text.replace("，", "、")
        .replace(",", "、")
        .replace("；", "、")
        .replace(";", "、")
        .replace("/", "、")
    )
    return [part.strip() for part in normalized.split("、") if part.strip() and part.strip() != "?"]


def build_menpai(element: ET.Element) -> dict[str, object]:
    name = parse_optional_text(element.get("name"))
    if name is None:
        raise ValueError("menpai entry is missing name.")

    return {
        "id": name,
        "name": name,
        "storyId": parse_optional_text(element.get("story")),
        "primaryFocus": parse_optional_text(element.get("zhuxiu")),
        "description": parse_optional_text(element.get("info")),
        "portrait": parse_optional_text(element.get("pic")),
        "signatureSkillNames": split_values(element.get("wuxue")),
        "masterNames": split_values(element.get("shifu")),
        "background": parse_optional_text(element.get("bg")),
        "traitTags": split_values(element.get("tedian")),
    }


def convert_menpai(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_menpai(element) for element in root.findall("menpai")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_menpai(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
