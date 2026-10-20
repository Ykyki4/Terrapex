"""The Sultan as the game draws it: each frame with its sun in its legs (SunBall frame 0 at the
code's HeldBall offset), and flipped to face left. Inspection only.

    uv run --with pillow --python 3.12 python Art/scarab/compose_boss.py
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
HELD = (8, 42)  # ScarabSultan.HeldBall, px from the frame centre
S = 3

body = Image.open(ROOT / "Content/NPCs/Bosses/ScarabSultan.png").convert("RGBA")
ball = Image.open(ROOT / "Content/Projectiles/Scarab/SunBall.png").convert("RGBA")
fw, fh = body.width, body.width * 112 // 128
n = body.height // fh
bw = ball.width
out = Image.new("RGBA", ((fw + 40) * n * S, (fh + 60) * 2 * S), (222, 190, 130, 255))
for i in range(n):
    for row, kind in enumerate((0, 1)):
        canvas = Image.new("RGBA", (fw + 40, fh + 60), (0, 0, 0, 0))
        b = ball.crop((0, kind * bw, bw, kind * bw + bw))
        cx, cy = 20 + fw // 2 + HELD[0], 10 + fh // 2 + HELD[1]
        canvas.alpha_composite(b, (cx - bw // 2, cy - bw // 2))
        canvas.alpha_composite(body.crop((0, i * fh, fw, i * fh + fh)), (20, 10))
        if row == 1:
            canvas = canvas.transpose(Image.FLIP_LEFT_RIGHT)
        big = canvas.resize((canvas.width * S, canvas.height * S), Image.NEAREST)
        out.alpha_composite(big, (i * (fw + 40) * S, row * (fh + 60) * S))
out.save(ROOT / "Art/scarab/preview_boss_composite.png")
print("ok")
