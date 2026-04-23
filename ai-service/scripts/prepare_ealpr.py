from __future__ import annotations

import argparse
import json
import random
import shutil
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

IMAGE_EXTS = {".jpg", ".jpeg", ".png", ".bmp"}


@dataclass
class Record:
    image_path: Path
    label_path: Path | None


def find_annotations(root: Path) -> dict[str, list[Path]]:
    return {
        "voc": list(root.rglob("*.xml")),
        "coco": list(root.rglob("*.json")),
        "yolo": list(root.rglob("*.txt")),
    }


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


def write_yolo_label(label_path: Path, boxes: list[tuple[float, float, float, float]]) -> None:
    label_path.parent.mkdir(parents=True, exist_ok=True)
    lines = [f"0 {x:.6f} {y:.6f} {w:.6f} {h:.6f}" for x, y, w, h in boxes]
    label_path.write_text("\n".join(lines), encoding="utf-8")


def parse_voc(xml_path: Path) -> tuple[Path, list[tuple[float, float, float, float]]]:
    tree = ET.parse(xml_path)
    root = tree.getroot()
    filename = root.findtext("filename")
    size = root.find("size")
    if filename is None or size is None:
        raise ValueError(f"Missing VOC fields in {xml_path}")
    width = float(size.findtext("width", "1"))
    height = float(size.findtext("height", "1"))
    image_path = xml_path.parent / filename

    boxes: list[tuple[float, float, float, float]] = []
    for obj in root.findall("object"):
        bbox = obj.find("bndbox")
        if bbox is None:
            continue
        x1 = float(bbox.findtext("xmin", "0"))
        y1 = float(bbox.findtext("ymin", "0"))
        x2 = float(bbox.findtext("xmax", "0"))
        y2 = float(bbox.findtext("ymax", "0"))
        x_center = ((x1 + x2) / 2.0) / width
        y_center = ((y1 + y2) / 2.0) / height
        box_w = (x2 - x1) / width
        box_h = (y2 - y1) / height
        boxes.append((x_center, y_center, box_w, box_h))
    return image_path, boxes


def parse_coco(json_path: Path) -> list[tuple[Path, list[tuple[float, float, float, float]]]]:
    payload = json.loads(json_path.read_text(encoding="utf-8"))
    images = {img["id"]: img for img in payload.get("images", [])}
    anns_by_image: dict[int, list[tuple[float, float, float, float]]] = {}
    for ann in payload.get("annotations", []):
        image = images.get(ann["image_id"])
        if not image:
            continue
        width = float(image.get("width", 1))
        height = float(image.get("height", 1))
        x, y, w, h = ann.get("bbox", [0, 0, 0, 0])
        x_center = (x + w / 2.0) / width
        y_center = (y + h / 2.0) / height
        box_w = w / width
        box_h = h / height
        anns_by_image.setdefault(ann["image_id"], []).append(
            (x_center, y_center, box_w, box_h)
        )

    results: list[tuple[Path, list[tuple[float, float, float, float]]]] = []
    for image_id, meta in images.items():
        file_name = meta.get("file_name")
        if not file_name:
            continue
        image_path = json_path.parent / file_name
        results.append((image_path, anns_by_image.get(image_id, [])))
    return results


def prepare_from_voc(annotations: Iterable[Path], out_dir: Path, use_link: bool) -> list[Record]:
    records: list[Record] = []
    for xml_path in annotations:
        image_path, boxes = parse_voc(xml_path)
        label_path = out_dir / "labels" / f"{image_path.stem}.txt"
        write_yolo_label(label_path, boxes)
        link_or_copy(image_path, out_dir / "images" / image_path.name, use_link)
        records.append(Record(image_path=out_dir / "images" / image_path.name, label_path=label_path))
    return records


def prepare_from_coco(annotations: Iterable[Path], out_dir: Path, use_link: bool) -> list[Record]:
    records: list[Record] = []
    for json_path in annotations:
        for image_path, boxes in parse_coco(json_path):
            label_path = out_dir / "labels" / f"{image_path.stem}.txt"
            write_yolo_label(label_path, boxes)
            link_or_copy(image_path, out_dir / "images" / image_path.name, use_link)
            records.append(Record(image_path=out_dir / "images" / image_path.name, label_path=label_path))
    return records


def prepare_from_yolo(labels: Iterable[Path], out_dir: Path, use_link: bool) -> list[Record]:
    records: list[Record] = []
    for label_path in labels:
        if label_path.name == "classes.txt":
            continue
        image_path = label_path.with_suffix(".jpg")
        if not image_path.exists():
            image_path = label_path.with_suffix(".png")
        if not image_path.exists():
            continue
        out_label = out_dir / "labels" / label_path.name
        link_or_copy(label_path, out_label, use_link)
        link_or_copy(image_path, out_dir / "images" / image_path.name, use_link)
        records.append(Record(image_path=out_dir / "images" / image_path.name, label_path=out_label))
    return records


def prepare_from_yolo_pairs(
    label_dir: Path, image_dir: Path, out_dir: Path, use_link: bool
) -> list[Record]:
    records: list[Record] = []
    for label_path in label_dir.glob("*.txt"):
        if label_path.name == "classes.txt":
            continue
        image_path = image_dir / f"{label_path.stem}.jpg"
        if not image_path.exists():
            image_path = image_dir / f"{label_path.stem}.png"
        if not image_path.exists():
            continue
        out_label = out_dir / "labels" / label_path.name
        link_or_copy(label_path, out_label, use_link)
        link_or_copy(image_path, out_dir / "images" / image_path.name, use_link)
        records.append(
            Record(
                image_path=out_dir / "images" / image_path.name,
                label_path=out_label,
            )
        )
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
        if record.label_path:
            link_or_copy(record.label_path, labels_dir / record.label_path.name, use_link)


def write_data_yaml(out_dir: Path, split_dir: Path) -> None:
    data_yaml = out_dir / "data.yaml"
    rel_train = Path("splits") / "images" / "train"
    rel_val = Path("splits") / "images" / "val"
    data_yaml.write_text(
        "\n".join(
            [
                f"path: {out_dir}",
                f"train: {rel_train}",
                f"val: {rel_val}",
                "names:",
                "  0: plate",
            ]
        ),
        encoding="utf-8",
    )


def main() -> None:
    parser = argparse.ArgumentParser(description="Prepare EALPR dataset for detection")
    parser.add_argument("--raw", type=Path, default=Path("dataset"))
    parser.add_argument("--out", type=Path, default=Path("data/processed/detection"))
    parser.add_argument("--labels", type=Path, default=None)
    parser.add_argument("--images", type=Path, default=None)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--train-ratio", type=float, default=0.8)
    parser.add_argument("--no-link", action="store_true", help="Copy instead of symlink")
    args = parser.parse_args()

    out_dir = args.out
    if out_dir.exists():
        shutil.rmtree(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    use_link = not args.no_link

    records: list[Record] = []
    if args.labels and args.images:
        records = prepare_from_yolo_pairs(args.labels, args.images, out_dir, use_link)
    else:
        ealpr_vehicle_dir = args.raw / "EALPR Vechicles dataset"
        ealpr_labels = ealpr_vehicle_dir / "Vehicles Labeling"
        ealpr_images = ealpr_vehicle_dir / "Vehicles"
        if ealpr_labels.exists() and ealpr_images.exists():
            records = prepare_from_yolo_pairs(ealpr_labels, ealpr_images, out_dir, use_link)
        else:
            annotations = find_annotations(args.raw)
            if annotations["voc"]:
                records = prepare_from_voc(annotations["voc"], out_dir, use_link)
            elif annotations["coco"]:
                records = prepare_from_coco(annotations["coco"], out_dir, use_link)
            elif annotations["yolo"]:
                records = prepare_from_yolo(annotations["yolo"], out_dir, use_link)
            else:
                raise SystemExit("No annotations found. Please check dataset structure.")

    if not records:
        raise SystemExit("No labeled images found after processing.")

    train_records, val_records = split_records(records, args.seed, args.train_ratio)
    split_dir = out_dir / "splits"
    write_split(train_records, split_dir, "train", use_link)
    write_split(val_records, split_dir, "val", use_link)
    write_data_yaml(out_dir, split_dir)

    print(f"Prepared {len(records)} images")
    print(f"Data config written to {out_dir / 'data.yaml'}")


if __name__ == "__main__":
    main()
