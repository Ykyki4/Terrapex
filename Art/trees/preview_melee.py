"""Contact sheet of the melee tree (Confluence), for inspection only:

    uv run --with pillow --python 3.12 python Art/trees/preview_melee.py

Row 1: the four ingredient swords (Cutlass, Khopesh, Glacierbreaker, Bonefissure) and the later
ingredients (Cleaver, Warp, Nothing). Row 2: the four nodes at 4x on dark and light ground, with
the 1x sprite under each and the share of aligned 2x2 blocks that are one colour (must be 100%).
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
W = ROOT / "Content/Items/Weapons"
ROWS = [
    [W / "Shellback/WreckersCutlass.png", W / "Scarab/SultansKhopesh.png", W / "Rime/Glacierbreaker.png",
     W / "Bonefissure.png", W / "RiftshardCleaver.png", W / "Warp.png", W / "Nothing.png"],
    [W / "Trees/Melee/Confluence.png", W / "Trees/Melee/TrueConfluence.png", W / "Trees/Melee/Worldseam.png",
     W / "Trees/Melee/Meridian.png", ROOT / "Content/Projectiles/Trees/Melee/ConfluenceWave.png"],
]


def aligned(im):
    px = im.load()
    total = same = 0
    for y in range(0, im.height - 1, 2):
        for x in range(0, im.width - 1, 2):
            c = {px[x, y], px[x + 1, y], px[x, y + 1], px[x + 1, y + 1]}
            if all(p[3] == 0 for p in c):
                continue
            total += 1
            same += len(c) == 1
    return 100.0 * same / max(1, total)


S = 4
CELL = 84 * S
sheet = Image.new("RGBA", (CELL * 7 + 20, (CELL + 110) * 2 + 20), (22, 18, 34, 255))
d = ImageDraw.Draw(sheet)
for r, row in enumerate(ROWS):
    for c, p in enumerate(row):
        x0, y0 = 10 + c * CELL, 10 + r * (CELL + 110)
        bg = (40, 34, 56, 255) if (c + r) % 2 == 0 else (120, 150, 190, 255)
        d.rectangle([x0, y0, x0 + CELL - 4, y0 + CELL - 4], fill=bg)
        if not p.exists():
            d.text((x0 + 4, y0 + 4), "missing", fill=(255, 80, 80, 255))
            continue
        im = Image.open(p).convert("RGBA")
        big = im.resize((im.width * S, im.height * S), Image.NEAREST)
        sheet.alpha_composite(big, (x0 + (CELL - big.width) // 2, y0 + (CELL - big.height) // 2))
        sheet.alpha_composite(im, (x0 + 4, y0 + CELL + 6))
        d.text((x0 + im.width + 10, y0 + CELL + 8), f"{p.stem} {im.width}x{im.height}", fill=(230, 230, 230, 255))
        d.text((x0 + im.width + 10, y0 + CELL + 22), f"2x2 {aligned(im):.0f}%", fill=(230, 230, 230, 255))
out = ROOT / "Art/trees/preview_melee.png"
sheet.save(out)
print(out)
