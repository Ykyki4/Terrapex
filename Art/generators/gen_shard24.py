import math
from PIL import Image

N = 24          # frame size
FRAMES = 8
SS = 6          # supersample

# stone body ramp (light -> dark), then the rift core
BODY = ["#b8b1c6", "#918a9f", "#6d667c", "#514a5e", "#393343", "#28222f"]
CORE = ["#ffffff", "#ffe4ff", "#f5a2ff", "#d155ff", "#9724e0"]
OUT  = "#1b1228"

def hx(c):
    return (int(c[1:3], 16), int(c[3:5], 16), int(c[5:7], 16))

BODY = [hx(c) for c in BODY]
CORE = [hx(c) for c in CORE]
OUT = hx(OUT)


def ramp(tab, t):
    t = max(0.0, min(0.999, t)) * (len(tab) - 1)
    i = int(t)
    f = t - i
    a, b = tab[i], tab[min(i + 1, len(tab) - 1)]
    return tuple(int(a[k] + (b[k] - a[k]) * f) for k in range(3))


def halfwidth(u):
    """u in [-1,1] along the shard. Pointed tail, fat shoulder, sharp tip."""
    if u < -1 or u > 1:
        return 0.0
    return 0.335 * ((1 - u) ** 0.80) * ((1 + u) ** 1.70) / 1.24


def shade(u, w, hw):
    """returns rgb or None"""
    if hw <= 0:
        return None
    a = abs(w) / hw
    if a > 1.0:
        return None
    # the rift runs down the middle as a hard bright line
    core_hw = 0.40 * hw * (1.0 - 0.25 * abs(u))
    if abs(w) < core_hw and -0.86 < u < 0.90:
        t = abs(w) / max(core_hw, 1e-5)
        return ramp(CORE, t * 1.05 + 0.35 * abs(u) ** 1.5)
    # body reads as two flat facets split along the rift, not a soft gradient
    side = w / hw
    if side < -0.30:
        return ramp(BODY, 0.02 + 0.30 * max(0.0, u))     # lit facet
    if side < 0.45:
        return ramp(BODY, 0.52 + 0.22 * max(0.0, u))     # mid facet
    return ramp(BODY, 0.82 + 0.16 * max(0.0, u))         # shadow facet


sheet = Image.new("RGBA", (N, N * FRAMES), (0, 0, 0, 0))

for f in range(FRAMES):
    ang = math.pi * 2 * f / FRAMES
    ca, sa = math.cos(-ang), math.sin(-ang)
    fill = {}
    for py in range(N):
        for px in range(N):
            acc = []
            for sy in range(SS):
                for sx in range(SS):
                    x = px + (sx + 0.5) / SS - N / 2.0
                    y = py + (sy + 0.5) / SS - N / 2.0
                    # into shard space (radius ~ N/2 - 1.5)
                    r = (N / 2.0 - 1.6)
                    u = (x * ca - y * sa) / r
                    w = (x * sa + y * ca) / r
                    c = shade(u, w, halfwidth(u))
                    if c:
                        acc.append(c)
            if len(acc) >= SS * SS * 0.42:
                m = len(acc)
                fill[(px, py)] = tuple(sum(c[k] for c in acc) // m for k in range(3))

    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    for (px, py), c in fill.items():
        img.putpixel((px, py), c + (255,))

    # 1px selout: darken toward the outline colour, tinted by the neighbour
    for (px, py) in list(fill.keys()):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = px + dx, py + dy
            if 0 <= nx < N and 0 <= ny < N and (nx, ny) not in fill:
                src = fill[(px, py)]
                col = tuple(int(src[k] * 0.38 + OUT[k] * 0.62) for k in range(3))
                img.putpixel((nx, ny), col + (255,))

    sheet.paste(img, (0, f * N))

sheet.save("shard24.png")

# glowmask: only the rift core survives
glow = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
for y in range(sheet.height):
    for x in range(sheet.width):
        r, g, b, a = sheet.getpixel((x, y))
        if a and b > 90 and b > g + 25 and (r + g + b) > 260:
            glow.putpixel((x, y), (r, g, b, a))
glow.save("shard24_Glow.png")

prev = sheet.resize((sheet.width * 6, sheet.height * 6), Image.NEAREST)
prev.save("shard24_preview.png")
print("ok", sheet.size)
