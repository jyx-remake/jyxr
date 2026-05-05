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
        description="Convert jyx legacy resource_suggesttips.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "resource_suggesttips.xml"),
        help="Path to resource_suggesttips.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "game-tips.json"),
        help="Path to output game-tips.json",
    )
    return parser.parse_args()


def parse_optional_text(value: str | None) -> str | None:
    if value is None:
        return None
    value = value.strip()
    return value or None


def build_game_tip(element: ET.Element) -> dict[str, object]:
    tip_id = parse_optional_text(element.get("key"))
    if tip_id is None:
        raise ValueError("game tip resource is missing key.")

    return {
        "id": tip_id,
        "text": parse_optional_text(element.get("value")),
    }


def convert_game_tips(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_game_tip(element) for element in root.findall("resource")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_game_tips(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
