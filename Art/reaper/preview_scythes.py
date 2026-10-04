"""Inspection only: every scythe in the mod on one sheet, with the numbers read from the code.

uv run --with pillow --python 3.12 python Art/reaper/preview_scythes.py
"""
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
WEAPONS = ROOT / "Content/Items/Weapons"

# (class path, Russian name, where it comes from) - in progression order
SCYTHES = [
    ("Reaper/RustedSickle", "Ржавый серп", "крафт · дерево + железо"),
    ("Reaper/FrostbiteSickle", "Морозный серп", "ледяные сундуки и слизни"),
    ("Reaper/Gravedigger", "Могильщик", "Кровавая луна"),
    ("Reaper/Sandsweeper", "Пескомёт", "крафт · жвала + окаменелость"),
    ("Reaper/FissuriteScythe", "Трещинитовая коса", "T0 · трещинит"),
    ("Reaper/Lidcutter", "Векорез", "Дремлющее Око"),
    ("Reaper/GlassReaper", "Стеклорез", "T1 · разломное стекло"),
    ("Reaper/Rotreaper", "Гнилорез", "крафт · демонит"),
    ("Reaper/Veinreaper", "Жилорез", "крафт · кримтан"),
    ("Reaper/Hivecutter", "Ульерез", "Королева пчёл"),
    ("Reaper/Thornreaper", "Терновник", "крафт · джунгли"),
    ("Reaper/Kusarigama", "Кусаригама", "лавка Ронина"),
    ("Reaper/Hellreaper", "Адорез", "крафт · адский камень"),
    ("RiftScythe", "Разломная коса", "T4 · пока ближний бой"),
]
PER_ROW = 7

K = 5
BG = (36, 31, 50, 255)
CARD = (52, 45, 70, 255)
INK = (240, 232, 250, 255)
DIM = (170, 160, 190, 255)
GOLD = (245, 192, 74, 255)


def font(size, bold=False):
    for name in (("segoeuib.ttf" if bold else "segoeui.ttf"), "arial.ttf"):
        try:
            return ImageFont.truetype(f"C:/Windows/Fonts/{name}", size)
        except OSError:
            continue
    return ImageFont.load_default()


def number(src, pattern):
    m = re.search(pattern, src)
    return m.group(1) if m else "?"


cards = []
for path, ru, where in SCYTHES:
    src = (WEAPONS / f"{path}.cs").read_text(encoding="utf-8")
    cards.append({
        "name": Path(path).name, "ru": ru, "where": where,
        "icon": Image.open(WEAPONS / f"{path}.png").convert("RGBA"),
        "damage": number(src, r"Item\.damage\s*=\s*(\d+)"),
        "use": number(src, r"Item\.useTime\s*=\s*(\d+)"),
        "reach": number(src, r"Reach\s*=>\s*([\d.]+)f"),
    })

ICON_BOX = 52 * K
CW, CH = ICON_BOX + 24, ICON_BOX + 150
PAD = 20
rows = (len(cards) + PER_ROW - 1) // PER_ROW
W = PAD + PER_ROW * (CW + PAD)
H = 100 + rows * (CH + PAD)
sheet = Image.new("RGBA", (W, H), BG)
d = ImageDraw.Draw(sheet)
d.text((PAD, 18), "Косы Terrapex", font=font(40, True), fill=INK)
d.text((PAD, 66), "класс «Жнец» · кромка всегда критует · Сбор по ПКМ при полной Жатве · по порядку прогрессии",
       font=font(20), fill=DIM)

for i, c in enumerate(cards):
    x0 = PAD + (i % PER_ROW) * (CW + PAD)
    y0 = 100 + (i // PER_ROW) * (CH + PAD)
    d.rounded_rectangle([x0, y0, x0 + CW, y0 + CH], radius=14, fill=CARD)
    ic = c["icon"].resize((c["icon"].width * K, c["icon"].height * K), Image.NEAREST)
    sheet.alpha_composite(ic, (x0 + (CW - ic.width) // 2, y0 + 14 + (ICON_BOX - ic.height)))
    sheet.alpha_composite(c["icon"], (x0 + CW - c["icon"].width - 10, y0 + 10))
    ty = y0 + ICON_BOX + 26
    d.text((x0 + 14, ty), c["ru"], font=font(23, True), fill=INK)
    d.text((x0 + 14, ty + 32), c["where"], font=font(16, True), fill=GOLD)
    reach = f" · длина {c['reach']}" if c["reach"] != "?" else ""
    d.text((x0 + 14, ty + 58), f"урон {c['damage']} · время {c['use']}{reach}", font=font(16), fill=INK)
    d.text((x0 + 14, ty + 82), c["name"], font=font(14), fill=DIM)

out = ROOT / "Art/reaper/scythes.png"
sheet.save(out)
print(out, sheet.size)
