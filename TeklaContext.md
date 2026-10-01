# TeklaContext.md
> Reusable AI agent context for Tekla Structures plugin development.
> Target: Tekla Structures 2026, .NET 4.8, C#. Strip project-specific values before reusing.

---

## 1. Namespaces & Imports

```csharp
using Tekla.Structures.Geometry3d;       // Point, Vector, Line, CoordinateSystem
using Tekla.Structures.Model;            // Beam, ContourPlate, BooleanPart, Fitting, BoltArray, Weld, Model, Position
using Tekla.Structures.Model.UI;         // Picker, GraphicsDrawer
using Tekla.Structures.Model.Operations; // Operation.MoveObject, CopyObject
using Tekla.Structures.Plugins;          // PluginBase, ConnectionBase, [Plugin], [PluginUserInterface], [StructuresField]
using Tekla.Structures.Datatype;         // Distance (used for unit conversion guards)
using Tekla.Core.Extensions;             // ToMm(), MoveTowards(), GetDirectionTo(), GetPerpendicular(), GetNormal()
using Tekla.Core.Geometry;              // MathNetAdapters (.ToMathNet() / .ToTekla())
using Tekla.Core.Model;                 // BooleanOperations, PartMover, PipeBuilder
using Tekla.Core.Interactions;          // SafePicker
using MathNet.Spatial.Euclidean;        // Only via Tekla.Core — never reference directly in plugin code
```

---

## 2. Plugin Architecture

```csharp
public class PluginData {
    [StructuresField("myField")] public double myField;
    [StructuresField("myString")] public string myString;
}

[Plugin("PluginName")]
[PluginUserInterface("Namespace.MainForm")]
public class MyPlugin : PluginBase {
    private readonly Model _model;
    private readonly PluginData _data;

    public MyPlugin(PluginData data) {
        _model = new Model();
        _data  = data;
    }

    public override List<InputDefinition> DefineInput() {
        Picker picker = new Picker();

        // Pattern 1: Point-based input (stairs, rails, linear members)
        ArrayList pts = picker.PickPoints(Picker.PickPointEnum.PICK_TWO_POINTS, "Pick start/end");
        return new List<InputDefinition> { new InputDefinition(pts) };

        // Pattern 2: Part-to-part input (connections)
        // ModelObject prim = picker.PickObject(Picker.PickObjectEnum.PICK_ONE_PART, "Pick Primary");
        // ModelObject sec  = picker.PickObject(Picker.PickObjectEnum.PICK_ONE_PART, "Pick Secondary");
        // return new List<InputDefinition> { new InputDefinition(prim.Identifier), new InputDefinition(sec.Identifier) };
    }

    public override bool Run(List<InputDefinition> input) {
        try {
            GetValuesFromDialog();
            ArrayList pts = (ArrayList)input[0].GetInput();
            Point start = pts[0] as Point;
            Point end   = pts[1] as Point;
            // Delegate all geometry/model logic to a dedicated Builder class
            _model.CommitChanges();
            return true;
        } catch (Exception ex) {
            MessageBox.Show(ex.ToString());
            return false; // Return false so Tekla registers execution failure
        }
    }
}
```

**Rules:**
- Store `_model` in the constructor; always use it — never spin up `new Model()` inside methods or loops.
- `Run()` returns `false` in the catch block so Tekla flags the plugin as failed, not partially committed.
- Call `_model.CommitChanges()` **once**, at the very end of `Run()`, after all inserts are complete.
- Delegate all geometry/model logic to a dedicated `XxxBuilder` class with public properties set by the plugin.

---

## 3. Part Instantiation & Modification

```csharp
// Set ALL properties BEFORE Insert() — changes after Insert() require Modify() to take effect.
Beam beam = new Beam {
    StartPoint = start,
    EndPoint   = end,
    Profile    = new Profile  { ProfileString  = "HEIGHTxWIDTH" }, // see ProjectContext.md
    Material   = new Material { MaterialString = "MaterialName"  }, // see ProjectContext.md
    Class      = "N",                                               // see ProjectContext.md — ALWAYS ask if undefined
    Position   = new Position { Depth = Position.DepthEnum.FRONT, Plane = Position.PlaneEnum.MIDDLE }
};
beam.AssemblyNumber.Prefix      = "PREFIX";
beam.AssemblyNumber.StartNumber = 1;
beam.PartNumber.Prefix          = "prefix";
beam.PartNumber.StartNumber     = 100;
beam.Insert();

// Modifying an already-inserted part
beam.Class = "2";
beam.Modify(); // Required — property changes after Insert() are silently ignored without this

// ContourPlate (arbitrary polygon slab, stringer, or operative cut shape)
ContourPlate plate = new ContourPlate {
    Profile  = new Profile  { ProfileString  = "PLThickness" },
    Material = new Material { MaterialString = "MaterialName" },
    Class    = BooleanPart.BooleanOperativeClassName,
    Position = new Position { Depth = Position.DepthEnum.MIDDLE }
};
plate.AddContourPoint(new ContourPoint(pt1, null)); // null = sharp corner
plate.AddContourPoint(new ContourPoint(pt2, new Chamfer(r, 0, Chamfer.ChamferTypeEnum.CHAMFER_ROUNDING)));
plate.Insert();
```

---

## 4. Connections & Detailing: Fittings, Bolts, and Welds

```csharp
// End Cuts & Miters — prefer Fitting over BooleanPart for straight beam-end cuts
Fitting fitting = new Fitting {
    Father = beam,
    Plane  = new Plane { Origin = cutPoint, AxisX = normalVector.GetPerpendicular(), AxisY = normalVector }
};
fitting.Insert();

// Bolts
BoltArray bolts = new BoltArray {
    PartToBeBolted  = primaryPart,
    PartToBoltTo    = secondaryPart,
    FirstPosition   = boltOrigin,
    SecondPosition  = boltOrigin.MoveTowards(direction, 100.0),
    BoltSize        = 19.05,
    BoltStandard    = "A325N",
    BoltType        = BoltGroup.BoltTypeEnum.BOLT_TYPE_WORKSHOP
};
bolts.AddBoltDistX(76.2);
bolts.AddBoltDistY(0.0);
bolts.Insert();

// Weld
Weld weld = new Weld {
    MainObject      = primaryPart,
    SecondaryObject = secondaryPart,
    TypeAbove       = BaseWeld.WeldTypeEnum.WELD_TYPE_FILLET,
    SizeAbove       = 6.35
};
weld.Insert();
```

---

## 5. Boolean Cuts

```csharp
// Pattern: Insert operative → apply cut → delete operative
operativePart.Class = BooleanPart.BooleanOperativeClassName; // "BlOpCl"
operativePart.Insert();

BooleanPart cut = new BooleanPart {
    Father = hostPart,
    Type   = BooleanPart.BooleanTypeEnum.BOOLEAN_CUT
};
cut.SetOperativePart(operativePart); // preferred over setting .OperativePart directly
bool ok = cut.Insert();
operativePart.Delete();              // always clean up the operative immediately

if (!ok) throw new InvalidOperationException("Boolean cut failed.");
```

Use `Tekla.Core.Model.BooleanOperations` helpers (`CopeBeamToBeam`, `Fitting`) for common steel cuts.

---

## 6. Reading Existing Model Objects

```csharp
// Fetch all objects from the model
ModelObjectEnumerator enumerator = _model.GetModelObjects();

// CORRECT iteration — never use foreach on a ModelObjectEnumerator
while (enumerator.MoveNext()) {
    if (enumerator.Current is Beam beam) {
        beam.Select(); // refresh properties from model before reading
        // read beam.Profile.ProfileString, beam.StartPoint, etc.
    }
}

// Selector — filter by type or fetch user's current selection
ModelObjectSelector selector = _model.GetModelObjectSelector();
enumerator = selector.GetSelectedObjects();
```

---

## 7. Geometry & Coordinate System

```csharp
// Direction & translation
Vector dir = start.GetDirectionTo(end); // Tekla.Core.Extensions
dir.Normalize();                         // always normalize before scaling
Point p2 = p1.MoveTowards(dir, 500.0);  // distance in mm

// Perpendicular vectors (horizontal plane)
// WARNING: if dir is purely vertical, GetPerpendicular() falls back to Y-axis cross.
// Always validate the result magnitude > 1e-6 before using.
Vector lateral  = dir.GetPerpendicular();
Vector leftDir  = new Vector(-dir.Y,  dir.X, 0).GetNormal();
Vector rightDir = new Vector( dir.Y, -dir.X, 0).GetNormal();
// GetNormal() returns a new normalized vector — never call on a zero vector.

// Line intersection
Line l1 = new Line(beam1.StartPoint, beam1.EndPoint);
Line l2 = new Line(beam2.StartPoint, beam2.EndPoint);
Point ix = l1.GetIntersection(l2); // returns null if parallel or skew — always null-check

// Non-global coordinate systems
TransformationPlane globalPlane = new TransformationPlane(); // identity / global XY
_model.GetWorkPlaneHandler().SetCurrentTransformationPlane(globalPlane);
// Always reset to global after local-plane work to avoid corrupting subsequent geometry.

// Unit conversion — all Tekla API distances are millimetres
double mm = inches.ToMm(); // Tekla.Core.Extensions: value * 25.4
```

---

## 8. Object Move & Copy

```csharp
// Move via PartMover (Tekla.Core.Model)
PartMover.Move(obj, dir, 500.0);          // translates along normalized direction
PartMover.MovePerpendicular(beam, dir, 200.0);
PartMover.CopyZ(obj, 3000.0);             // vertical copy

// Raw Operation (when PartMover wrappers are insufficient)
Operation.MoveObject(obj, new Vector(dx, dy, dz));
ModelObject copy = Operation.CopyObject(obj, translationVector); // guard: vec.GetLength() > 1e-6

// After any move/copy, refresh the object's cached properties
obj.Select();
```

---

## 9. Tekla.Core Utilities

| Utility | Location | Purpose |
|---|---|---|
| `double.ToMm()` | `DoubleExtensions` | Converts dialog values (inches) → mm |
| `Point.MoveTowards(Vector, double)` | `PointExtensions` | Translate a point along a normalized direction |
| `Point.GetDirectionTo(Point)` | `VectorExtensions` | Raw direction vector between two points |
| `Vector.GetPerpendicular()` | `VectorExtensions` | Horizontal perpendicular via MathNet cross-product |
| `Vector.GetNormal()` | `VectorExtensions` | Returns new normalized vector (never mutates in place) |
| `Line.GetIntersection(Line)` | `LineExtensions` | Wraps `Intersection.LineToLine`; returns null if parallel/skew |
| `MathNetAdapters.ToMathNet/ToTekla` | `Tekla.Core.Geometry` | Bridge between `Tekla.Structures.Geometry3d` and `MathNet.Spatial` |
| `BooleanOperations` | `Tekla.Core.Model` | Beam cope / fitting helpers |
| `PartMover` | `Tekla.Core.Model` | `Move`, `Copy`, `CopyZ` via `Operation`; guards zero-vector copy |
| `PipeBuilder` | `Tekla.Core.Model` | Builder-pattern wrapper for `Beam` with pipe defaults |
| `SafePicker` | `Tekla.Core.Interactions` | Multi-point picker; exits cleanly on Esc |

---

## 10. Negative Rules (Do NOT)

- **Do NOT** use `System.Windows.Media` or `System.Drawing` geometry — use `Tekla.Structures.Geometry3d` exclusively.
- **Do NOT** reference `MathNet.Spatial` directly in plugin code — go through `Tekla.Core.Geometry.MathNetAdapters`.
- **Do NOT** use `foreach` on a `ModelObjectEnumerator` — always iterate with `while (enumerator.MoveNext())`.
- **Do NOT** call `CommitChanges()` after every `Insert()` — batch all inserts and commit once per `Run()`.
- **Do NOT** set properties after `Insert()` without calling `part.Modify()` — changes are silently ignored.
- **Do NOT** spin up `new Model()` inside methods or loops — store it in the constructor and reuse `_model`.
- **Do NOT** return `true` from a plugin's `catch` block — return `false` so Tekla registers the failure.
- **Do NOT** leave operative `ContourPlate`/`Beam` objects in the model after a cut — call `.Delete()` immediately.
- **Do NOT** use `BooleanPart` for simple straight cuts on beam ends — use `Fitting` or `CutPlane` instead.
- **Do NOT** call `Operation.CopyObject` with a zero-length vector — guard with `vec.GetLength() > 1e-6`.
- **Do NOT** call `GetNormal()` or `Normalize()` on a zero vector — validate magnitude first.
- **Do NOT** read back model object properties without calling `.Select()` first after any move/modify.
- **Do NOT** assume a work plane is global after local-plane operations — always reset with `SetCurrentTransformationPlane(new TransformationPlane())`.
- **Do NOT** call `.ToMm()` on distances injected directly from a `[StructuresField]` `double` (when using `TD.Distance` in your ViewModel), because Tekla already converts the dialog values (like inches) into millimetres internally. `ToMm()` should only be used if you are manually parsing strings or working outside the auto-binding system.
- **Do NOT** assign a `Class` number without checking `ProjectContext.md` — ask the user if undefined.
- **Do NOT** assume points picked by the user are in Global Coordinates — always transform them using `workPlaneHandler.GetCurrentTransformationPlane().TransformationMatrixToGlobal.Transform(point)` before switching the active Work Plane.
- **Do NOT** use a `LineSegment` when casting a ray with `Solid.Intersect()`. Use the overload that takes two points: `solid.Intersect(rayBottom, rayTop)`.
- **Do NOT** use a small Z range (like 10,000 mm) for vertical ray-casts. Use a massive range (e.g. `±1,000,000` mm) to ensure it reaches parts at high real-world elevations.

