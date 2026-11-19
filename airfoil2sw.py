#!/usr/bin/env python3
"""
Конвертер CSV с airfoiltools.com в файл точек для SolidWorks
("Кривая через точки XYZ" / Curve Through XYZ Points).

Использование:
    python airfoil2sw.py naca2412.csv
    python airfoil2sw.py *.csv                 # несколько файлов сразу
    python airfoil2sw.py naca2412.csv -o out.sldcrv
    python airfoil2sw.py naca2412.csv --format csv   # вывод через запятую
"""

import argparse
import csv
import sys
from pathlib import Path


def is_number(s: str) -> bool:
    try:
        float(s)
        return True
    except ValueError:
        return False


def extract_surface(path: Path):
    """Возвращает список (x, y) только из раздела 'Airfoil surface'."""
    points = []
    in_surface = False

    with open(path, newline="", encoding="utf-8-sig") as f:
        for row in csv.reader(f):
            # убираем пробелы и пустые ячейки
            cells = [c.strip() for c in row if c.strip()]

            if not cells:
                # пустая строка после точек = конец раздела
                if in_surface and points:
                    break
                continue

            first = cells[0].lower()

            if not in_surface:
                if first.startswith("airfoil surface"):
                    in_surface = True
                continue

            # мы внутри раздела Airfoil surface
            if len(cells) >= 2 and is_number(cells[0]) and is_number(cells[1]):
                points.append((float(cells[0]), float(cells[1])))
            elif points:
                # встретили новый заголовок (Camber, Chord и т.д.) — стоп
                break
            # иначе это строка заголовка столбцов "X(mm),Y(mm)" — пропускаем

    if not points:
        raise ValueError(f"В файле {path.name} не найден раздел 'Airfoil surface'")
    return points


def fmt(v: float) -> str:
    # убираем "-0" и лишние нули
    if abs(v) < 1e-12:
        v = 0.0
    return f"{v:.6f}".rstrip("0").rstrip(".") if "." in f"{v:.6f}" else f"{v}"


def write_output(points, out_path: Path, fmt_type: str):
    sep = "," if fmt_type == "csv" else "\t"
    with open(out_path, "w", encoding="utf-8", newline="") as f:
        for x, y in points:
            f.write(f"{fmt(x)}mm{sep}{fmt(y)}mm{sep}0mm\n")


def main():
    p = argparse.ArgumentParser(description="airfoiltools.com CSV -> SolidWorks XYZ")
    p.add_argument("files", nargs="+", type=Path, help="входные CSV файлы")
    p.add_argument("-o", "--output", type=Path,
                   help="имя выходного файла (только для одного входного)")
    p.add_argument("--format", choices=["txt", "sldcrv", "csv"], default="txt",
                   help="формат вывода (по умолчанию txt, разделитель — табуляция)")
    args = p.parse_args()

    if args.output and len(args.files) > 1:
        sys.exit("Опция -o работает только с одним входным файлом")

    for src in args.files:
        try:
            pts = extract_surface(src)
        except (ValueError, OSError) as e:
            print(f"[ОШИБКА] {e}")
            continue

        out = args.output or src.with_name(f"{src.stem}_sw.{args.format}")
        write_output(pts, out, args.format)
        print(f"[OK] {src.name} -> {out.name} ({len(pts)} точек)")


if __name__ == "__main__":
    main()
