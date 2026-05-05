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
        description="Convert jyx legacy battles.xml into a typed JSON file."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(repo_root / "battles.xml"),
        help="Path to battles.xml",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(output_dir / "battles.json"),
        help="Path to output battles.json",
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


def parse_optional_int(value: str | None) -> int | None:
    text = parse_optional_text(value)
    return None if text is None else int(text)


def parse_bool(value: str | None) -> bool:
    text = parse_optional_text(value)
    if text is None:
        return False
    return text.lower() == "true"


def split_required_characters(value: str | None) -> list[str]:
    text = parse_optional_text(value)
    if text is None:
        return []
    return [part.strip() for part in text.split("#") if part.strip()]


def build_position(role: ET.Element) -> dict[str, int]:
    return {
        "x": parse_int(role.get("x")),
        "y": parse_int(role.get("y")),
    }


def build_fixed_participant(role: ET.Element) -> dict[str, object]:
    return {
        "position": build_position(role),
        "team": parse_int(role.get("team"), default=1),
        "facing": parse_int(role.get("face")),
        "characterId": parse_optional_text(role.get("key")),
        "partyIndex": parse_optional_int(role.get("index")),
    }


def build_random_participant(role: ET.Element) -> dict[str, object]:
    participant = {
        "position": build_position(role),
        "facing": parse_int(role.get("face")),
        "characterId": parse_optional_text(role.get("name")),
        "level": parse_int(role.get("level")),
    }
    animation = parse_optional_text(role.get("animation"))
    if animation is not None:
        participant["animation"] = animation
    if parse_bool(role.get("boss")):
        participant["boss"] = True
    return participant


def build_battle(battle: ET.Element) -> dict[str, object]:
    battle_id = parse_optional_text(battle.get("key"))
    if battle_id is None:
        raise ValueError("battle entry is missing key.")

    roles = battle.find("roles")
    random = battle.find("random")

    return {
        "id": battle_id,
        "name": battle_id,
        "mapId": parse_optional_text(battle.get("mapkey")),
        "music": parse_optional_text(battle.get("music")),
        "requiredCharacterIds": split_required_characters(battle.get("must")),
        "participants": []
        if roles is None
        else [build_fixed_participant(role) for role in roles.findall("role")],
        "randomParticipants": []
        if random is None
        else [build_random_participant(role) for role in random.findall("role")],
    }


def convert_battles(input_path: Path) -> list[dict[str, object]]:
    root = ET.parse(input_path).getroot()
    return [build_battle(battle) for battle in root.findall("battle")]


def main() -> None:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    payload = convert_battles(input_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
