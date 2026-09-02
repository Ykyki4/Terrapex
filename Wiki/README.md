# Terrapex wiki

A static site generated from the mod's own sources. Nothing on it is written
twice: item stats come from `SetDefaults`, recipes from `AddRecipes`, drops from
`ModifyNPCLoot`, names and tooltips from the localisation file. Change a damage
number in C# and the wiki follows on the next push.

English at the root, Russian under `/ru/`, with a language switcher in the header.

## Running it

```
uv run --with pillow --python 3.12 python Wiki/generate.py
cd Wiki
uv run --with mkdocs-material --python 3.12 mkdocs build -f mkdocs.en.yml --strict
uv run --with mkdocs-material --python 3.12 mkdocs build -f mkdocs.ru.yml --strict
```

The English build **must** run first — it cleans `site/` wholesale and would take
`site/ru/` with it.

To preview while editing:

```
cd Wiki
uv run --with mkdocs-material --python 3.12 mkdocs serve -f mkdocs.en.yml
```

## Before the first deploy

Edit **`Wiki/data/site.json`** and set `owner` to your GitHub username. That one
value produces `site_url`, the repository link and the language switcher's hrefs.
Then in the repository: *Settings → Pages → Source → **GitHub Actions***.

After that, `.github/workflows/wiki.yml` rebuilds and publishes on every push to
`main` that touches `Content/`, `Localization/` or `Wiki/`.

## What is generated and what is not

| Path | |
|---|---|
| `Wiki/docs/` | **generated, gitignored.** Deleted and rebuilt every run |
| `Wiki/mkdocs.{en,ru}.yml` | **generated.** The nav is 200-odd entries and has to track `Content/` |
| `Wiki/site/` | **generated, gitignored.** The built HTML |
| `Wiki/generate.py` | the builder: markdown, nav, configs |
| `Wiki/terrapex_parse.py` | the reader: C# and hjson in, plain dicts out |
| `Wiki/data/site.json` | owner and repo name. Edit this |
| `Wiki/data/tiers.json` | which tier each thing belongs to, and the armour sets |
| `Wiki/data/ru.json` | every Russian string on the site |
| `Wiki/assets/terrapex.css` | the skin |
| `Wiki/pages/{en,ru}/*.md` | hand-written pages, copied over the generated tree |
| `Wiki/legacy-wikigg/` | the abandoned wiki.gg MediaWiki attempt, kept for reference |

## Adding content to the mod

New items, NPCs and buffs appear on the wiki with no work beyond the mod itself —
`generate.py` walks `Content/` and picks them up. Two things it will ask for:

- an entry in **`Wiki/data/tiers.json`** under `content` (it prints
  `! no tier assigned:` for anything missing), and
- an entry in **`Wiki/data/ru.json`** (it prints `! N missing translations`).

Both are warnings, not errors: the page is still built, falling back to English
and to no tier badge.

## Hand-written pages

Anything in `Wiki/pages/en/` and `Wiki/pages/ru/` is copied into the docs tree
**after** generation, so a file at `Wiki/pages/en/bosses/KeeperOfTheRift.md`
replaces the generated page at that path. Use it for boss strategy — the sort of
thing that cannot be read out of `SetDefaults`. Link to generated pages by their
stable paths (`items/weapons/Skol.md`, `enemies/Mirrorling.md`).

## Two settings that are load-bearing

- **`use_directory_urls: false`.** MkDocs rewrites relative links written as
  Markdown, but leaves raw `<img src>` alone. Every sprite on this site is a raw
  `<img>` (it needs a class for `image-rendering: pixelated`), so with directory
  URLs the two disagree by exactly one level and every sprite 404s. Flat `.html`
  URLs put the rendered page in its source's own directory and one depth is
  correct for both.
- **Assets are copied into `docs/en/` and `docs/ru/` separately.** MkDocs serves
  only what is inside `docs_dir`, and `docs_dir` is per language. The duplication
  is 2.3 MB.

The language switcher's links are absolute (`/Terrapex/`, `/Terrapex/ru/`),
because that is what they must be once deployed. Under `mkdocs serve` they point
outside the dev server and will 404 — switch language by editing the URL locally,
or just serve the language you are working on.

## Known shape of the data

A few things the parser is deliberately shallow about, so you know what to check
by hand if a page looks wrong:

- **Drop rules.** `Common`, `OneFromOptions`, `BossBag` and one level of
  `LeadingConditionRule` are understood. A deeper rule tree will render as a bare
  item with an em dash for its chance.
- **Set bonuses** are read from the `Mods.Terrapex.SetBonus.*` key a head asks
  for, and the Fissurite placeholders `{0}`/`{1}` are filled with 8 and 15 to
  match the code.
- **Off-class armour.** Tiers 0-2 ship one set each. The class setup tables show
  it for every class but say plainly when it is not that class's set.
