"""Inspection only: lays the Old Shellback's sheets and icons out on a beach-coloured card.

uv run --with pillow --python 3.12 python Art/shellback/preview.py [out.png]

Also checks the Calamity rule: every aligned 2x2 block of every sprite is one colour.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SPRITES = [
    ("Content/NPCs/Bosses/OldShellback.png", 12),
    ("Content/NPCs/Bosses/OldShellback_Head_Boss.png", 1),
    ("Content/NPCs/Ocean/BarnacleCrab.png", 5),
    ("Content/NPCs/Ocean/ScavengerGull.png", 4),
    ("Content/Projectiles/Shellback/ShellbackBubble.png", 1),
    ("Content/Projectiles/Shellback/ShellbackCannonball.png", 1),
    ("Content/Projectiles/Shellback/ShellbackWave.png", 3),
    ("Content/Projectiles/Shellback/BarnacleShrapnel.png", 1),
    ("Content/Projectiles/Shellback/ConchBubble.png", 1),
    ("Content/Projectiles/Shellback/ConchDroplet.png", 1),
    ("Content/Projectiles/Shellback/UndertowWhirl.png", 1),
    ("Content/Projectiles/Shellback/PotCrabMinion.png", 4),
    ("Content/Items/Weapons/Shellback/WreckersCutlass.png", 1),
    ("Content/Items/Weapons/Shellback/BarnacleBlunderbuss.png", 1),
    ("Content/Items/Weapons/Shellback/BrineConch.png", 1),
    ("Content/Items/Weapons/Shellback/RustedCrabPot.png", 1),
    ("Content/Items/Weapons/Shellback/Undertow.png", 1),
    ("Content/Items/Consumables/BarnacledBell.png", 1),
    ("Content/Items/Consumables/ShellbackBag.png", 1),
    ("Content/Items/Armor/OldShellbackMask.png", 1),
    ("Content/Items/Accessories/HermitsHull.png", 1),
    ("Content/Items/Materials/BarnacleCluster.png", 1),
    ("Content/Buffs/HermitShellBuff.png", 1),
    ("Content/Buffs/PotCrabBuff.png", 1),
]
K = 3
BG = (70, 120, 140, 255)
CARD = (226, 204, 150, 255)


def check(img):
    w, h = img.size
    bad = 0
    for y in range(0, h - 1, 2):
        for x in range(0, w - 1, 2):
            px = {img.getpixel((x + i, y + j)) for i in (0, 1) for j in (0, 1)}
            if len(px) > 1:
                bad += 1
    return bad


def main():
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "Art/shellback/preview.png"
    tiles = []
    for rel, frames in SPRITES:
        p = ROOT / rel
        if not p.exists():
            print("missing", rel)
            continue
        img = Image.open(p).convert("RGBA")
        bad = check(img)
        print(f"{rel}: {img.size} frames={frames} off-grid blocks={bad}")
        fh = img.height // frames
        per = max(1, min(frames, 1700 // ((img.width + 4) * K)))
        rows = (frames + per - 1) // per
        strip = Image.new("RGBA", (img.width * per + 4 * (per - 1), fh * rows + 4 * (rows - 1)), (0, 0, 0, 0))
        for f in range(frames):
            strip.paste(img.crop((0, f * fh, img.width, (f + 1) * fh)),
                        ((f % per) * (img.width + 4), (f // per) * (fh + 4)))
        tiles.append(strip.resize((strip.width * K, strip.height * K), Image.NEAREST))
    width = 1800
    x = y = 10
    rowh = 0
    placed = []
    for t in tiles:
        if x + t.width > width - 10:
            x = 10
            y += rowh + 10
            rowh = 0
        placed.append((t, x, y))
        x += t.width + 10
        rowh = max(rowh, t.height)
    sheet = Image.new("RGBA", (width, y + rowh + 10), BG)
    d = ImageDraw.Draw(sheet)
    for t, x, y in placed:
        d.rectangle((x - 3, y - 3, x + t.width + 2, y + t.height + 2), fill=CARD)
        sheet.alpha_composite(t, (x, y))
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()
