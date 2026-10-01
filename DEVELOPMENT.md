# Stanchions — Development Notebook

> **Purpose:** Living reference for the Stanchions Tekla Structures plugin.
> Update this file when a version is complete but ask for approval to update first. 
> Use it as the starting context for any new development session.

---

## Project at a Glance

| Item | Value |
|---|---|
| Plugin name (Tekla) | `Stanchions` |
| Target | Tekla Structures 2026 · .NET 4.8 · x64 |
| Source | `C:\Quinn\Documents\Tekla API\New Project Organized\Stanchions` |
| Deploy (Debug) | `C:\ProgramData\Trimble\Tekla Structures\2026.0\Environments\common\extensions\Stanchions\` |
| Standards | `TeklaContext.md` in project root — read this before touching any Tekla API code |

---

## What This Plugin Does

Replaces Tekla's built-in **Stanchions (S76)** component, which has had unfixed bugs with
non-level stringers since 2019. Core design decisions:

| S76 approach | Stanchions approach |
|---|---|
| Places posts using a 2D picture-based algorithm that breaks on sloped bases | Ray-casts a vertical line at each XY station, seats the post exactly on the highest intersection |
| Compiled binary — cannot be fixed | Our own C# code — fully fixable |
| Posts may tilt with stringer | Posts are always **plumb (global Z)** |
| Dialog: numbered fields with descriptions (Advance Steel style), Example 1.Profile |

---

## Architecture

```
TeklaContext.md             ← Coding standards (read before any API change)
```

### Execution flow

```
DefineInput()
  └─ Picker: 2 points (run start / end)
  └─ Picker: N base parts (stringers, slabs, concrete)
  └─ Returns: List<InputDefinition> with points ArrayList + identifiers ArrayList

Run(input)
  └─ ApplyDefaults()           — fill unset fields with sensible values
  └─ Transform points to global coordinate system
  └─ Resolve Part objects from identifiers
  └─ SetCurrentTransformationPlane(global)
  └─ PlacePosts(start, end, parts)
       └─ ComputeStations(usable) — returns list of offsets from FirstDistance
       └─ for each station:
            └─ TrySeatOnBase(xy, parts) — vertical ray cast → highest intersection Z
            └─ ComputePostHeight(seatPart) — fixed height OR nosing-offset formula
            └─ InsertPlumbPost(seat, postHeight, isFirst, isLast)
  └─ Restore previous transformation plane (in finally)
  └─ model.CommitChanges()     — ONE commit at the very end
  └─ return true / false
```

### Key rules from TeklaContext.md

- `_model` is created **once** in the constructor, never inside methods or loops.
- `CommitChanges()` is called **once**, at the very end of `Run()`.
- `Beam.Type` is **read-only** — pass `BeamTypeEnum.COLUMN` to the constructor.
- `foreach` on a `ModelObjectEnumerator` is **forbidden** — use `while (enumerator.MoveNext())`.
- After `Insert()`, property changes require `.Modify()` to take effect.
- Operative parts for boolean cuts **must** be deleted immediately after `cut.Insert()`.
- Always reset the work plane — done in a `finally` block.

---

## Field Reference (all `[StructuresField]` keys)

### Parameters (Tab 1)

| Field key | C# property | Type | Default | Description |
|---|---|---|---|---|
| `startOffset` | `StartOffset` | double | 100 mm | ③ Distance from run start to first post |
| `endOffset` | `EndOffset` | double | 100 mm | ④ Distance from run end to last post |
| `space_scheme` | `SpaceScheme` | int | 1 | 1 = Max span · 0 = Exact span |
| `maxspan` | `MaxSpan` | double | 1000 mm | ⑤ Max post spacing (equal divisions ≤ MaxSpan) |
| `exactspan` | `ExactSpan` | double | 1000 mm | ⑤ Fixed spacing |
| `soffset` | `Offset` | double | 0 mm | ⑧ Lateral offset from run line |
| `offsetside` | `OffsetSide` | int | 0 | 0 = right · 1 = left |
| `sheight` | `Height` | double | 1100 mm | Post height|
| `nosing_offset` | `NosingOffset` | double | 0 mm | ① Perpendicular offset from stringer surface to nosing reference line |
| `height_above_nosing` | `HeightAboveNosing` | double | 1100 mm | ② Vertical height from nosing line to post top |
| `level_start_mode` | `LevelStartMode` | int | 0 | 0 = extend slope calc · 1 = custom height |
| `level_start_height` | `LevelStartHeight` | double | 1100 mm | ⑥ Post height on level stringer at start |
| `level_end_mode` | `LevelEndMode` | int | 0 | 0 = extend slope calc · 1 = custom height |
| `level_end_height` | `LevelEndHeight` | double | 1100 mm | ⑦ Post height on level stringer at end |

### Parts (Tab 2)

| Field key | C# property | Type | Default | Description |
|---|---|---|---|---|
| `Fsprof` | `FirstProfile` | string | PIPE1-1/4SCH40 | First post profile |
| `mat2` | `FirstMaterial` | string | A53-GR.B | First post material |
| `ClassFirst` | `ClassFirst` | int | 11 | First post class |
| `partname2` | `FirstPartName` | string | POST | First post name |
| `Msprof` | `MidProfile` | string | PIPE1-1/4SCH40 | Middle post profile |
| `mat1` | `MidMaterial` | string | A53-GR.B | Middle post material |
| `ClassMid` | `ClassMid` | int | 11 | Middle post class |
| `partname1` | `MidPartName` | string | POST | Middle post name |
| `Lsprof` | `LastProfile` | string | PIPE1-1/4SCH40 | Last post profile |
| `mat3` | `LastMaterial` | string | A53-GR.B | Last post material |
| `ClassLast` | `ClassLast` | int | 11 | Last post class |
| `partname3` | `LastPartName` | string | POST | Last post name |

### Embed / Core Drill (Tab 3) — *planned v3.0*

| Field key | C# property | Type | Default | Description |
|---|---|---|---|---|
| `embed_mode` | `EmbedMode` | int | 0 | 0 = stop at surface · 1 = embed |
| `embed_depth` | `EmbedDepth` | double | 101.6 mm | How far below the surface the post extends |
| `core_drill` | `CoreDrill` | int | 0 | 0 = none · 1 = offset from face · 2 = custom diameter |
| `core_drill_offset` | `CoreDrillOffset` | double | 12.7 mm | Added to the round pipe outside diameter to determine the clearance on each side |
| `core_drill_diam` | `CoreDrillDiameter` | double | 69.85 mm | Custom drill hole diameter |

---

## Numbering Reference

Part number: prefix **`r`**, start number **100**
Assembly number: prefix **`""`** (empty), start number **1**

---

## Version Roadmap

### ✅ v1.0 — Base Plugin
Core functionality, build pipeline, deploy.

**What's in it:**
- Multiple points to define path + N-part pick input
- Vertical ray-cast seat detection (`Solid.Intersect`)
- Plumb post insertion (`Beam.BeamTypeEnum.COLUMN`)
- Max span and Exact span spacing modes
- First / Mid / Last post profiles, materials, classes, names
- Assembly/part number prefixes
- 2-tab (Parameters + Parts)
- Post-build auto-deploy to Tekla extensions folder

---

### 🔲 v1.1 — Dialog Redesign + Profile Linking
*UI-only changes. No geometry logic changes.*

**Goals:**
- 3-tab dialog: Parameters · Parts · Embed 
- Numbered parameter labels (1. 2. 3. …) matching reference diagram(Note: User will update reference diagram based on parameter labels)
- No profile-link dropdown for the first post; its profile remains independently specified
- `mprofile_link` dropdown for middle post: Custom | Match First | Match Last
- `lprofile_link` dropdown for last post: Custom | Match First | Match Middle
- `mprofile_link` and `lprofile_link` default to Match First

**New fields:** `mprofile_link` `lprofile_link`

**Test checklist v1.1:**
- [ ] Build succeeds — 0 errors, 0 warnings
- [ ] DLL deploys to extensions folder
- [ ] Dialog opens with 3 tabs visible
- [ ] Parameters tab: all spacing fields present and labeled
- [ ] Parts tab: Height field present
- [ ] Parts tab: `mprofile_link` and `lprofile_link` dropdowns are visible, each with its documented three options
- [ ] Existing v1.0: posts still place correctly

---

### 🔲 v2.0 — Stair Mode (Nosing Offset)
*New height calculation geometry. Requires stringer slope detection.*

**Goals:**
- `height_mode` toggle in Parameters tab
- When `height_mode = 1` (Stair):
  - Read `nosing_offset` (perpendicular) and `height_above_nosing` (vertical)
  - Compute stringer slope from `Beam.StartPoint` / `Beam.EndPoint`
  - `post_height = nosing_offset × cos(θ) + height_above_nosing`
  - All posts on the same sloped stringer get the same height → handrail is parallel to stringer ✅
- `TrySeatOnBase()` updated to also return the winning `Part`

**New fields:** `height_mode`, `nosing_offset`, `height_above_nosing` (`nosing_offset` and `height_above_nosing` are already listed in the Field Reference)

**Math:**
```
stringer direction: v = (dx, dy, dz) / |v|
horizontal magnitude: h = sqrt(dx² + dy²) / |v|   (= cos θ)
post_height = nosing_offset × h + height_above_nosing
```

**Test checklist v2.0:**
- [ ] Build succeeds
- [ ] Simple mode (`height_mode = 0`): behaviour identical to v1.1
- [ ] Stair mode on a perfectly flat stringer: `post_height = nosing_offset × 1.0 + height_above_nosing` (h=1)
- [ ] Stair mode on a 30° slope stringer: measure actual post heights — all equal, value ≈ `nosing_offset × cos(30°) + height_above_nosing`
- [ ] Stair mode on a 45° slope stringer: value ≈ `nosing_offset × 0.707 + height_above_nosing`
- [ ] Fallback: if seatPart is not a Beam, use `_data.Height` (no crash)

---

### 🔲 v2.1 — Level Section Height Overrides
*Companion to v2.0. Detects level parts and applies per-side height rules.*

**Goals:**
- Auto-detect level parts (slope angle < 5°)
- Determine start vs end by centroid proximity to run start/end point
- Per-side mode: mode 0 uses the leveled-stringer perpendicular-offset calculation; mode 1 uses a custom fixed height
- When mode = 0: seat each post at its intersection with the leveled stringer, then calculate height using `nosing_offset` measured perpendicular from the stringer's top face, plus `height_above_nosing`
- When mode = 1: level posts use `LevelStartHeight` or `LevelEndHeight`

**New fields:** `level_start_mode`, `level_start_height`, `level_end_mode`, `level_end_height`

**New helper methods:**
- `IsLevel(Part, threshold=5°) → bool`
- `IsNearStart(Part, runStart, runEnd) → bool`

**Test checklist v2.1:**
- [ ] Build succeeds
- [ ] No level parts picked: behaviour identical to v2.0
- [ ] Level part at start, mode=0: post seat is at the stringer intersection and height uses the perpendicular `nosing_offset` from the top face plus `height_above_nosing`
- [ ] Level part at start, mode=1: posts use `LevelStartHeight`
- [ ] Level part at end, mode=0: post seat is at the stringer intersection and height uses the perpendicular `nosing_offset` from the top face plus `height_above_nosing`
- [ ] Level part at end, mode=1: posts use `LevelEndHeight`
- [ ] Both level parts present, both modes=1: each side uses its own height
- [ ] Sloped part between two level parts: sloped posts use stair formula correctly

---

### 🔲 v3.0 — Embed + Core Drill
*New boolean cut logic. Fully independent of v2.x — can be used on flat or sloped stringers.*

**Goals:**
- `embed_mode`: extend post below the stringer surface by `embed_depth`
- `core_drill`: boolean-cut a cylinder through the base part at each post location
  - Mode 1: diameter = parsed round pipe outside diameter + 2 × `core_drill_offset`
  - Mode 2: diameter = `core_drill_diameter`
- `GetPipeOuterDiameter(Beam post)`: parse the round pipe profile string to obtain its outside diameter; if it cannot be parsed, use `CoreDrillDiameter` as the hole diameter

**Boolean cut rules (from TeklaContext.md):**
- Operative part class must be `BooleanPart.BooleanOperativeClassName`
- Call `cut.SetOperativePart()` (not `.OperativePart = ...`)
- **Always** call `operative.Delete()` immediately after `cut.Insert()`

**Test checklist v3.0:**
- [ ] Build succeeds
- [ ] `embed_mode = 0`: post bottom at stringer surface (unchanged)
- [ ] `embed_mode = 1`: post bottom is `embed_depth` below stringer surface
- [ ] `core_drill = 0`: no boolean cut in model
- [ ] `core_drill = 1`: cylindrical hole exists in base part; diameter = round pipe outside diameter + 2×offset
- [ ] `core_drill = 2`: cylindrical hole exists; diameter = custom value
- [ ] No orphaned operative parts remain in model after run
- [ ] Core drill works with embed mode on
- [ ] Core drill on non-Beam base part (ContourPlate): no crash, fallback to custom diameter

---

## Regression Checklist (run after EVERY version)

Run these in Tekla Structures 2026 with a fresh model after every build before merging/testing.

```
□ Build: dotnet build → 0 errors, 0 warnings
□ Deploy: DLL present in extensions folder
□ Dialog opens without error
□ Pick 2 points + 1 flat stringer → posts placed and `CommitChanges()` completes successfully
□ Pick 2 points + 1 sloped stringer → posts plumb (not tilted with stringer)
□ Post bottoms sit on stringer surface (not floating, not buried)
□ First post at correct FirstDistance from start point
□ Last post at correct LastDistance from end point
□ Max span: all gaps ≤ MaxSpan
□ Exact span: gaps equal ExactSpan (except possibly last gap)
□ Part number prefix = "r", start = 100
□ Assembly number prefix = "" (empty), start = 1
□ Undo works: Ctrl+Z removes all posts from one run
```

---

## Known Issues / Bugs

| # | Version found | Description | Status |
|---|---|---|---|
| — | — | No known issues | — |

---

## Ideas for Future Versions

- **Top rail / handrail**: optional horizontal rail inserted at post top
- **Base plate**: optional flat plate welded to post bottom, bolted to stringer
- **Saved attributes**: `.j76`-style attribute files for quick recall of settings
- **Report**: count of posts placed, total steel weight

---

## Build & Deploy Quick Reference

```powershell
# Build and auto-deploy (Debug only)
dotnet build C:\Users\eduar\source\repos\Stanchions\Stanchions.csproj

# Verify DLL is in Tekla extensions
Test-Path "C:\ProgramData\Trimble\Tekla Structures\2026.0\Environments\common\extensions\Stanchions\Stanchions.dll"

# After build: restart Tekla (or open a model) for the new DLL to load
```

---

*Last updated: 2026-09-30 · Version: v1.0 complete, v1.1 planned*

