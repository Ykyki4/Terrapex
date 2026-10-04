"""Inspection only: composite the Upheaval sheets onto a few player frames.

uv run --with pillow --python 3.12 python Art/concepts/upheaval/preview.py
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).parent
K = 6
BG = (46, 40, 62, 255)

head = Image.open(HERE / "UpheavalHelm_Head.png").convert("RGBA")
body = Image.open(HERE / "UpheavalPlate_Body.png").convert("RGBA")
legs = Image.open(HERE / "UpheavalGreaves_Legs.png").convert("RGBA")


def cell(sheet, cx, cy):
    return sheet.crop((cx * 40, cy * 56, cx * 40 + 40, cy * 56 + 56))


def frame(f, arm=(2, 0), back_arm=(7, 2)):
    out = Image.new("RGBA", (40, 56))
    out.alpha_composite(cell(body, *back_arm))
    out.alpha_composite(legs.crop((0, f * 56, 40, f * 56 + 56)))
    out.alpha_composite(cell(body, 0, 0))
    out.alpha_composite(head.crop((0, f * 56, 40, f * 56 + 56)))
    out.alpha_composite(cell(body, *arm))
    return out


frames = [frame(0), frame(8, (7, 0)), frame(12, (8, 0)), frame(0, (4, 0)), frame(0, (0, 1))]
icons = [Image.open(HERE / f"{n}.png").convert("RGBA")
         for n in ("UpheavalHelm", "UpheavalPlate", "UpheavalGreaves")]

W = len(frames) * (40 * K + 16) + sum(i.width * K + 16 for i in icons) + 16
sheet = Image.new("RGBA", (W, 56 * K + 32), BG)
x = 16
for fr in frames:
    sheet.alpha_composite(fr.resize((40 * K, 56 * K), Image.NEAREST), (x, 16))
    x += 40 * K + 16
for ic in icons:
    sheet.alpha_composite(ic.resize((ic.width * K, ic.height * K), Image.NEAREST), (x, 40))
    x += ic.width * K + 16
sheet.save(HERE / "preview.png")

full = Image.new("RGBA", (360 * 3, 224 * 3), BG)
full.alpha_composite(body.resize((360 * 3, 224 * 3), Image.NEAREST))
full.save(HERE / "preview_body.png")
legstrip = Image.new("RGBA", (20 * 40 * 3, 20 * 3), BG)
for f in range(20):
    legstrip.alpha_composite(legs.crop((0, f * 56 + 36, 40, f * 56 + 56)).resize((120, 60), Image.NEAREST), (f * 120, 0))
legstrip.save(HERE / "preview_legs.png")
