"""Inspection only: lay out the frames of a vertical sheet side by side, scaled.

uv run --with pillow --python 3.12 python Art/rime/frames.py sheet.png frame_h K out.png [bg]
"""
import sys
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).resolve().parents[2]
src, fh, K, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
bg = (196, 210, 226, 255) if len(sys.argv) > 5 and sys.argv[5] == "snow" else (34, 30, 48, 255)
im = Image.open(ROOT / src).convert("RGBA")
n = im.height // fh
o = Image.new("RGBA", (n * (im.width * K + 8) + 8, fh * K + 16), bg)
for i in range(n):
    f = im.crop((0, i * fh, im.width, i * fh + fh)).resize((im.width * K, fh * K), Image.NEAREST)
    o.alpha_composite(f, (8 + i * (im.width * K + 8), 8))
o.save(ROOT / out)
