from __future__ import annotations

import argparse
import random
import shutil
from dataclasses import dataclass
from pathlib import Path


@dataclass
class Record:
    image_path: Path
    label_path: Path


def link_or_copy(src: Path, dst: Path, use_link: bool) -> None:
    src_abs = src.resolve()
    if not src_abs.exists():
        raise FileNotFoundError(f"Source does not exist: {src_abs}")
    dst.parent.mkdir(parents=True, exist_ok=True)
    if dst.exists() or dst.is_symlink():
        if use_link and dst.is_symlink():
            try:
                if dst.resolve() == src_abs:
                    return
            except FileNotFoundError:
                pass
        if dst.is_dir() and not dst.is_symlink():
            shutil.rmtree(dst)
        else:
            dst.unlink(missing_ok=True)
    if use_link:
        dst.symlink_to(src_abs)
    else:
        shutil.copy2(src_abs, dst)


def load_charset(classes_path: Path) -> list[str]:
    lines = [line.strip() for line in classes_path.read_text(encoding="utf-8").splitlines()]
    return [line for line in lines if line]


def write_data_yaml(out_dir: Path, split_dir: Path, classes: list[str]) -> None:
    data_yaml = out_dir / "data.yaml"
    rel_train = Path("splits") / "images" / "train"
    rel_val = Path("splits") / "images" / "val"
    lines = [
        f"path: {out_dir}",
        f"train: {rel_train}",
        f"val: {rel_val}",
        "names:",
    ]
    for idx, name in enumerate(classes):
        lines.append(f"  {idx}: {name}")
    data_yaml.write_text("\n".join(lines), encoding="utf-8")


def prepare_records(label_dir: Path, image_dir: Path, out_dir: Path, use_link: bool) -> list[Record]:
    records: list[Record] = []
    for label_path in label_dir.glob("*.txt"):
        if label_path.name == "classes.txt":
            continue
        image_path = image_dir / f"{label_path.stem}.png"
        if not image_path.exists():
            image_path = image_dir / f"{label_path.stem}.jpg"
        if not image_path.exists():
            continue
        out_label = out_dir / "labels" / label_path.name
        link_or_copy(label_path, out_label, use_link)
        out_image = out_dir / "images" / image_path.name
        link_or_copy(image_path, out_image, use_link)
        records.append(Record(image_path=out_image, label_path=out_label))
    return records


def split_records(records: list[Record], seed: int, ratio: float) -> tuple[list[Record], list[Record]]:
    random.Random(seed).shuffle(records)
    split_index = int(len(records) * ratio)
    return records[:split_index], records[split_index:]


def write_split(records: list[Record], split_dir: Path, name: str, use_link: bool) -> None:
    images_dir = split_dir / "images" / name
    labels_dir = split_dir / "labels" / name
    images_dir.mkdir(parents=True, exist_ok=True)
    labels_dir.mkdir(parents=True, exist_ok=True)

    for record in records:
        link_or_copy(record.image_path, images_dir / record.image_path.name, use_link)
        link_or_copy(record.label_path, labels_dir / record.label_path.name, use_link)


def main() -> None:
    parser = argparse.ArgumentParser(description="Prepare EALPR recognition dataset")
    parser.add_argument("--raw", type=Path, default=Path("dataset"))
    parser.add_argument("--out", type=Path, default=Path("data/processed/recognition"))
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--train-ratio", type=float, default=0.8)
    parser.add_argument("--no-link", action="store_true", help="Copy instead of symlink")
    args = parser.parse_args()

    label_dir = args.raw / "EALPR- LP characters dataset" / "Characters Labeling"
    image_dir = args.raw / "EALPR- Plates dataset"
    classes_path = label_dir / "classes.txt"

    if not label_dir.exists() or not image_dir.exists():
        raise SystemExit("Characters labels or plate images not found. Check --raw path.")
    if not classes_path.exists():
        raise SystemExit("classes.txt not found in Characters Labeling.")

    out_dir = args.out
    if out_dir.exists():
        shutil.rmtree(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    use_link = not args.no_link

    classes = load_charset(classes_path)
    (out_dir / "charset.txt").write_text("\n".join(classes), encoding="utf-8")

    records = prepare_records(label_dir, image_dir, out_dir, use_link)
    if not records:
        raise SystemExit("No labeled plates found for recognition dataset.")

    train_records, val_records = split_records(records, args.seed, args.train_ratio)
    split_dir = out_dir / "splits"
    write_split(train_records, split_dir, "train", use_link)
    write_split(val_records, split_dir, "val", use_link)
    write_data_yaml(out_dir, split_dir, classes)

    print(f"Prepared {len(records)} plate images for recognition")
    print(f"Data config written to {out_dir / 'data.yaml'}")


if __name__ == "__main__":
    main()
