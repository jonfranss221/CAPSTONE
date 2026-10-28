#!/usr/bin/env python3
"""
Imports the roster v3 character art into the Unity project.

For every unit in Docs/NewCharacters/thermo_tactics_roster_v3 it writes, into
Assets/ThermoTactics/Art/Units/<Folder>/ (bosses: Art/Bosses/<Folder>/):
  idle_00..11.png    12 frames sliced from <name>_idle_12f.png
  attack_00..11.png  12 frames sliced from <name>_attack_12f.png
  hurt_00..07.png    8 frames made from idle frame 0: a white flash, then a red tint that fades
                     while the unit is knocked back 5 px and slides back (same as the original 13 units)
  card.png           the deploy-bar portrait

Usage (from the project folder):
  python Tools/import_roster_v3.py            import the units that are not in the game yet
  python Tools/import_roster_v3.py --all      re-import all 30 (overwrites the existing frames)
  python Tools/import_roster_v3.py --check    rebuild the 13 original units in memory and compare with the project
Requires Pillow (pip install pillow). Unity creates the .meta files on the next import.
"""
import argparse
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Docs", "NewCharacters", "thermo_tactics_roster_v3")
ART = os.path.join(ROOT, "Assets", "ThermoTactics", "Art")

# (roster folder, Unity art folder, is boss)
ROSTER = [
    ("allies/A01_mangrove_guardian", "MangroveGuardian", False),
    ("allies/A02_coral_defender", "CoralDefender", False),
    ("allies/A03_rice_terrace_warden", "RiceTerraceWarden", False),
    ("allies/A04_forest_spirit", "ForestSpirit", False),
    ("allies/A05_solar_sentinel", "SolarSentinel", False),
    ("allies/A06_wind_caller", "WindCaller", False),
    ("allies/A07_hydro_guardian", "HydroGuardian", False),
    ("allies/A08_geothermal_sentinel", "GeothermalSentinel", False),
    ("allies/A09_bamboo_warden", "BambooWarden", False),
    ("allies/A10_seagrass_healer", "SeagrassHealer", False),
    ("allies/A11_tarsier_scout", "TarsierScout", False),
    ("allies/A12_sea_turtle_guardian", "SeaTurtleGuardian", False),
    ("allies/A13_eagle_striker", "EagleStriker", False),
    ("allies/A14_reef_sentinel", "ReefSentinel", False),
    ("allies/A15_rainforest_ancient", "RainforestAncient", False),
    ("enemies/E01_coal_brute", "CoalBrute", False),
    ("enemies/E02_diesel_drone", "DieselDrone", False),
    ("enemies/E03_smog_wraith", "SmogWraith", False),
    ("enemies/E04_deforestation_beast", "DeforestationBeast", False),
    ("enemies/E05_plastic_golem", "PlasticGolem", False),
    ("enemies/E06_factory_titan", "FactoryTitan", True),
    ("enemies/E07_oil_slick", "OilSlick", False),
    ("enemies/E08_mining_ravager", "MiningRavager", False),
    ("enemies/E09_illegal_logger", "IllegalLogger", False),
    ("enemies/E10_quarry_colossus", "QuarryColossus", False),
    ("enemies/E11_toxic_runoff", "ToxicRunoff", False),
    ("enemies/E12_methane_cloud", "MethaneCloud", False),
    ("enemies/E13_incinerator_fiend", "IncineratorFiend", False),
    ("enemies/E14_landfill_leviathan", "LandfillLeviathan", False),
    ("enemies/E15_typhoon_tyrant", "TyphoonTyrant", True),
]
FRAMES = 12

# Hurt frames: (x offset in px, amount of tint, tint colour). Measured from the original 13 units.
WHITE, RED = (255, 255, 255), (230, 30, 30)
HURT = [(0, 0.85, WHITE), (-3, 0.45, WHITE), (-5, 0.55, RED), (-4, 0.45, RED),
        (-3, 0.30, RED), (-2, 0.18, RED), (-1, 0.08, RED), (0, 0.0, RED)]


def slice_strip(path):
    strip = Image.open(path).convert("RGBA")
    w = strip.width // FRAMES
    return [strip.crop((i * w, 0, (i + 1) * w, strip.height)) for i in range(FRAMES)]


def hurt_frames(base):
    out = []
    px = base.load()
    for dx, t, (cr, cg, cb) in HURT:
        frame = Image.new("RGBA", base.size, (0, 0, 0, 0))
        fp = frame.load()
        for y in range(base.height):
            for x in range(base.width):
                sx = x - dx
                if 0 <= sx < base.width:
                    r, g, b, a = px[sx, y]
                    if a:
                        fp[x, y] = (int(r * (1 - t) + cr * t), int(g * (1 - t) + cg * t), int(b * (1 - t) + cb * t), a)
        out.append(frame)
    return out


def build(entry):
    src_dir, folder, boss = entry
    name = src_dir.split("/")[1].split("_", 1)[1]
    files = {}
    idle = slice_strip(os.path.join(SRC, src_dir, f"{name}_idle_12f.png"))
    attack = slice_strip(os.path.join(SRC, src_dir, f"{name}_attack_12f.png"))
    for i, f in enumerate(idle):
        files[f"idle_{i:02d}.png"] = f
    for i, f in enumerate(attack):
        files[f"attack_{i:02d}.png"] = f
    for i, f in enumerate(hurt_frames(idle[0])):
        files[f"hurt_{i:02d}.png"] = f
    files["card.png"] = Image.open(os.path.join(SRC, "cards", f"{src_dir.split('/')[1]}_card.png")).convert("RGBA")
    out_dir = os.path.join(ART, "Bosses" if boss else "Units", folder)
    return out_dir, files


def same(a, b):
    return a.size == b.size and a.tobytes() == b.tobytes()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--check", action="store_true")
    args = ap.parse_args()

    if args.check:
        bad = 0
        for entry in ROSTER:
            out_dir, files = build(entry)
            if not os.path.isdir(out_dir) or not os.path.exists(os.path.join(out_dir, "hurt_07.png")):
                continue
            diffs = [n for n, img in files.items()
                     if not os.path.exists(os.path.join(out_dir, n)) or not same(img, Image.open(os.path.join(out_dir, n)).convert("RGBA"))]
            print(f"{entry[1]:20s} {'OK' if not diffs else 'DIFFERENT: ' + ', '.join(diffs[:6])}")
            bad += bool(diffs)
        sys.exit(1 if bad else 0)

    for entry in ROSTER:
        out_dir, files = build(entry)
        if os.path.isdir(out_dir) and not args.all:
            print(f"skip   {entry[1]} (already in the project)")
            continue
        os.makedirs(out_dir, exist_ok=True)
        for n, img in files.items():
            img.save(os.path.join(out_dir, n))
        print(f"import {entry[1]} -> {os.path.relpath(out_dir, ROOT)} ({len(files)} files)")


if __name__ == "__main__":
    main()
