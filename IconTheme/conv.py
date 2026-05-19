import sys
import json
from pathlib import Path


def main():
    if len(sys.argv) < 2:
        print("Usage: python conv.py <folder_path>")
        sys.exit(1)

    folder = Path(sys.argv[1])
    if not folder.is_dir():
        print(f"Error: {folder} is not a directory")
        sys.exit(1)

    icon_map = {
        "clock.svg": "clock",
        "cpu.svg": "cpu",
        "gpu.svg": "gpu",
        "hd.svg": "hd",
        "net.svg": "net",
        "ram.svg": "ram",
        "win.svg": "win",
    }

    icons: dict[str, str] = {}
    for filename, key in icon_map.items():
        filepath = folder / filename
        if filepath.is_file():
            icons[key] = filepath.read_text(encoding="utf-8")
        else:
            print(f"Warning: {filename} not found")

    data = {
        "Name": folder.name,
        "Icons": icons,
    }

    # Merge if info.json exists
    info_path = folder / "info.json"
    if info_path.is_file():
        with open(info_path, encoding="utf-8") as f:
            info = json.load(f)
        data = {**info, **data}

    output_path = Path(__file__).parent / (folder.name + ".json")
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print(f"Wrote {output_path}")


if __name__ == "__main__":
    main()
