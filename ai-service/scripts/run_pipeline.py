from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path
from typing import Any

import yaml


def load_config(path: Path) -> dict[str, Any]:
    return yaml.safe_load(path.read_text(encoding="utf-8"))


def normalize_list(value: Any) -> list[str]:
    if value is None:
        return []
    if isinstance(value, list):
        return [str(item) for item in value]
    return [str(value)]


def should_run(name: str, only: set[str], skip: set[str]) -> bool:
    if only and name not in only:
        return False
    if skip and name in skip:
        return False
    return True


def run_step(base_dir: Path, step: dict[str, Any]) -> None:
    name = step.get("name", "<unnamed>")
    script = step.get("script")
    if not script:
        raise SystemExit(f"Step '{name}' missing 'script'.")

    args = normalize_list(step.get("args"))
    env = os.environ.copy()
    env.update({str(k): str(v) for k, v in (step.get("env") or {}).items()})

    script_path = base_dir / script
    command = [sys.executable, str(script_path), *args]
    print(f"[pipeline] {name}: {' '.join(command)}")
    subprocess.run(command, check=True, env=env)


def main() -> None:
    parser = argparse.ArgumentParser(description="Run ALPR pipeline steps from YAML")
    parser.add_argument(
        "--config",
        type=Path,
        default=Path("configs/pipeline_run.yaml"),
        help="Pipeline YAML config",
    )
    parser.add_argument(
        "--only",
        type=str,
        default="",
        help="Comma-separated step names to run",
    )
    parser.add_argument(
        "--skip",
        type=str,
        default="",
        help="Comma-separated step names to skip",
    )
    args = parser.parse_args()

    base_dir = Path(__file__).resolve().parents[1]
    config_path = base_dir / args.config
    config = load_config(config_path)

    only = {name.strip() for name in args.only.split(",") if name.strip()}
    skip = {name.strip() for name in args.skip.split(",") if name.strip()}

    steps = config.get("steps", [])
    if not steps:
        raise SystemExit("No steps defined in pipeline config.")

    for step in steps:
        name = step.get("name", "<unnamed>")
        if not step.get("enabled", True):
            print(f"[pipeline] skip disabled: {name}")
            continue
        if not should_run(name, only, skip):
            print(f"[pipeline] skip filtered: {name}")
            continue
        run_step(base_dir, step)


if __name__ == "__main__":
    main()
