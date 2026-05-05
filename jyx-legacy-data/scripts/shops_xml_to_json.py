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
        description="Convert jyx legacy shops.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "shops.xml"),
        help="Path to shops.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "shops.json"),
        help="Path to output shops.json",
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


def parse_optional_non_negative_int(value: str | None) -> int | None:
    number = parse_int(value, default=-1)
    return number if number >= 0 else None


def build_products(shop: ET.Element) -> list[dict[str, object]]:
    return [
        {
            "contentId": content_id,
            "purchaseLimit": parse_optional_non_negative_int(sale.get("limit")),
            "price": parse_optional_non_negative_int(sale.get("price")),
            "premiumPrice": parse_optional_non_negative_int(sale.get("yuanbao")),
        }
        for sale in shop.findall("sale")
        if (content_id := parse_optional_text(sale.get("name"))) is not None
    ]


def build_shop(shop: ET.Element) -> dict[str, object]:
    name = parse_optional_text(shop.get("name"))
    if name is None:
        raise ValueError("Shop entry is missing name.")

    return {
        "id": name,
        "name": name,
        "music": parse_optional_text(shop.get("music")),
        "background": parse_optional_text(shop.get("pic")),
        "products": build_products(shop),
    }


def convert_shops(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_shop(shop) for shop in root.findall("shop")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_shops(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
