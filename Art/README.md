# Art

Working material, not shipping assets — `build.txt` keeps this whole folder out of the
`.tmod`. Anything the game actually loads lives next to its `.cs` under `Content/`.

| Folder | What |
|---|---|
| `generators/` | Python + PIL scripts that render sprites parametrically. `uv run --with pillow --python 3.12 python generators/gen_shard24.py` |
| `sheets/` | master sheets as first produced (horizontal strips) plus their glowmasks |
| `tml/` | the same sheets converted to the vertical layout tModLoader expects |
| `preview/` | GIFs and upscaled PNG previews for eyeballing animation |
| `archive/` | superseded and rejected versions — the first sword, the three unused hilts, the pre-ramp palette mob, scratch canvases |

Naming: `sword02_*`, `mob01_*`, `boss01_*`, `proj_*` are the working names from when the
art was made; the shipped copies are renamed to match their class (`RiftShard.png`,
`KeeperOfTheRift.png`, …).
