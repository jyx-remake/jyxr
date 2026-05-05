from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


TALENT_PREFIX = "天赋."


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert legacy Resource.xml talent entries into talents.json."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "Resource.xml"),
        help="Path to Resource.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "talents.json"),
        help="Path to output talents.json",
    )
    return parser.parse_args()


def parse_talent_point(raw_value: str | None) -> tuple[int, str]:
    if raw_value is None:
        return 0, ""

    value = raw_value.strip()
    if not value:
        return 0, ""

    number_text, separator, description = value.partition("#")
    if not separator:
        return 0, value

    try:
        number = int(number_text.strip() or "0")
    except ValueError:
        number = 0

    return number, description.strip()


def build_talent_entry(element: ET.Element) -> dict[str, object] | None:
    key = (element.get("key") or "").strip()
    if not key.startswith(TALENT_PREFIX):
        return None

    name = key[len(TALENT_PREFIX):].strip()
    if not name:
        return None

    point, description = parse_talent_point(element.get("value"))
    return {
        "id": name,
        "name": name,
        "point": point,
        "description": description,
    }


def convert_resource_talents(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    talents: list[dict[str, object]] = []
    seen_ids: set[str] = set()

    for element in root.findall("resource"):
        talent = build_talent_entry(element)
        if talent is None:
            continue

        talent_id = talent["id"]
        if talent_id in seen_ids:
            continue

        seen_ids.add(talent_id)
        talents.append(talent)

    talents.sort(key=lambda entry: str(entry["id"]))
    return talents


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_resource_talents(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
