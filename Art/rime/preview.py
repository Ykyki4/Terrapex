"""Inspection only: lay PNGs out at a scale on a dark and a snowy background.

uv run --with pillow --python 3.12 python Art/rime/preview.py out.png K file1 file2 ...
Paths are relative to the repo root. Each sprite is shown twice: on a cavern dark and on
a pale snow tone, because a snow sprite that only reads on black has failed.
"""
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
out_path, K, files = sys.argv[1], int(sys.argv[2]), sys.argv[3:]
ims = [Image.open(ROOT / f).convert("RGBA") for f in files]
W = sum(i.width * K + 16 for i in ims) + 16
H = max(i.height * K for i in ims) * 2 + 48
out = Image.new("RGBA", (W, H), (34, 30, 48, 255))
half = H // 2
out.paste((196, 210, 226, 255), (0, half, W, H))
x = 16
for im in ims:
    big = im.resize((im.width * K, im.height * K), Image.NEAREST)
    out.alpha_composite(big, (x, 16))
    out.alpha_composite(big, (x, half + 16))
    x += im.width * K + 16
out.save(ROOT / out_path)
# the 2x2 check: every aligned 2x2 block must be one colour
for f, im in zip(files, ims):
    px = im.load()
    bad = 0
    tot = 0
    for y in range(0, im.height - 1, 2):
        for x0 in range(0, im.width - 1, 2):
            c = {px[x0, y], px[x0 + 1, y], px[x0, y + 1], px[x0 + 1, y + 1]}
            tot += 1
            if len(c) > 1:
                bad += 1
    print(f"{f}: {im.width}x{im.height}, non-uniform 2x2 blocks {bad}/{tot}")
