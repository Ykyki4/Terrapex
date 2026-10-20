"""Inspection only: the Rime Mask's first frame composited on the vanilla player (head, eyes,
body, legs from Art/reference/vanilla_player), at 6x."""
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).resolve().parents[2]
ref = ROOT / "Art/reference/vanilla_player"
base = Image.new("RGBA", (40, 56), (196, 210, 226, 255))
for n, tint in [("Player_0_3", (120, 160, 90)), ("Player_0_7", (180, 140, 120)), ("Player_0_9", (90, 90, 150)),
                ("Player_0_10", (60, 60, 110)), ("Player_0_0", (230, 190, 160)), ("Player_0_1", (255, 255, 255)),
                ("Player_0_2", (60, 90, 160))]:
    p = ref / f"{n}.png"
    if not p.exists():
        continue
    im = Image.open(p).convert("RGBA").crop((0, 0, 40, 56))
    px = im.load()
    for y in range(56):
        for x in range(40):
            r, g, b, a = px[x, y]
            if a:
                l = (r + g + b) / 765
                px[x, y] = (int(tint[0] * l), int(tint[1] * l), int(tint[2] * l), a)
    base.alpha_composite(im)
mask = Image.open(ROOT / "Content/Items/Armor/RimeMask_Head.png").convert("RGBA").crop((0, 0, 40, 56))
both = base.copy()
both.alpha_composite(mask)
out = Image.new("RGBA", (80 * 6 + 8, 56 * 6), (0, 0, 0, 255))
out.paste(base.resize((240, 336), Image.NEAREST), (0, 0))
out.paste(both.resize((240, 336), Image.NEAREST), (248, 0))
out.save(ROOT / "Art/rime/preview_mask_player.png")
