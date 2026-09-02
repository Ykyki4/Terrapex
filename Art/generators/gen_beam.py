import math
from PIL import Image

# ---------------------------------------------------------------- beam texture
# 32 wide (stretched to ~26 px in game, so near 1:1), 96 tall and stretched down
# the whole length. The vertical ramp gives the beam a soft origin and a taper,
# which is what stops four crossing beams from piling into a white blob.
W, H = 32, 96
beam = Image.new("RGBA", (W, H), (0, 0, 0, 0))

CORE = (255, 255, 255)
HOT = (247, 176, 255)
MID = (196, 84, 255)
EDGE = (108, 30, 176)


def lerp(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(int(a[k] + (b[k] - a[k]) * t) for k in range(3))


for y in range(H):
    # 0 at the muzzle, 1 at full body, tapering again at the far tip
    if y < 16:
        head = (y / 16.0) ** 1.4
    elif y > H - 14:
        head = ((H - y) / 14.0) ** 0.9
    else:
        head = 1.0

    for x in range(W):
        d = abs(x + 0.5 - W / 2.0) / (W / 2.0)   # 0 centre .. 1 edge
        if d >= 1.0:
            continue
        if d < 0.14:
            c, a = CORE, 1.0
        elif d < 0.30:
            c, a = lerp(CORE, HOT, (d - 0.14) / 0.16), 1.0
        elif d < 0.55:
            c, a = lerp(HOT, MID, (d - 0.30) / 0.25), 1.0
        else:
            t = (d - 0.55) / 0.45
            c, a = lerp(MID, EDGE, t), (1.0 - t) ** 1.5
        beam.putpixel((x, y), c + (int(255 * a * head),))

beam.save("beam_RiftLaser.png")

# ---------------------------------------------------------------- flare
# soft round bloom, used for the beam muzzle and the rift-tear telegraph ring
S = 64
flare = Image.new("RGBA", (S, S), (0, 0, 0, 0))
for y in range(S):
    for x in range(S):
        r = math.hypot(x + 0.5 - S / 2, y + 0.5 - S / 2) / (S / 2)
        if r >= 1.0:
            continue
        a = (1.0 - r) ** 2.4
        c = lerp(CORE, MID, min(1.0, r * 1.6))
        flare.putpixel((x, y), c + (int(255 * a),))
flare.save("beam_RiftFlare.png")

# ---------------------------------------------------------------- ring
# thin expanding ring, drawn scaling outward as the "something is spawning" tell
ring = Image.new("RGBA", (S, S), (0, 0, 0, 0))
for y in range(S):
    for x in range(S):
        r = math.hypot(x + 0.5 - S / 2, y + 0.5 - S / 2) / (S / 2)
        d = abs(r - 0.82)
        if d > 0.18 or r >= 1.0:
            continue
        a = (1.0 - d / 0.18) ** 1.8
        c = lerp(CORE, MID, d / 0.18)
        ring.putpixel((x, y), c + (int(255 * a),))
ring.save("beam_RiftRing.png")

print("ok")
