"""Contact sheets for the T0 textures, read straight out of Content/.

    uv run --with pillow --python 3.12 python Art/generators/preview_t0.py

Inspection only. The art itself is drawn by hand in LibreSprite; the PNGs under
Content/ are the source of truth, and there is no generator to re-run.
Writes Art/preview/t0_{items,tiles,equip,body_grid}.png.
"""
import os

from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
OUT = os.path.join(ROOT, "Art", "preview")
os.makedirs(OUT, exist_ok=True)

ITEMS = [
    "Items/Placeable/FissuriteOre", "Items/Materials/FissuriteBar",
    "Items/Materials/RiftDust", "Items/Placeable/CrackedStone",
    "Items/Placeable/RiftTorch", "Items/Consumables/FissurePotion",
    "Items/Tools/FissuritePickaxe", "Items/Tools/FissuriteAxe",
    "Items/Tools/FissuriteHammer", "Items/Weapons/Skol",
    "Items/Weapons/Wedge", "Items/Weapons/Crackthrower",
    "Items/Weapons/DustBeam", "Items/Weapons/DustLash",
    "Projectiles/WedgeSpear", "Projectiles/DustBeamShot",
    "Projectiles/DustLashWhip", "Items/Armor/FissuriteHelmet",
    "Items/Armor/FissuriteBreastplate", "Items/Armor/FissuriteGreaves",
    "Items/Accessories/PocketShard", "Items/Accessories/DustyBoots",
    "Buffs/Cracked", "Buffs/FissureSight",
]
BG = (28, 24, 36, 255)


def load(rel):
    return Image.open(os.path.join(ROOT, "Content", rel + ".png")).convert("RGBA")


def items(zoom=4, cell=190, cols=6):
    rows = (len(ITEMS) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * cell), BG)
    for i, rel in enumerate(ITEMS):
        im = load(rel)
        im = im.resize((im.width * zoom, im.height * zoom), Image.NEAREST)
        if im.height > cell - 8:
            s = (cell - 8) / im.height
            im = im.resize((max(1, int(im.width * s)), cell - 8), Image.NEAREST)
        sheet.alpha_composite(im, ((i % cols) * cell + (cell - im.width) // 2,
                                   (i // cols) * cell + (cell - im.height) // 2))
    sheet.save(os.path.join(OUT, "t0_items.png"))


def tiles(zoom=4):
    """Lay the block frames edge to edge — seams and repetition only show tiled."""
    out = Image.new("RGBA", (2 * (16 * 4 * zoom + 20), 16 * 4 * zoom + 20), BG)
    for k, name in enumerate(("FissuriteOreTile", "CrackedStoneTile")):
        s = load("Tiles/" + name)
        grid = Image.new("RGBA", (64, 64))
        for r in range(4):
            for c in range(4):
                fx, fy = (5 + (c + r) % 8) * 18, (5 + r % 6) * 18
                grid.paste(s.crop((fx, fy, fx + 16, fy + 16)), (c * 16, r * 16))
        out.alpha_composite(grid.resize((64 * zoom, 64 * zoom), Image.NEAREST),
                            (10 + k * (16 * 4 * zoom + 20), 10))
    out.save(os.path.join(OUT, "t0_tiles.png"))


def equip(zoom=4, frames=(0, 1, 6, 9, 12, 15, 18)):
    """Rough composite of the equip sheets, the way they stack on a player.

    Head and legs are 40x1120 strips indexed by frame; the body is the 1.4.4
    composite grid (360x224), whose cell (0,0) holds the plain torso. The arm
    cells are pose-dependent and are not reconstructed here — only the game can
    show those, so this preview is for placement, not for the animation.
    """
    head = load("Items/Armor/FissuriteHelmet_Head")
    legs = load("Items/Armor/FissuriteGreaves_Legs")
    torso = load("Items/Armor/FissuriteBreastplate_Body").crop((0, 0, 40, 56))
    out = Image.new("RGBA", (len(frames) * (40 * zoom + 8) + 8, 56 * zoom + 16), BG)
    for i, f in enumerate(frames):
        comp = Image.new("RGBA", (40, 56), (0, 0, 0, 0))
        comp.alpha_composite(legs.crop((0, f * 56, 40, f * 56 + 56)))
        comp.alpha_composite(torso)
        comp.alpha_composite(head.crop((0, f * 56, 40, f * 56 + 56)))
        out.alpha_composite(comp.resize((40 * zoom, 56 * zoom), Image.NEAREST),
                            (8 + i * (40 * zoom + 8), 8))
    out.save(os.path.join(OUT, "t0_equip.png"))


def body_grid(zoom=2):
    """The body sheet's 36 cells, so a wrong-format sheet is obvious at a glance."""
    b = load("Items/Armor/FissuriteBreastplate_Body")
    out = Image.new("RGBA", (b.width * zoom, b.height * zoom), BG)
    out.alpha_composite(b.resize((b.width * zoom, b.height * zoom), Image.NEAREST))
    out.save(os.path.join(OUT, "t0_body_grid.png"))


if __name__ == "__main__":
    items()
    tiles()
    equip()
    body_grid()
    print("previews -> " + os.path.normpath(OUT))
