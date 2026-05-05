from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


BUFF_PREFIX = "buff."


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert legacy resource.xml buff entries into buffs.json."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "resource.xml"),
        help="Path to resource.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "buffs.json"),
        help="Path to output buffs.json",
    )
    return parser.parse_args()


def parse_description(raw_value: str | None) -> str:
    if raw_value is None:
        return ""
    return raw_value.strip()


def build_buff_entry(element: ET.Element) -> dict[str, object] | None:
    key = (element.get("key") or "").strip()
    if not key.startswith(BUFF_PREFIX):
        return None

    name = key[len(BUFF_PREFIX):].strip()
    if not name:
        return None

    return {
        "id": name,
        "name": name,
        "description": parse_description(element.get("value")),
    }


def convert_resource_buffs(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    buffs: list[dict[str, object]] = []
    seen_ids: set[str] = set()

    for element in root.findall("resource"):
        buff = build_buff_entry(element)
        if buff is None:
            continue

        buff_id = buff["id"]
        if buff_id in seen_ids:
            continue

        seen_ids.add(buff_id)
        buffs.append(buff)

    buffs.sort(key=lambda entry: str(entry["id"]))
    return buffs


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_resource_buffs(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
