"""Inspection only: the reaper items at 8x next to the Rift Scythe they descend from.

uv run --with pillow --python 3.12 python Art/reaper/preview.py
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
FILES = [
    "Content/Items/Weapons/RiftScythe.png",
    "Content/Items/Weapons/Reaper/RustedSickle.png",
    "Content/Items/Weapons/Reaper/FissuriteScythe.png",
    "Content/Items/Weapons/Reaper/GlassReaper.png",
    "Content/Items/Weapons/Reaper/Kusarigama.png",
    "Content/Items/Accessories/Whetstone.png",
    "Content/Items/Accessories/ReaperEmblem.png",
]
K = 8
ims = [Image.open(ROOT / f).convert("RGBA") for f in FILES]
W = sum(i.width * K + 24 for i in ims) + 24
H = max(i.height * K for i in ims) + 48
out = Image.new("RGBA", (W, H), (46, 40, 62, 255))
x = 24
for im in ims:
    out.alpha_composite(im.resize((im.width * K, im.height * K), Image.NEAREST), (x, 24))
    x += im.width * K + 24
out.save(ROOT / "Art/reaper/preview_items.png")
