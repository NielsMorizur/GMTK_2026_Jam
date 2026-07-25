"""Stamp the standard ground collision shape into tile sprites.

Any tile PNG in the Tiles folder whose LIVE physicsShape slot is empty gets
the calibrated flat-top rect (top at y=83 on a 256px tile, scaled for other
sizes). Already-shaped tiles are left alone. Run after adding new tiles,
then reimport the Tiles folder in Unity and toggle the TilemapCollider2D.
"""
import os
import re
import struct

TILES = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                     "GMTK_2026_Jam", "Assets", "_Parsa_Prototype", "Art", "Sprites", "Tiles")
if not os.path.isdir(TILES):  # script lives inside the project already
    TILES = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                         "Assets", "_Parsa_Prototype", "Art", "Sprites", "Tiles")

TOP_256 = 83  # calibrated surface height on a 256px tile

# tiles the player stands ON get the lowered flat top; everything that only
# fills space below/beside (undergrounds, bottom edges) is a full block so
# stacked tiles never leave seams
FILL_PATTERN = re.compile(r"under|deep|bot[lr]|underground", re.I)

def png_size(path):
    with open(path, "rb") as f:
        f.seek(16)
        return struct.unpack(">II", f.read(8))

def main():
    stamped, skipped = [], []
    for f in sorted(os.listdir(TILES)):
        if not f.endswith(".png"):
            continue
        meta_path = os.path.join(TILES, f + ".meta")
        if not os.path.exists(meta_path):
            continue
        t = open(meta_path, encoding="utf-8").read()
        empty = "\n    physicsShape: []\n"      # the LIVE top-level slot
        if empty not in t:
            skipped.append(f)
            continue
        w, h = png_size(os.path.join(TILES, f))
        hx, hy = w // 2, h // 2
        top = hy if FILL_PATTERN.search(f) else round(TOP_256 * h / 256)
        shape = ("\n    physicsShape:\n"
                 "    - - {x: %d, y: %d}\n"
                 "      - {x: -%d, y: %d}\n"
                 "      - {x: -%d, y: -%d}\n"
                 "      - {x: %d, y: -%d}\n" % (hx, top, hx, top, hx, hy, hx, hy))
        t = t.replace(empty, shape, 1)
        tmp = meta_path + ".tmpw"
        open(tmp, "w", newline="\n", encoding="utf-8").write(t)
        os.replace(tmp, meta_path)
        stamped.append("%s (%dx%d, top %d)" % (f, w, h, top))
    print("stamped:", len(stamped))
    for s in stamped:
        print("  ", s)
    print("already shaped / untouched:", len(skipped))

if __name__ == "__main__":
    main()
