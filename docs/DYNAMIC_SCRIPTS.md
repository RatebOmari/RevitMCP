# Dynamic Scripts Guide

Dynamic scripts allow Claude to generate and execute custom C# code in Revit at runtime. This provides unlimited flexibility beyond predefined tools.

## Table of Contents

- [How Dynamic Scripts Work](#how-dynamic-scripts-work)
- [Security Model](#security-model)
- [Script Structure](#script-structure)
- [Helper Methods](#helper-methods)
- [Example Scripts](#example-scripts)
- [Best Practices](#best-practices)
- [Advanced Patterns](#advanced-patterns)

---

## How Dynamic Scripts Work

### The Flow

```
1. Claude generates C# code based on your request
2. Code sent to execute_script tool
3. Script validated for security (blocks dangerous operations)
4. Approval dialog shows you the code
5. You click "Approve" or "Reject"
6. If approved: Code compiled using Roslyn
7. Compiled assembly executed in Revit transaction
8. Results returned to Claude
```

### Execution Context

Scripts run with:
- Full access to Revit API
- Access to active UIApplication
- Transaction automatically managed
- Helper methods for common operations
- Security constraints enforced

---

## Security Model

### What's Allowed ✅

- ✅ All Revit API operations
- ✅ LINQ queries
- ✅ Standard .NET collections
- ✅ Math operations
- ✅ String manipulation
- ✅ Creating/modifying/deleting Revit elements
- ✅ Reading/writing element parameters
- ✅ Showing dialogs to user

### What's Blocked ❌

- ❌ File I/O (System.IO.File, Directory, etc.)
- ❌ Network access (HTTP, Sockets, etc.)
- ❌ Process spawning (Process.Start)
- ❌ Assembly loading (dynamic DLL loading)
- ❌ Registry access
- ❌ Unsafe code
- ❌ Environment manipulation

**Why?** These restrictions prevent malicious scripts from accessing your filesystem, network, or system beyond Revit.

---

## Script Structure

### Basic Structure

Scripts are automatically wrapped in a class. You write the body:

```csharp
// Your code here
var doc = ScriptContext.GetActiveDocument(uiApp);
ScriptContext.ShowMessage("Hello", "Script executed!");
```

Gets wrapped as:

```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

public class DynamicScript
{
    public static void Execute(UIApplication uiApp, string[] args)
    {
        // Your code here
        var doc = ScriptContext.GetActiveDocument(uiApp);
        ScriptContext.ShowMessage("Hello", "Script executed!");
    }
}
```

### Available Variables

- `uiApp` - UIApplication instance (always available)
- `args` - string array of arguments (usually empty)

### Available Using Directives

```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
```

Add more if needed in the script.

---

## Helper Methods

The `ScriptContext` class provides common operations:

### Get Active Document

```csharp
Document doc = ScriptContext.GetActiveDocument(uiApp);
```

### Get Elements by Category

```csharp
var walls = ScriptContext.GetElementsByCategory(doc, BuiltInCategory.OST_Walls);
```

### Get Element by ID

```csharp
Element element = ScriptContext.GetElement(doc, 123456);
```

### Get Parameter Value

```csharp
string value = ScriptContext.GetParameter(element, "Comments");
```

### Set Parameter Value

```csharp
bool success = ScriptContext.SetParameter(element, "Comments", "New value");
```

### Show Message to User

```csharp
ScriptContext.ShowMessage("Title", "Message text");
```

### Log for Debugging

```csharp
ScriptContext.Log("Debug message");
```

---

## Example Scripts

### Example 1: Count Walls by Type

**User Request:**
```
Count all walls grouped by wall type
```

**Generated Script:**
```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);

// Get all walls
var walls = ScriptContext.GetElementsByCategory(doc, BuiltInCategory.OST_Walls);

// Group by type
var grouped = walls.GroupBy(w => doc.GetElement(w.GetTypeId()).Name);

// Build result message
var result = "Wall Count by Type:\n\n";
foreach (var group in grouped.OrderByDescending(g => g.Count()))
{
    result += $"{group.Key}: {group.Count()} walls\n";
}

result += $"\nTotal: {walls.Count} walls";

ScriptContext.ShowMessage("Wall Count", result);
```

**Result:**
```
Wall Count by Type:

Basic Wall: Generic - 200mm: 45 walls
Basic Wall: Generic - 300mm: 23 walls
Curtain Wall: Exterior Glazing: 8 walls

Total: 76 walls
```

---

### Example 2: Rotate Selected Elements

**User Request:**
```
Rotate all selected elements 90 degrees around Z axis
```

**Generated Script:**
```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);
var uidoc = uiApp.ActiveUIDocument;

// Get selected elements
var selection = uidoc.Selection.GetElementIds();

if (selection.Count == 0)
{
    ScriptContext.ShowMessage("Error", "No elements selected");
    return;
}

int rotatedCount = 0;

foreach (var elemId in selection)
{
    Element elem = doc.GetElement(elemId);

    // Get element location
    if (elem.Location is LocationPoint locPoint)
    {
        XYZ origin = locPoint.Point;
        XYZ axis = new XYZ(0, 0, 1); // Z axis
        double angle = Math.PI / 2; // 90 degrees in radians

        Line axisLine = Line.CreateBound(origin, origin + axis);
        ElementTransformUtils.RotateElement(doc, elemId, axisLine, angle);

        rotatedCount++;
    }
    else if (elem.Location is LocationCurve locCurve)
    {
        // For curve-based elements, rotate around midpoint
        Curve curve = locCurve.Curve;
        XYZ midpoint = curve.Evaluate(0.5, true);
        XYZ axis = new XYZ(0, 0, 1);
        double angle = Math.PI / 2;

        Line axisLine = Line.CreateBound(midpoint, midpoint + axis);
        ElementTransformUtils.RotateElement(doc, elemId, axisLine, angle);

        rotatedCount++;
    }
}

ScriptContext.ShowMessage("Rotation Complete",
    $"Rotated {rotatedCount} of {selection.Count} elements by 90 degrees");
```

---

### Example 3: Color Code Walls by Height

**User Request:**
```
Color code all walls based on their height: red for tall (>10ft), yellow for medium (5-10ft), green for short (<5ft)
```

**Generated Script:**
```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);

// Get all walls
var walls = ScriptContext.GetElementsByCategory(doc, BuiltInCategory.OST_Walls)
    .Cast<Wall>()
    .ToList();

// Define colors
Color red = new Color(255, 0, 0);
Color yellow = new Color(255, 255, 0);
Color green = new Color(0, 255, 0);

int redCount = 0, yellowCount = 0, greenCount = 0;

foreach (var wall in walls)
{
    // Get wall height
    Parameter heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
    if (heightParam == null || !heightParam.HasValue)
        continue;

    double height = heightParam.AsDouble(); // in feet

    // Determine color
    Color color;
    if (height > 10.0)
    {
        color = red;
        redCount++;
    }
    else if (height >= 5.0)
    {
        color = yellow;
        yellowCount++;
    }
    else
    {
        color = green;
        greenCount++;
    }

    // Apply override graphics
    OverrideGraphicSettings overrides = new OverrideGraphicSettings();
    overrides.SetProjectionLineColor(color);
    overrides.SetSurfaceForegroundPatternColor(color);

    // Apply to active view
    View activeView = doc.ActiveView;
    activeView.SetElementOverrides(wall.Id, overrides);
}

string result = $"Color coding applied to {walls.Count} walls:\n\n";
result += $"🔴 Tall (>10ft): {redCount}\n";
result += $"🟡 Medium (5-10ft): {yellowCount}\n";
result += $"🟢 Short (<5ft): {greenCount}";

ScriptContext.ShowMessage("Color Coding Complete", result);
```

---

### Example 4: Tag Untagged Rooms

**User Request:**
```
Add room tags to all rooms that don't have tags
```

**Generated Script:**
```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);

// Get all rooms
var rooms = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Rooms)
    .WhereElementIsNotElementType()
    .Cast<Room>()
    .ToList();

// Get all room tags
var existingTags = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_RoomTags)
    .Cast<RoomTag>()
    .Select(tag => tag.TaggedLocalRoomId)
    .ToHashSet();

// Find untagged rooms
var untaggedRooms = rooms.Where(r => !existingTags.Contains(r.Id)).ToList();

if (untaggedRooms.Count == 0)
{
    ScriptContext.ShowMessage("Complete", "All rooms already have tags!");
    return;
}

// Get active view
View activeView = doc.ActiveView;

if (!(activeView is ViewPlan))
{
    ScriptContext.ShowMessage("Error", "Please switch to a floor plan view to add room tags");
    return;
}

int taggedCount = 0;

foreach (var room in untaggedRooms)
{
    // Get room location
    LocationPoint locPoint = room.Location as LocationPoint;
    if (locPoint == null)
        continue;

    XYZ roomPoint = locPoint.Point;

    // Create room tag
    UV uvPoint = new UV(roomPoint.X, roomPoint.Y);

    try
    {
        RoomTag tag = doc.Create.NewRoomTag(
            new LinkElementId(room.Id),
            uvPoint,
            activeView.Id
        );

        if (tag != null)
            taggedCount++;
    }
    catch
    {
        // Room might not be visible in view
        continue;
    }
}

ScriptContext.ShowMessage("Tagging Complete",
    $"Added tags to {taggedCount} of {untaggedRooms.Count} untagged rooms");
```

---

### Example 5: Generate Door Schedule Report

**User Request:**
```
Show me a report of all doors with their room from/to information
```

**Generated Script:**
```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);

// Get all doors
var doors = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Doors)
    .WhereElementIsNotElementType()
    .Cast<FamilyInstance>()
    .OrderBy(d => d.get_Parameter(BuiltInParameter.DOOR_NUMBER)?.AsString())
    .ToList();

var report = "=== DOOR SCHEDULE ===\n\n";
report += $"Total Doors: {doors.Count}\n\n";

foreach (var door in doors)
{
    // Get door number and mark
    string doorNumber = door.get_Parameter(BuiltInParameter.DOOR_NUMBER)?.AsString() ?? "N/A";
    string doorMark = door.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "N/A";

    // Get door type
    ElementType doorType = doc.GetElement(door.GetTypeId()) as ElementType;
    string typeName = doorType?.Name ?? "Unknown";

    // Get rooms
    Phase phase = doc.Phases.Cast<Phase>().Last();
    Room fromRoom = door.FromRoom[phase];
    Room toRoom = door.ToRoom[phase];

    string fromRoomName = fromRoom?.Name ?? "Outside";
    string toRoomName = toRoom?.Name ?? "Outside";

    report += $"Door {doorNumber} (Mark: {doorMark})\n";
    report += $"  Type: {typeName}\n";
    report += $"  From: {fromRoomName}\n";
    report += $"  To: {toRoomName}\n\n";
}

// Show in dialog (for small projects)
if (doors.Count <= 20)
{
    ScriptContext.ShowMessage("Door Schedule", report);
}
else
{
    // For large projects, show summary
    ScriptContext.ShowMessage("Door Schedule",
        $"Generated report for {doors.Count} doors.\nSee log for details.");
    ScriptContext.Log(report);
}
```

---

### Example 6: Align Elements to Grid

**User Request:**
```
Align all selected columns to the nearest grid intersection
```

**Generated Script:**
```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);
var uidoc = uiApp.ActiveUIDocument;

// Get selected elements
var selection = uidoc.Selection.GetElementIds()
    .Select(id => doc.GetElement(id))
    .Where(e => e.Category?.Name == "Structural Columns")
    .ToList();

if (selection.Count == 0)
{
    ScriptContext.ShowMessage("Error", "No structural columns selected");
    return;
}

// Get all grids
var grids = new FilteredElementCollector(doc)
    .OfClass(typeof(Grid))
    .Cast<Grid>()
    .ToList();

if (grids.Count == 0)
{
    ScriptContext.ShowMessage("Error", "No grids found in project");
    return;
}

// Find grid intersections
var intersections = new List<XYZ>();

for (int i = 0; i < grids.Count; i++)
{
    for (int j = i + 1; j < grids.Count; j++)
    {
        Curve curve1 = grids[i].Curve;
        Curve curve2 = grids[j].Curve;

        IntersectionResultArray results;
        SetComparisonResult result = curve1.Intersect(curve2, out results);

        if (result == SetComparisonResult.Overlap && results != null && results.Size > 0)
        {
            intersections.Add(results.get_Item(0).XYZPoint);
        }
    }
}

if (intersections.Count == 0)
{
    ScriptContext.ShowMessage("Error", "No grid intersections found");
    return;
}

int movedCount = 0;

foreach (var column in selection)
{
    // Get column location
    LocationPoint locPoint = column.Location as LocationPoint;
    if (locPoint == null)
        continue;

    XYZ currentLocation = locPoint.Point;

    // Find nearest intersection
    XYZ nearestIntersection = intersections
        .OrderBy(pt => pt.DistanceTo(currentLocation))
        .First();

    // Move column (only X and Y, keep Z)
    XYZ newLocation = new XYZ(
        nearestIntersection.X,
        nearestIntersection.Y,
        currentLocation.Z
    );

    XYZ translation = newLocation - currentLocation;

    if (translation.GetLength() > 0.001) // Only move if significant
    {
        ElementTransformUtils.MoveElement(doc, column.Id, translation);
        movedCount++;
    }
}

ScriptContext.ShowMessage("Alignment Complete",
    $"Aligned {movedCount} of {selection.Count} columns to grid intersections");
```

---

## Best Practices

### 1. Always Get Active Document First

```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);
```

### 2. Check for Null/Empty

```csharp
if (elements.Count == 0)
{
    ScriptContext.ShowMessage("Warning", "No elements found");
    return;
}
```

### 3. Use Try-Catch for Risky Operations

```csharp
try
{
    // Risky operation
    Element elem = doc.GetElement(new ElementId(id));
}
catch (Exception ex)
{
    ScriptContext.ShowMessage("Error", $"Operation failed: {ex.Message}");
    return;
}
```

### 4. Provide User Feedback

```csharp
// Always show results
ScriptContext.ShowMessage("Complete", $"Processed {count} elements");
```

### 5. Handle Edge Cases

```csharp
// Check if active view is appropriate
if (!(doc.ActiveView is ViewPlan))
{
    ScriptContext.ShowMessage("Error", "This operation requires a floor plan view");
    return;
}
```

---

## Advanced Patterns

### Filtering with LINQ

```csharp
var filtered = elements
    .Where(e => e.Category.Name == "Walls")
    .Where(e => {
        Parameter param = e.LookupParameter("Fire Rating");
        return param != null && param.AsInteger() > 0;
    })
    .OrderBy(e => e.Name)
    .ToList();
```

### Working with Geometry

```csharp
// Get element geometry
Options options = new Options();
options.DetailLevel = ViewDetailLevel.Fine;

GeometryElement geomElem = element.get_Geometry(options);

foreach (GeometryObject geomObj in geomElem)
{
    if (geomObj is Solid solid && solid.Volume > 0)
    {
        double volume = solid.Volume;
        // Process solid
    }
}
```

### Creating Complex Elements

```csharp
// Create wall with opening
Wall wall = Wall.Create(doc, curve, levelId, false);

// Create opening
FamilySymbol windowSymbol = /* get window type */;
FamilyInstance window = doc.Create.NewFamilyInstance(
    insertPoint,
    windowSymbol,
    wall,
    level,
    StructuralType.NonStructural
);
```

---

## Debugging Scripts

### Add Logging

```csharp
ScriptContext.Log($"Processing element {element.Id.IntegerValue}");
ScriptContext.Log($"Parameter value: {value}");
```

Check logs at: `%AppData%\RevitClaudeMCP\Logs\`

### Show Intermediate Results

```csharp
ScriptContext.ShowMessage("Debug", $"Found {elements.Count} elements");
```

### Use Revit Debug Output

```csharp
System.Diagnostics.Debug.WriteLine($"Debug: {variable}");
```

View in Visual Studio Output window when debugging.

---

## Common Pitfalls

### ❌ Forgetting Transaction is Auto-Managed

Don't create manual transactions - they're handled automatically:

```csharp
// ❌ WRONG - Don't do this
using (Transaction trans = new Transaction(doc, "My Transaction"))
{
    trans.Start();
    // ...
    trans.Commit();
}

// ✅ CORRECT - Just modify directly
wall.Move(translation);
```

### ❌ Not Checking for Null

```csharp
// ❌ WRONG
Parameter param = element.LookupParameter("Comments");
string value = param.AsString(); // Crash if param is null!

// ✅ CORRECT
Parameter param = element.LookupParameter("Comments");
string value = param != null ? param.AsString() : "N/A";
```

### ❌ Modifying Collection While Iterating

```csharp
// ❌ WRONG
foreach (var elem in elements)
{
    doc.Delete(elem.Id); // Modifying while iterating!
}

// ✅ CORRECT
var idsToDelete = elements.Select(e => e.Id).ToList();
foreach (var id in idsToDelete)
{
    doc.Delete(id);
}
```

---

## Script Library

Save commonly used scripts for reuse:

### Save to File

Create `scripts/` folder in your project and save scripts as `.cs` files.

### Quick Templates

**Element Query Template:**
```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);
var elements = ScriptContext.GetElementsByCategory(doc, BuiltInCategory.OST_[CATEGORY]);

// Process elements
foreach (var elem in elements)
{
    // Your logic here
}

ScriptContext.ShowMessage("Complete", $"Processed {elements.Count} elements");
```

**Parameter Modification Template:**
```csharp
var doc = ScriptContext.GetActiveDocument(uiApp);
var elements = ScriptContext.GetElementsByCategory(doc, BuiltInCategory.OST_[CATEGORY]);

int modifiedCount = 0;

foreach (var elem in elements)
{
    if (ScriptContext.SetParameter(elem, "[PARAM_NAME]", "[VALUE]"))
    {
        modifiedCount++;
    }
}

ScriptContext.ShowMessage("Complete", $"Modified {modifiedCount} elements");
```

---

## Next Steps

1. ✅ Try the example scripts
2. ✅ Experiment with variations
3. ✅ Build your own workflow scripts
4. ✅ Share useful scripts with your team

**Remember:** Dynamic scripts are powerful but require approval for security. Review code carefully before approving!
