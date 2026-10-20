"""Contact sheet for the Scarab Sultan batch - inspection only, never paints a shipping pixel.

    uv run --with pillow --python 3.12 python Art/scarab/preview.py [out.png] [scale] [paths...]

Each PNG is laid out at `scale`x on a desert-sand and a cavern-dark swatch side by side,
with the share of aligned 2x2 blocks that are a single colour printed next to it - it must
be 100% for the Calamity grid.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]

DEFAULT = [
    "Content/NPCs/Bosses/ScarabSultan.png",
    "Content/NPCs/Bosses/ScarabSultan_Head_Boss.png",
    "Content/Projectiles/Scarab/SunBall.png",
]


def grid_share(im):
    im = im.convert("RGBA")
    w, h = im.size
    px = im.load()
    total = ok = 0
    for y in range(0, h - 1, 2):
        for x in range(0, w - 1, 2):
            c = {px[x, y], px[x + 1, y], px[x, y + 1], px[x + 1, y + 1]}
            if all(p[3] == 0 for p in c):
                continue
            total += 1
            ok += len(c) == 1
    return ok / total if total else 1.0, w % 2 == 0 and h % 2 == 0


def main():
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "Art/scarab/preview.png"
    scale = int(sys.argv[2]) if len(sys.argv) > 2 else 3
    paths = sys.argv[3:] or DEFAULT
    tiles = []
    for p in paths:
        im = Image.open(ROOT / p).convert("RGBA")
        share, even = grid_share(im)
        big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        pad = 8
        tile = Image.new("RGBA", (big.width * 2 + pad * 3, big.height + pad * 2 + 14), (40, 30, 40, 255))
        tile.paste((222, 190, 130, 255), (pad, pad, pad + big.width, pad + big.height))
        tile.paste((30, 24, 34, 255), (pad * 2 + big.width, pad, pad * 2 + big.width * 2, pad + big.height))
        tile.alpha_composite(big, (pad, pad))
        tile.alpha_composite(big, (pad * 2 + big.width, pad))
        d = ImageDraw.Draw(tile)
        d.text((pad, big.height + pad + 1), f"{Path(p).name} {im.width}x{im.height} grid {share*100:.0f}%"
               + ("" if even else " ODD"), fill=(255, 255, 255, 255))
        tiles.append(tile)
        print(f"{p}: {im.width}x{im.height} 2x2 {share*100:.1f}%{'' if even else ' ODD SIZE'}")
    # pack in rows up to 1800 wide
    rows, row, rw = [], [], 0
    for t in tiles:
        if row and rw + t.width > 1800:
            rows.append(row)
            row, rw = [], 0
        row.append(t)
        rw += t.width
    if row:
        rows.append(row)
    W = max(sum(t.width for t in r) for r in rows)
    H = sum(max(t.height for t in r) for r in rows)
    sheet = Image.new("RGBA", (W, H), (40, 30, 40, 255))
    y = 0
    for r in rows:
        x = 0
        for t in r:
            sheet.alpha_composite(t, (x, y))
            x += t.width
        y += max(t.height for t in r)
    sheet.save(out)
    print("->", out)


if __name__ == "__main__":
    main()
