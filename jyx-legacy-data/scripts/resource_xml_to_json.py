from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


EXCLUDED_GROUPS = {"天赋", "buff"}
VALUE_PREFIX_MAP = {
    "Audios": "audio",
    "UI": "ui",
    "Heads": "head",
    "Items": "item",
    "Maps": "map",
    "CGs": "cg",
}


def parse_args() -> argparse.Namespace:
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent
    output_dir = repo_root / "json"
    parser = argparse.ArgumentParser(
        description="Convert jyx legacy resource.xml into a typed JSON file."
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
        default=str(output_dir / "resources.json"),
        help="Path to output resources.json",
    )
    return parser.parse_args()


def parse_optional_text(value: str | None) -> str | None:
    if value is None:
        return None
    value = value.strip()
    return value or None


def infer_group(resource_id: str) -> str | None:
    head, separator, _ = resource_id.partition(".")
    if not separator:
        return None
    return head or None


def transform_resource_value(value: str | None) -> str | None:
    value = parse_optional_text(value)
    if value is None:
        return None

    prefix, separator, remainder = value.partition("/")
    mapped_prefix = VALUE_PREFIX_MAP.get(prefix)
    if mapped_prefix is None:
        return value
    if not separator:
        return mapped_prefix
    return f"{mapped_prefix}/{remainder}"


def build_resource(element: ET.Element) -> dict[str, object] | None:
    resource_id = parse_optional_text(element.get("key"))
    if resource_id is None:
        raise ValueError("resource entry is missing key.")

    group = infer_group(resource_id)
    if group in EXCLUDED_GROUPS:
        return None

    return {
        "id": resource_id,
        "group": group,
        "value": transform_resource_value(element.get("value")),
    }


def convert_resource(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [
        resource
        for element in root.findall("resource")
        if (resource := build_resource(element)) is not None
    ]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_resource(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
