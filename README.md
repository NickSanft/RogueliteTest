# RogueliteTest

A cosmic horror roguelite built in Godot 4.6 (C#). You play an investigator piecing together the truth behind a decaying coastal town, accumulating doom with every discovery. Each run ends in madness, exhaustion, or — rarely — understanding.

## Gameplay

Each turn you choose a location to investigate. Visiting a location costs turns and raises doom, then triggers a random event from that location's pool. Events present choices, some gated by stat checks (fixed threshold or dice roll), held items, or minimum doom level. Consequences modify your stats, grant items, chain into further events, or advance the current mystery arc.

**Stats**
| Stat | Range | Game over at |
|------|-------|-------------|
| Stamina | 0–10 | 0 |
| Reason | 0–10 | 0 |
| Doom | 0–100 | 100 |

**Locations**
| Location | Turn cost | Doom cost | Status |
|----------|-----------|-----------|--------|
| Town Square | 1 | +2 | Always available |
| Old Library | 1 | +2 | Always available |
| Coastal Cliff | 1 | +2 | Always available |
| Abandoned Shrine | 2 | +4 | Always available |
| Sunken Catacomb | 3 | +6 | Unlocked mid-run |

**Mysteries**
Two mystery arcs must be completed to win. The second arc (The Forbidden Knowledge) unlocks automatically when the first (The Sunken Truth) is solved. Mystery progress is shown in the HUD alongside current stats.

**Meta-progression**
Stats carry across runs in `roguelite_meta.cfg`. After your first mystery solved you begin with the Worn Journal (+1 Reason on stat checks). After three runs you begin with Rope & Torch (+1 Stamina on stat checks).

## Controls

| Key | Action |
|-----|--------|
| TAB | Open location selection |
| 1–9 | Choose event option |
| Space | Continue (result screen) |
| ESC | Close current window |

## Building

Requires **Godot 4.6** and the **.NET SDK** (for C#).

```
dotnet build
```

Open the project in the Godot editor and press F5 to run, or export from Project → Export.

**Tests** use the [GUT](https://github.com/bitwes/Gut) framework and run from the editor's GUT panel, or headless:

```
godot --headless -s addons/gut/gut_cmdln.gd -gdir=res://tests/ -gexit
```

## Adding Content

All game content is data-driven. No code changes are required for new events, locations, items, or mysteries.

- **Event:** `data/events/{id}.tres` — reference `EventResource.cs`
- **Location:** `data/locations/{id}.tres` — reference `LocationResource.cs`, picked up automatically
- **Item:** `data/items/{id}.tres` — reference `ItemResource.cs`
- **Mystery:** `data/mysteries/{id}.tres` — set `UnlockedByDefault = false` to make it unlock sequentially

See `CLAUDE.md` for enum values needed in `.tres` files and full architecture notes.

**Content visualiser:** generates a self-contained HTML overview of all locations, events, mystery paths, and items:

```
python tools/visualize_content.py
# open tools/content_overview.html
```
