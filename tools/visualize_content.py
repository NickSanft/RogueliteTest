#!/usr/bin/env python3
"""
Generates tools/content_overview.html — a self-contained interactive browser
overview of all locations, events, mysteries, and items in RogueliteTest.

Run from the project root (or from tools/):
    python tools/visualize_content.py

Then open tools/content_overview.html in any browser. No server required.
"""

import re, json, sys
from pathlib import Path

# ── Resource type identification ──────────────────────────────────────────────

SCRIPT_UIDS = {
    "uid://bwoliaoytwc7e": "EventResource",
    "uid://2wwbq38npbrd":  "EventOption",
    "uid://wjlfxffqq08k":  "StatCheck",
    "uid://ln4cmwg8ep8":   "EventConsequence",
    "uid://c6qeqad1ggm1b": "LocationResource",
    "uid://gptp2ivbnb6a":  "MysteryResource",
    "uid://dnrkflagtfvpo": "ItemResource",
}
SCRIPT_PATH_KEYS = {
    "EventResource.cs":    "EventResource",
    "EventOption.cs":      "EventOption",
    "StatCheck.cs":        "StatCheck",
    "EventConsequence.cs": "EventConsequence",
    "LocationResource.cs": "LocationResource",
    "MysteryResource.cs":  "MysteryResource",
    "ItemResource.cs":     "ItemResource",
}

STAT_NAMES  = {0: "Stamina", 1: "Reason", 2: "Doom"}
CONS_TYPES  = {
    0: "StatChange", 1: "ItemGain", 2: "TriggerEvent",
    3: "AdvanceMystery", 4: "UnlockLocation",
}

# ── Low-level .tres parser ────────────────────────────────────────────────────

def parse_tres(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")

    # Map ext_resource id -> resource type name.
    # Use (?<!\w)id= to avoid matching "uid=" which also ends in "id".
    ext_resources: dict[str, str] = {}
    for blk_m in re.finditer(r'\[ext_resource[^\]]+\]', text):
        blk   = blk_m.group(0)
        id_m  = re.search(r'(?<!\w)id="([^"]+)"', blk)
        uid_m = re.search(r'uid="([^"]+)"',        blk)
        path_m = re.search(r'path="([^"]+)"',      blk)
        if not id_m:
            continue
        rid   = id_m.group(1)
        uid   = uid_m.group(1)  if uid_m  else ""
        epath = path_m.group(1) if path_m else ""
        rtype = SCRIPT_UIDS.get(uid, "")
        if not rtype:
            for pk, pv in SCRIPT_PATH_KEYS.items():
                if pk in epath:
                    rtype = pv
                    break
        ext_resources[rid] = rtype

    # Split into sections starting at [sub_resource ...] or [resource]
    raw_sections = re.split(r'(?=\[(?:sub_resource|resource)\b)', text)

    sub_resources: dict[str, dict] = {}
    main_block:    dict            = {}

    for sec in raw_sections:
        hdr = re.match(r'\[sub_resource\b[^\]]*?id="([^"]+)"[^\]]*?\]', sec)
        if hdr:
            props = _parse_props(sec[hdr.end():])
            # Identify the sub_resource's type from its script reference
            script_ref = props.get("script", {})
            if isinstance(script_ref, dict):
                eid = script_ref.get("__extresource", "")
                props["__type"] = ext_resources.get(eid, "")
            sub_resources[hdr.group(1)] = props
            continue
        if re.match(r'\[resource\]', sec):
            main_block = _parse_props(sec[len("[resource]"):])

    return {"ext_resources": ext_resources, "sub_resources": sub_resources, "main": main_block}


def _parse_props(text: str) -> dict:
    props = {}
    for line in text.splitlines():
        line = line.strip()
        if not line:
            continue          # skip blank lines between properties
        if line.startswith("["):
            break             # next section header — stop
        sep = line.find(" = ")
        if sep == -1:
            continue
        props[line[:sep].strip()] = _parse_value(line[sep + 3:].strip())
    return props


def _parse_value(s: str):
    if re.fullmatch(r"-?\d+", s):           return int(s)
    if re.fullmatch(r"-?\d+\.\d+", s):      return float(s)
    if s == "true":                          return True
    if s == "false":                         return False
    if s.startswith('"') and s.endswith('"'): return s[1:-1]
    m = re.fullmatch(r'SubResource\("([^"]+)"\)', s)
    if m: return {"__subresource": m.group(1)}
    m = re.fullmatch(r'ExtResource\("([^"]+)"\)', s)
    if m: return {"__extresource": m.group(1)}
    m = re.fullmatch(r"Array\[.*?\]\((.*)\)", s, re.DOTALL)
    if m: return _parse_array(m.group(1))
    # Plain array: ["a", "b"] written by older Godot serializer
    if s.startswith("[") and s.endswith("]"):
        return _parse_array(s)
    return s


def _parse_array(s: str) -> list:
    s = s.strip()
    if not s or s == "[]":
        return []
    inner = s[1:-1].strip()
    if not inner:
        return []
    items, depth, cur = [], 0, ""
    for ch in inner:
        if   ch == "(":                  depth += 1; cur += ch
        elif ch == ")":                  depth -= 1; cur += ch
        elif ch == "," and depth == 0:   items.append(cur.strip()); cur = ""
        else:                            cur += ch
    if cur.strip():
        items.append(cur.strip())
    return [_parse_value(i) for i in items]


# ── Resolve sub_resource references ──────────────────────────────────────────

def resolve(val, sr: dict):
    if isinstance(val, dict):
        if "__subresource" in val:
            sid = val["__subresource"]
            if sid in sr:
                return resolve(sr[sid], sr)
        return {k: resolve(v, sr) for k, v in val.items()}
    if isinstance(val, list):
        return [resolve(v, sr) for v in val]
    return val


# ── Build typed data objects ──────────────────────────────────────────────────

def build_consequence(raw: dict) -> dict:
    ctype = CONS_TYPES.get(raw.get("Type", 0), "StatChange")
    c: dict = {"type": ctype}
    if   ctype == "StatChange":     c["stat"]          = raw.get("StatName", ""); c["value"] = raw.get("Value", 0)
    elif ctype == "ItemGain":        c["item_id"]       = raw.get("ItemId", "")
    elif ctype == "TriggerEvent":    c["next_event_id"] = raw.get("NextEventId", "")
    elif ctype == "AdvanceMystery":  c["amount"]        = raw.get("MysteryProgress", 1)
    elif ctype == "UnlockLocation":  c["location_id"]   = raw.get("LocationId", "")
    return c


def build_stat_check(raw) -> dict | None:
    if not isinstance(raw, dict) or "__type" not in raw:
        return None
    return {
        "stat":      STAT_NAMES.get(raw.get("Stat", 0), "?"),
        "threshold": raw.get("Threshold", 0),
        "check_type": "DiceRoll" if raw.get("CheckType", 0) == 1 else "Fixed",
    }


def build_option(raw: dict) -> dict:
    def cons(key):
        v = raw.get(key, [])
        return [build_consequence(c) for c in v if isinstance(c, dict)] if isinstance(v, list) else []
    return {
        "text":         raw.get("OptionText", ""),
        "stat_check":   build_stat_check(raw.get("StatCheck")),
        "always":       cons("Consequences"),
        "on_success":   cons("SuccessConsequences"),
        "on_failure":   cons("FailureConsequences"),
        "success_text": raw.get("SuccessText", ""),
        "failure_text": raw.get("FailureText", ""),
    }


def build_event(tres: dict) -> dict | None:
    main, sr = tres["main"], tres["sub_resources"]
    eid = main.get("EventId")
    if not eid:
        return None
    raw_opts = main.get("Options", [])
    options = []
    for ref in (raw_opts if isinstance(raw_opts, list) else []):
        resolved = resolve(ref, sr)
        if isinstance(resolved, dict):
            options.append(build_option(resolved))
    return {"id": eid, "text": main.get("EventText", ""), "options": options}


def build_location(tres: dict) -> dict | None:
    main = tres["main"]
    lid = main.get("LocationId")
    if not lid:
        return None
    pool = main.get("EventPool", [])
    return {
        "id":               lid,
        "name":             main.get("LocationName", lid),
        "description":      main.get("Description", ""),
        "turn_cost":        main.get("TurnCost", 1),
        "unlocked_default": main.get("UnlockedByDefault", True),
        "event_pool":       [e for e in pool if isinstance(e, str)] if isinstance(pool, list) else [],
    }


def build_mystery(tres: dict) -> dict | None:
    main = tres["main"]
    mid = main.get("MysteryId")
    if not mid:
        return None
    return {
        "id":                mid,
        "name":              main.get("MysteryName", mid),
        "description":       main.get("Description", ""),
        "required_progress": main.get("RequiredProgress", 1),
        "completion_text":   main.get("CompletionText", ""),
        "sort_order":        main.get("SortOrder", 0),
    }


def build_item(tres: dict) -> dict | None:
    main = tres["main"]
    iid = main.get("ItemId")
    if not iid:
        return None
    return {
        "id":           iid,
        "name":         main.get("ItemName", iid),
        "description":  main.get("Description", ""),
        "stamina_bonus": main.get("StaminaBonus", 0),
        "reason_bonus":  main.get("ReasonBonus", 0),
    }


# ── Load a directory of .tres files ──────────────────────────────────────────

def load_dir(data_dir: Path, builder) -> dict:
    results = {}
    if not data_dir.exists():
        return results
    for f in sorted(data_dir.glob("*.tres")):
        try:
            obj = builder(parse_tres(f))
            if obj:
                results[obj["id"]] = obj
        except Exception as e:
            print(f"  Warning: {f.name}: {e}", file=sys.stderr)
    return results


# ── Entry point ───────────────────────────────────────────────────────────────

def main():
    root = Path(__file__).resolve().parent.parent
    print("Parsing data files…")
    events    = load_dir(root / "data" / "events",    build_event)
    locations = load_dir(root / "data" / "locations", build_location)
    mysteries = load_dir(root / "data" / "mysteries", build_mystery)
    items     = load_dir(root / "data" / "items",     build_item)
    print(f"  {len(events)} events, {len(locations)} locations, "
          f"{len(mysteries)} mysteries, {len(items)} items")

    data      = {"events": events, "locations": locations,
                 "mysteries": mysteries, "items": items}
    data_json = (json.dumps(data, ensure_ascii=False)
                     .replace("</script>", "<\\/script>"))

    out_dir  = root / "tools"
    out_dir.mkdir(exist_ok=True)
    out_path = out_dir / "content_overview.html"
    out_path.write_text(HTML_TEMPLATE.replace("__DATA_JSON__", data_json), encoding="utf-8")
    print(f"Generated: {out_path}")
    print("Open that file in any browser — no server required.")


# ── HTML template ─────────────────────────────────────────────────────────────
# __DATA_JSON__ is replaced with the serialised Python data at generation time.

HTML_TEMPLATE = """\
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>RogueliteTest — Content Overview</title>
<style>
:root {
  --bg:       #09090f;
  --bg2:      #11111c;
  --bg3:      #181828;
  --border:   #252535;
  --text:     #c0c0d4;
  --dim:      #5a5a7a;
  --accent:   #7070ee;
  --doom:     #e05020;
  --stamina:  #30b050;
  --reason:   #4090e0;
  --mystery:  #a040e0;
  --item:     #30c0a0;
  --unlock:   #d0a020;
  --trigger:  #50b0e0;
  --success:  #30b050;
  --failure:  #e03030;
}
* { box-sizing: border-box; margin: 0; padding: 0; }
body {
  background: var(--bg); color: var(--text);
  font-family: Georgia, 'Times New Roman', serif;
  font-size: 14px; line-height: 1.65;
}
a { color: var(--accent); text-decoration: none; }
a:hover { text-decoration: underline; }

/* ── Header ── */
header {
  background: var(--bg2); border-bottom: 1px solid var(--border);
  padding: 10px 20px; display: flex; align-items: center; gap: 20px;
  position: sticky; top: 0; z-index: 100;
}
header h1 { font-size: 16px; color: var(--accent); letter-spacing: .12em; text-transform: uppercase; }
.tabs { display: flex; gap: 4px; }
.tab-btn {
  background: none; border: 1px solid var(--border); color: var(--dim);
  padding: 5px 14px; cursor: pointer; font-family: inherit; font-size: 12px;
  border-radius: 3px; transition: all .15s; text-transform: uppercase; letter-spacing: .08em;
}
.tab-btn:hover { color: var(--text); border-color: var(--accent); }
.tab-btn.active { background: var(--accent); color: #fff; border-color: var(--accent); }
.summary { margin-left: auto; font-size: 11px; color: var(--dim); font-family: monospace; }

/* ── Layout ── */
main { padding: 18px 22px; max-width: 1140px; margin: 0 auto; }
.tab-pane { display: none; }
.tab-pane.active { display: block; }

/* ── Filter bar ── */
.filter-bar { margin-bottom: 14px; }
.filter-bar input {
  background: var(--bg2); border: 1px solid var(--border); color: var(--text);
  padding: 7px 12px; width: 280px; font-family: inherit; font-size: 13px;
  border-radius: 3px; outline: none;
}
.filter-bar input:focus { border-color: var(--accent); }

/* ── Location grid ── */
.location-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(330px, 1fr)); gap: 14px; }
.location-card { background: var(--bg2); border: 1px solid var(--border); border-radius: 4px; overflow: hidden; }
.loc-header {
  padding: 11px 14px; background: var(--bg3); display: flex; align-items: center;
  gap: 8px; cursor: pointer; user-select: none;
}
.loc-header:hover { background: #1c1c2e; }
.loc-name { font-size: 14px; font-weight: bold; flex: 1; }
.loc-badges { display: flex; gap: 5px; align-items: center; flex-shrink: 0; }
.loc-body { padding: 12px 14px; }
.loc-body.hidden { display: none; }
.loc-desc { color: var(--dim); font-size: 12px; font-style: italic; margin-bottom: 10px; }
.pool-label { font-size: 10px; text-transform: uppercase; letter-spacing: .1em; color: var(--dim); margin-bottom: 5px; }
.event-link {
  display: flex; align-items: center; gap: 6px;
  padding: 5px 9px; background: var(--bg); border: 1px solid var(--border);
  border-radius: 3px; margin-bottom: 3px; cursor: pointer;
  font-size: 12px; color: var(--dim); font-family: monospace; transition: all .1s;
}
.event-link:hover { color: var(--accent); border-color: var(--accent); }
.event-link-id { flex: 1; }

/* ── Badges ── */
.badge {
  font-size: 10px; padding: 2px 5px; border-radius: 2px;
  font-family: monospace; letter-spacing: .04em; white-space: nowrap;
}
.b-locked   { background: #2a1010; color: #b05050; }
.b-turn     { background: #0e1a28; color: #5080b0; }
.b-doom     { background: #28140a; color: var(--doom); }
.b-mystery  { background: #1c082c; color: var(--mystery); }
.b-item     { background: #082820; color: var(--item); }
.b-chain    { background: #081828; color: var(--trigger); }
.b-unlock   { background: #28200a; color: var(--unlock); }

/* ── Event cards ── */
.event-card { background: var(--bg2); border: 1px solid var(--border); border-radius: 4px; margin-bottom: 8px; overflow: hidden; }
.ev-header {
  padding: 9px 13px; display: flex; align-items: center; gap: 9px;
  cursor: pointer; user-select: none; background: var(--bg3);
}
.ev-header:hover { background: #1c1c2e; }
.ev-id { font-family: monospace; font-size: 13px; color: var(--accent); flex: 1; }
.ev-badges { display: flex; gap: 4px; }
.expand-icon { color: var(--dim); font-size: 9px; flex-shrink: 0; }
.ev-body { padding: 12px 14px; display: none; }
.ev-body.open { display: block; }
.ev-text {
  color: var(--text); margin-bottom: 12px; padding: 9px 11px;
  background: var(--bg); border-left: 3px solid var(--border); font-style: italic; font-size: 13px;
}

/* ── Options ── */
.option { border: 1px solid var(--border); border-radius: 3px; margin-bottom: 8px; overflow: hidden; }
.opt-header {
  padding: 7px 11px; background: var(--bg3); display: flex; align-items: center; gap: 9px;
}
.opt-text { flex: 1; font-size: 13px; }
.stat-check {
  font-size: 10px; padding: 2px 6px; border-radius: 2px;
  font-family: monospace; white-space: nowrap; flex-shrink: 0;
}
.sc-stamina { background: #081c10; color: var(--stamina); }
.sc-reason  { background: #081018; color: var(--reason); }
.sc-doom    { background: #200800; color: var(--doom); }
.sc-none    { background: #141414; color: var(--dim); }
.opt-paths { padding: 9px 11px; display: flex; flex-direction: column; gap: 8px; }
.path-block {}
.path-label {
  font-size: 10px; font-family: monospace; text-transform: uppercase;
  letter-spacing: .1em; margin-bottom: 3px;
}
.pl-success { color: var(--success); }
.pl-failure { color: var(--failure); }
.pl-always  { color: var(--dim); }
.path-narr {
  font-size: 12px; color: var(--dim); font-style: italic;
  margin-bottom: 5px; padding-left: 8px; border-left: 2px solid var(--border);
}
.cons-row { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 3px; }
.con {
  font-size: 10px; padding: 2px 5px; border-radius: 2px;
  font-family: monospace; white-space: nowrap;
}
.c-sp  { background: #082010; color: var(--stamina); }
.c-sn  { background: #200808; color: var(--failure); }
.c-dp  { background: #200d00; color: var(--doom); }
.c-dn  { background: #002010; color: var(--stamina); }
.c-my  { background: #160828; color: var(--mystery); }
.c-it  { background: #001c16; color: var(--item); }
.c-tr  { background: #000e1c; color: var(--trigger); }
.c-ul  { background: #1c1600; color: var(--unlock); }

/* ── Mystery paths ── */
.mystery-section { background: var(--bg2); border: 1px solid var(--border); border-radius: 4px; margin-bottom: 16px; overflow: hidden; }
.mystery-header { padding: 13px 15px; background: #0c0c20; border-bottom: 1px solid var(--border); }
.mystery-name { font-size: 15px; color: var(--mystery); margin-bottom: 3px; }
.mystery-meta { font-size: 11px; color: var(--dim); font-family: monospace; }
.pbar-wrap { height: 5px; background: var(--bg); border-radius: 3px; margin-top: 8px; overflow: hidden; }
.pbar { height: 100%; background: var(--mystery); border-radius: 3px; transition: width .3s; }
.mystery-body { padding: 13px 15px; }
.sec-title {
  font-size: 10px; text-transform: uppercase; letter-spacing: .12em;
  color: var(--dim); margin-bottom: 8px; padding-bottom: 5px;
  border-bottom: 1px solid var(--border);
}
.m-event {
  background: var(--bg); border: 1px solid var(--border); border-radius: 3px;
  padding: 9px 11px; display: flex; gap: 10px; align-items: flex-start;
  margin-bottom: 6px;
}
.m-event-info { flex: 1; }
.m-event-id { font-family: monospace; font-size: 12px; color: var(--accent); margin-bottom: 2px; }
.m-event-desc { font-size: 12px; color: var(--dim); }
.m-prog-badge {
  font-family: monospace; font-size: 13px; color: var(--mystery);
  background: #160828; padding: 3px 9px; border-radius: 3px;
  white-space: nowrap; align-self: center; flex-shrink: 0;
}

/* ── Items section ── */
.item-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 10px; }
.item-card { background: var(--bg2); border: 1px solid var(--border); border-radius: 4px; padding: 11px 14px; }
.item-name { font-size: 14px; color: var(--item); margin-bottom: 4px; }
.item-id { font-family: monospace; font-size: 11px; color: var(--dim); margin-bottom: 6px; }
.item-desc { font-size: 12px; color: var(--dim); font-style: italic; margin-bottom: 8px; }
.item-bonuses { display: flex; gap: 6px; flex-wrap: wrap; }

/* ── Misc ── */
.no-results { color: var(--dim); font-style: italic; padding: 16px 0; font-size: 13px; }
.completion-text { margin-top: 10px; }
</style>
</head>
<body>

<header>
  <h1>RogueliteTest</h1>
  <div class="tabs">
    <button class="tab-btn active" onclick="showTab('locations')">Locations</button>
    <button class="tab-btn"        onclick="showTab('events')">Events</button>
    <button class="tab-btn"        onclick="showTab('paths')">Mystery Paths</button>
    <button class="tab-btn"        onclick="showTab('items')">Items</button>
  </div>
  <div class="summary" id="summary"></div>
</header>

<main>
  <div id="tab-locations" class="tab-pane active"></div>
  <div id="tab-events"    class="tab-pane"></div>
  <div id="tab-paths"     class="tab-pane"></div>
  <div id="tab-items"     class="tab-pane"></div>
</main>

<script>
const DATA = __DATA_JSON__;

// ── Tab switching ─────────────────────────────────────────────────────────────
function showTab(name) {
  document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));
  document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
  document.getElementById('tab-' + name).classList.add('active');
  const labels = {locations:'loc', events:'eve', paths:'mys', items:'ite'};
  document.querySelectorAll('.tab-btn').forEach(b => {
    if (b.textContent.toLowerCase().startsWith(labels[name] || name.substring(0,3)))
      b.classList.add('active');
  });
}

// ── Summary ───────────────────────────────────────────────────────────────────
document.getElementById('summary').textContent =
  Object.keys(DATA.locations).length + ' locations · ' +
  Object.keys(DATA.events).length    + ' events · ' +
  Object.keys(DATA.mysteries).length + ' mysteries · ' +
  Object.keys(DATA.items).length     + ' items';

// ── Consequence rendering ─────────────────────────────────────────────────────
function renderCon(c) {
  if (c.type === 'StatChange') {
    const stat = c.stat.toLowerCase(), sign = c.value > 0 ? '+' : '';
    if (stat === 'doom')
      return '<span class="con ' + (c.value > 0 ? 'c-dp' : 'c-dn') + '">DOOM ' + sign + c.value + '</span>';
    return '<span class="con ' + (c.value > 0 ? 'c-sp' : 'c-sn') + '">' + stat.toUpperCase() + ' ' + sign + c.value + '</span>';
  }
  if (c.type === 'AdvanceMystery')
    return '<span class="con c-my">MYSTERY +' + c.amount + '</span>';
  if (c.type === 'ItemGain') {
    const item = DATA.items[c.item_id];
    return '<span class="con c-it">ITEM: ' + (item ? item.name : c.item_id) + '</span>';
  }
  if (c.type === 'TriggerEvent')
    return '<span class="con c-tr">\u2192 ' + c.next_event_id + '</span>';
  if (c.type === 'UnlockLocation')
    return '<span class="con c-ul">UNLOCK: ' + c.location_id + '</span>';
  return '';
}

function renderConList(cons) {
  if (!cons || !cons.length) return '';
  return '<div class="cons-row">' + cons.map(renderCon).join('') + '</div>';
}

function statCheckHtml(sc) {
  if (!sc) return '<span class="stat-check sc-none">No check</span>';
  return '<span class="stat-check sc-' + sc.stat.toLowerCase() + '">' +
         sc.stat.toUpperCase() + ' \u2265 ' + sc.threshold +
         (sc.check_type === 'DiceRoll' ? ' (dice)' : '') + '</span>';
}

// ── Event badges ──────────────────────────────────────────────────────────────
function allCons(evt) {
  return evt.options.flatMap(o => [...(o.always||[]), ...(o.on_success||[]), ...(o.on_failure||[])]);
}
function eventBadges(evt) {
  const cons = allCons(evt);
  return [
    cons.some(c => c.type === 'AdvanceMystery') ? '<span class="badge b-mystery">MYSTERY</span>' : '',
    cons.some(c => c.type === 'ItemGain')        ? '<span class="badge b-item">ITEM</span>'       : '',
    cons.some(c => c.type === 'TriggerEvent')    ? '<span class="badge b-chain">CHAIN</span>'     : '',
    cons.some(c => c.type === 'UnlockLocation')  ? '<span class="badge b-unlock">UNLOCK</span>'   : '',
  ].join('');
}

// ── Single event card ─────────────────────────────────────────────────────────
function renderEventCard(evt) {
  const optHtml = evt.options.map(opt => {
    const paths = [];
    if (opt.always && opt.always.length)
      paths.push('<div class="path-block"><div class="path-label pl-always">Always</div>' +
                 renderConList(opt.always) + '</div>');
    if (opt.stat_check) {
      paths.push(
        '<div class="path-block"><div class="path-label pl-success">\u2713 Success</div>' +
        (opt.success_text ? '<p class="path-narr">' + opt.success_text + '</p>' : '') +
        renderConList(opt.on_success) + '</div>',
        '<div class="path-block"><div class="path-label pl-failure">\u2717 Failure</div>' +
        (opt.failure_text ? '<p class="path-narr">' + opt.failure_text + '</p>' : '') +
        renderConList(opt.on_failure) + '</div>'
      );
    } else if (opt.success_text) {
      paths.push('<div class="path-block"><div class="path-label pl-always">Outcome</div>' +
                 '<p class="path-narr">' + opt.success_text + '</p></div>');
    }
    return '<div class="option">' +
           '<div class="opt-header"><span class="opt-text">' + opt.text + '</span>' +
           statCheckHtml(opt.stat_check) + '</div>' +
           '<div class="opt-paths">' + paths.join('') + '</div></div>';
  }).join('');

  return '<div class="event-card" id="ev-' + evt.id + '">' +
    '<div class="ev-header" onclick="toggleCard(this)">' +
    '<span class="ev-id">' + evt.id + '</span>' +
    '<span class="ev-badges">' + eventBadges(evt) + '</span>' +
    '<span class="expand-icon">\u25b6</span></div>' +
    '<div class="ev-body">' +
    '<div class="ev-text">' + evt.text + '</div>' +
    optHtml + '</div></div>';
}

// ── Locations tab ─────────────────────────────────────────────────────────────
function renderLocations() {
  const locs = Object.values(DATA.locations).sort((a, b) => a.name.localeCompare(b.name));
  const cards = locs.map(loc => {
    const poolLinks = loc.event_pool.map(eid => {
      const evt = DATA.events[eid];
      return '<div class="event-link" onclick="jumpToEvent(\\'' + eid + '\\')">'+
             '<span class="event-link-id">' + eid + '</span>'+
             (evt ? eventBadges(evt) : '') + '</div>';
    }).join('');

    return '<div class="location-card">' +
      '<div class="loc-header" onclick="toggleLocCard(this)">' +
      '<span class="loc-name">' + loc.name + '</span>' +
      '<span class="loc-badges">' +
      (!loc.unlocked_default ? '<span class="badge b-locked">LOCKED</span>' : '') +
      '<span class="badge b-turn">' + loc.turn_cost + ' turn' + (loc.turn_cost !== 1 ? 's' : '') + '</span>' +
      '<span class="badge b-doom">+' + (loc.turn_cost * 2) + ' doom</span>' +
      '</span></div>' +
      '<div class="loc-body">' +
      (loc.description ? '<p class="loc-desc">' + loc.description + '</p>' : '') +
      (loc.event_pool.length
        ? '<div class="pool-label">Events (' + loc.event_pool.length + ')</div>' + poolLinks
        : '<p class="loc-desc">No events in pool.</p>') +
      '</div></div>';
  }).join('');

  document.getElementById('tab-locations').innerHTML =
    '<div class="location-grid">' + cards + '</div>';
}

// ── Events tab ────────────────────────────────────────────────────────────────
function renderEventsTab(filter) {
  filter = (filter || '').toLowerCase();
  const all = Object.values(DATA.events).sort((a, b) => a.id.localeCompare(b.id));
  const shown = filter
    ? all.filter(e =>
        e.id.toLowerCase().includes(filter) ||
        e.text.toLowerCase().includes(filter) ||
        e.options.some(o => o.text.toLowerCase().includes(filter)))
    : all;

  document.getElementById('tab-events').innerHTML =
    '<div class="filter-bar"><input type="text" placeholder="Filter by ID or text\u2026"' +
    ' oninput="renderEventsTab(this.value)" value="' + (filter || '') + '"' +
    ' id="ev-filter-input"></div>' +
    (shown.length ? shown.map(renderEventCard).join('') : '<p class="no-results">No matching events.</p>');
}

// ── Mystery paths tab ─────────────────────────────────────────────────────────
function renderPaths() {
  // Collect all events with AdvanceMystery consequences
  const contributors = [];  // {eventId, amount, optText, onSuccess, hasCheck}
  Object.values(DATA.events).forEach(evt => {
    evt.options.forEach(opt => {
      const alwaysCons   = (opt.always     || []).filter(c => c.type === 'AdvanceMystery');
      const successCons  = (opt.on_success || []).filter(c => c.type === 'AdvanceMystery');
      alwaysCons.forEach(c  => contributors.push({eventId: evt.id, amount: c.amount, optText: opt.text, onSuccess: false, hasCheck: !!opt.stat_check}));
      successCons.forEach(c => contributors.push({eventId: evt.id, amount: c.amount, optText: opt.text, onSuccess: true,  hasCheck: !!opt.stat_check}));
    });
  });

  // Merge by event (keep highest total per event)
  const byEvent = {};
  contributors.forEach(c => {
    if (!byEvent[c.eventId]) byEvent[c.eventId] = {total: 0, entries: []};
    byEvent[c.eventId].total += c.amount;
    byEvent[c.eventId].entries.push(c);
  });

  const totalProgress = contributors.reduce((s, c) => s + c.amount, 0);

  const mysteries = Object.values(DATA.mysteries).sort((a, b) => a.sort_order - b.sort_order);
  if (!mysteries.length) {
    document.getElementById('tab-paths').innerHTML = '<p class="no-results">No mystery data found.</p>';
    return;
  }

  // Note: AdvanceMystery always targets the currently-active mystery at runtime.
  // Show contributing events alongside each mystery; the viewer infers sequence from sort_order.
  const mysteryHtml = mysteries.map(m => {
    const pct = Math.min(100, Math.round(totalProgress / m.required_progress * 100));
    const eventRows = Object.entries(byEvent)
      .sort((a, b) => b[1].total - a[1].total)
      .map(([eid, info]) => {
        const entryDesc = info.entries.map(e =>
          '<span style="color:var(--dim)">' + e.optText +
          (e.onSuccess ? ' <em>(on success)</em>' : '') + '</span>'
        ).join('<br>');
        return '<div class="m-event">' +
          '<div class="m-event-info">' +
          '<div class="m-event-id">' + eid + '</div>' +
          '<div class="m-event-desc">' + entryDesc + '</div></div>' +
          '<div class="m-prog-badge">+' + info.total + '</div></div>';
      }).join('');

    return '<div class="mystery-section">' +
      '<div class="mystery-header">' +
      '<div class="mystery-name">' + m.name + '</div>' +
      '<div class="mystery-meta">Required: ' + m.required_progress +
      ' &nbsp;|&nbsp; Max available: ' + totalProgress +
      ' &nbsp;|&nbsp; Sort order: ' + m.sort_order + '</div>' +
      '<div class="pbar-wrap"><div class="pbar" style="width:' + pct + '%"></div></div></div>' +
      '<div class="mystery-body">' +
      (m.description ? '<p class="loc-desc" style="margin-bottom:12px">' + m.description + '</p>' : '') +
      '<div class="sec-title">Contributing events (all target the currently-active mystery at runtime)</div>' +
      (eventRows || '<p class="no-results">No events with AdvanceMystery consequences found.</p>') +
      (m.completion_text
        ? '<div class="sec-title completion-text">Completion text</div><div class="ev-text">' + m.completion_text + '</div>'
        : '') +
      '</div></div>';
  }).join('');

  document.getElementById('tab-paths').innerHTML = mysteryHtml;
}

// ── Items tab ─────────────────────────────────────────────────────────────────
function renderItems() {
  const items = Object.values(DATA.items).sort((a, b) => a.name.localeCompare(b.name));
  const cards = items.map(item => {
    const bonuses = [];
    if (item.stamina_bonus) bonuses.push('<span class="con c-sp">STAMINA +' + item.stamina_bonus + '</span>');
    if (item.reason_bonus)  bonuses.push('<span class="con c-sp" style="color:var(--reason)">REASON +'  + item.reason_bonus  + '</span>');

    return '<div class="item-card">' +
      '<div class="item-name">' + item.name + '</div>' +
      '<div class="item-id">' + item.id + '</div>' +
      (item.description ? '<p class="item-desc">' + item.description + '</p>' : '') +
      (bonuses.length ? '<div class="item-bonuses">' + bonuses.join('') + '</div>' : '') +
      '</div>';
  }).join('');

  document.getElementById('tab-items').innerHTML =
    '<div class="item-grid">' + (cards || '<p class="no-results">No items found.</p>') + '</div>';
}

// ── Expand / collapse ─────────────────────────────────────────────────────────
function toggleCard(header) {
  const body = header.nextElementSibling;
  const icon = header.querySelector('.expand-icon');
  const open = body.classList.toggle('open');
  if (icon) icon.textContent = open ? '\u25bc' : '\u25b6';
}
function toggleLocCard(header) {
  header.nextElementSibling.classList.toggle('hidden');
}

// ── Jump from location to event ───────────────────────────────────────────────
function jumpToEvent(eid) {
  showTab('events');
  renderEventsTab('');
  setTimeout(() => {
    const el = document.getElementById('ev-' + eid);
    if (!el) return;
    el.scrollIntoView({behavior: 'smooth', block: 'start'});
    const body = el.querySelector('.ev-body');
    const icon = el.querySelector('.expand-icon');
    if (body && !body.classList.contains('open')) {
      body.classList.add('open');
      if (icon) icon.textContent = '\u25bc';
    }
    el.style.outline = '2px solid var(--accent)';
    setTimeout(() => { el.style.outline = ''; }, 2000);
  }, 40);
}

// ── Init ──────────────────────────────────────────────────────────────────────
renderLocations();
renderEventsTab();
renderPaths();
renderItems();
</script>
</body>
</html>
"""

if __name__ == "__main__":
    main()
