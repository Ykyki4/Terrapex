"""Contact sheet of the two accessory trees, for inspection only:

    uv run --with pillow --python 3.12 python Art/trees/preview_accessories.py

Two rows (boots line, aegis line) at 1x and 4x on a cavern-dark and a sky-light background,
so a dark bottom edge that reads as prongs or a pale icon that vanishes both show.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
DIR = ROOT / "Content/Items/Accessories/Trees"
ROWS = [
    ["../DustyBoots", "TidewalkerSandals", "SandstriderWraps", "RimetreadBoots", "WayfarersBoots",
     "RiftstrideBoots", "ThreadstepBoots", "WorldwalkerBoots"],
    ["GullFeather", "SquallAegis", "RiftAegis", "WeaversAegis", "RegardAegis"],
]
S = 4
CELL = 46 * S
sheet = Image.new("RGBA", (CELL * 8 + 20, (CELL + 60) * 2 + 20), (22, 18, 34, 255))
d = ImageDraw.Draw(sheet)
for r, row in enumerate(ROWS):
    for c, name in enumerate(row):
        p = DIR / f"{name}.png"
        x0, y0 = 10 + c * CELL, 10 + r * (CELL + 60)
        bg = (40, 34, 56, 255) if (c + r) % 2 == 0 else (120, 150, 190, 255)
        d.rectangle([x0, y0, x0 + CELL - 4, y0 + CELL - 4], fill=bg)
        if not p.exists():
            d.text((x0 + 4, y0 + 4), "missing", fill=(255, 80, 80, 255))
            continue
        im = Image.open(p).convert("RGBA")
        big = im.resize((im.width * S, im.height * S), Image.NEAREST)
        sheet.alpha_composite(big, (x0 + (CELL - big.width) // 2, y0 + (CELL - big.height) // 2))
        sheet.alpha_composite(im, (x0 + 4, y0 + CELL + 6))
        d.text((x0 + im.width + 10, y0 + CELL + 8), f"{name.split('/')[-1]} {im.width}x{im.height}",
               fill=(230, 230, 230, 255))
out = ROOT / "Art/trees/preview_accessories.png"
sheet.save(out)
print(out)
