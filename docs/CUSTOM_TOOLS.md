# Custom Tools Development Guide

This guide shows you how to create your own MCP tools for Revit. Follow the examples to add new functionality that Claude can use.

## Understanding Tool Architecture

Every tool in the system:
1. **Implements `ITool` interface** - Defines name, description, and input schema
2. **Inherits from `ToolBase`** - Provides common functionality and helpers
3. **Uses `RevitContextManager`** - Ensures thread-safe Revit API access
4. **Returns `ToolsCallResult`** - Formatted response for Claude

## The 3-Step Process

### Step 1: Create the Tool Class
### Step 2: Register the Tool
### Step 3: Rebuild and Test

---

## Example 1: Simple Tool (No Parameters)

Let's create a tool that gets the current Revit version.

### Step 1: Create `GetRevitVersionTool.cs`

Create file: `Tools/Document/GetRevitVersionTool.cs`

```csharp
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;

namespace RevitClaudeMCP.Tools.Document
{
    /// <summary>
    /// Get Revit application version information
    /// </summary>
    public class GetRevitVersionTool : ToolBase
    {
        // Tool name - must be unique
        public override string Name => "get_revit_version";

        // Description shown to Claude
        public override string Description =>
            "Get the current Revit application version and build information.";

        // Input schema - no parameters needed
        public override JObject InputSchema => CreateSchema(
            "object",
            "No parameters required",
            new JObject { },
            required: new string[] { }
        );

        // Constructor - inject RevitContextManager
        public GetRevitVersionTool(RevitContextManager context) : base(context)
        {
        }

        // Tool implementation
        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            // Execute on Revit thread (read-only, no transaction needed)
            var versionInfo = await _context.ExecuteReadOnlyAsync(doc =>
            {
                var app = doc.Application;

                string info = $"Revit Version Information:\n";
                info += $"Version Name: {app.VersionName}\n";
                info += $"Version Number: {app.VersionNumber}\n";
                info += $"Version Build: {app.VersionBuild}\n";
                info += $"SubVersion Number: {app.SubVersionNumber}\n";
                info += $"Language: {app.Language}";

                return info;
            });

            // Return result as text
            return CreateTextResult(versionInfo);
        }
    }
}
```

### Step 2: Register the Tool

Open `Core/Server/MCPServerManager.cs` and add to `InitializeToolRegistry()` method:

```csharp
// Add this line with other document tools
_toolRegistry.RegisterTool(new GetRevitVersionTool(_contextManager));
```

### Step 3: Test

1. Rebuild project
2. Restart Revit
3. In Claude: "What version of Revit am I using?"

**Expected Output:**
```
Revit Version Information:
Version Name: Autodesk Revit 2024
Version Number: 2024
Version Build: 20230115_0715(x64)
...
```

---

## Example 2: Tool with Parameters

Let's create a tool that finds elements by name.

### Step 1: Create `FindElementsByNameTool.cs`

Create file: `Tools/Query/FindElementsByNameTool.cs`

```csharp
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using RevitClaudeMCP.Utils;
using Autodesk.Revit.DB;

namespace RevitClaudeMCP.Tools.Query
{
    /// <summary>
    /// Find elements by name (partial or exact match)
    /// </summary>
    public class FindElementsByNameTool : ToolBase
    {
        public override string Name => "find_elements_by_name";

        public override string Description =>
            "Find elements in the document by name. Supports partial name matching.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for finding elements by name",
            new JObject
            {
                // Required parameter
                ["name"] = CreateProperty("string",
                    "Name or partial name to search for"),

                // Optional parameter with default
                ["exactMatch"] = CreateProperty("boolean",
                    "Whether to require exact name match (default: false)", false),

                // Optional parameter with limit
                ["limit"] = CreateNumberProperty(
                    "Maximum number of results to return (default: 50)", 1, 500)
            },
            required: new[] { "name" } // Only 'name' is required
        );

        public FindElementsByNameTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            // Get parameters using helper methods
            string name = GetRequiredParam<string>(arguments, "name");
            bool exactMatch = GetOptionalParam(arguments, "exactMatch", false);
            int limit = GetOptionalParam(arguments, "limit", 50);

            // Execute query
            var result = await _context.ExecuteReadOnlyAsync(doc =>
            {
                // Find elements
                var elements = ElementHelper.FindElementsByName(doc, name, exactMatch)
                    .Take(limit)
                    .ToList();

                // Format results
                var sb = new StringBuilder();
                sb.AppendLine($"=== Found {elements.Count} element(s) ===");
                sb.AppendLine($"Search: '{name}' ({(exactMatch ? "exact" : "partial")} match)\n");

                foreach (var element in elements)
                {
                    sb.AppendLine($"ID: {element.Id.IntegerValue}");
                    sb.AppendLine($"  Name: {element.Name}");
                    sb.AppendLine($"  Category: {ElementHelper.GetCategoryName(element)}");
                    sb.AppendLine($"  Type: {ElementHelper.GetElementTypeName(element, doc)}");

                    XYZ location = ElementHelper.GetLocationPoint(element);
                    if (location != null)
                    {
                        sb.AppendLine($"  Location: {GeometryHelper.FormatPoint(location)}");
                    }

                    sb.AppendLine();
                }

                return sb.ToString();
            });

            return CreateTextResult(result);
        }
    }
}
```

### Step 2: Register

```csharp
_toolRegistry.RegisterTool(new FindElementsByNameTool(_contextManager));
```

### Step 3: Test

```
Find all elements with "Wall" in the name
Find element with exact name "Basic Wall: Generic - 200mm"
```

---

## Example 3: Complex Tool with Multiple Operations

Let's create a tool that creates a room with specific properties.

### Step 1: Create `CreateRoomTool.cs`

Create file: `Tools/Creation/CreateRoomTool.cs`

```csharp
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using RevitClaudeMCP.Utils;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitClaudeMCP.Tools.Creation
{
    /// <summary>
    /// Create a room with specified properties
    /// </summary>
    public class CreateRoomTool : ToolBase
    {
        public override string Name => "create_room";

        public override string Description =>
            "Create a room at specified location with name and number. " +
            "Rooms must be placed within bounded areas.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for creating a room",
            new JObject
            {
                ["x"] = CreateNumberProperty("Room location X coordinate (feet)"),
                ["y"] = CreateNumberProperty("Room location Y coordinate (feet)"),
                ["z"] = CreateNumberProperty("Room location Z coordinate (feet)", 0),
                ["name"] = CreateProperty("string", "Room name"),
                ["number"] = CreateProperty("string", "Room number"),
                ["levelName"] = CreateProperty("string",
                    "Level name (optional, uses nearest level if not specified)", null)
            },
            required: new[] { "x", "y", "name", "number" }
        );

        public CreateRoomTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            // Get parameters
            double x = GetRequiredParam<double>(arguments, "x");
            double y = GetRequiredParam<double>(arguments, "y");
            double z = GetOptionalParam<double>(arguments, "z", 0);
            string roomName = GetRequiredParam<string>(arguments, "name");
            string roomNumber = GetRequiredParam<string>(arguments, "number");
            string levelName = GetOptionalParam<string>(arguments, "levelName", null);

            // Create room (requires transaction)
            var result = await _context.ExecuteWithTransactionAsync("Create Room", doc =>
            {
                // Find level
                Level level;
                if (!string.IsNullOrEmpty(levelName))
                {
                    // Find level by name
                    level = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault(l => l.Name.Equals(levelName,
                            System.StringComparison.OrdinalIgnoreCase));

                    if (level == null)
                    {
                        throw new System.Exception($"Level '{levelName}' not found");
                    }
                }
                else
                {
                    // Find nearest level below the point
                    level = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .Where(l => l.Elevation <= z)
                        .OrderByDescending(l => l.Elevation)
                        .FirstOrDefault();

                    if (level == null)
                    {
                        // Use lowest level
                        level = new FilteredElementCollector(doc)
                            .OfClass(typeof(Level))
                            .Cast<Level>()
                            .OrderBy(l => l.Elevation)
                            .FirstOrDefault();
                    }

                    if (level == null)
                    {
                        throw new System.Exception("No levels found in document");
                    }
                }

                // Get active phase
                Phase phase = doc.Phases.Cast<Phase>().Last();

                // Create UV point (rooms use UV coordinates on level)
                UV roomPoint = new UV(x, y);

                // Create room
                Room room = doc.Create.NewRoom(level, roomPoint);

                if (room == null)
                {
                    throw new System.Exception(
                        "Failed to create room. Ensure location is within bounded area.");
                }

                // Set room properties
                room.Name = roomName;
                room.Number = roomNumber;

                // Get room info
                string resultMsg = $"Room created successfully!\n";
                resultMsg += $"Element ID: {room.Id.IntegerValue}\n";
                resultMsg += $"Name: {room.Name}\n";
                resultMsg += $"Number: {room.Number}\n";
                resultMsg += $"Level: {level.Name}\n";
                resultMsg += $"Location: {GeometryHelper.FormatPoint(new XYZ(x, y, z))}\n";

                // Get area if available
                Parameter areaParam = room.get_Parameter(BuiltInParameter.ROOM_AREA);
                if (areaParam != null && areaParam.HasValue)
                {
                    double area = areaParam.AsDouble();
                    resultMsg += $"Area: {area:F2} sq ft";
                }

                return resultMsg;
            });

            return CreateTextResult(result);
        }
    }
}
```

### Step 2: Register

```csharp
_toolRegistry.RegisterTool(new CreateRoomTool(_contextManager));
```

### Step 3: Test

```
Create a room named "Conference Room" with number "101" at location (10, 20, 0)
```

---

## Common Patterns and Best Practices

### 1. Parameter Validation

Always validate parameters before using:

```csharp
protected override void ValidateArguments(JObject arguments)
{
    string name = GetRequiredParam<string>(arguments, "name");

    if (string.IsNullOrWhiteSpace(name))
    {
        throw new ArgumentException("Name cannot be empty");
    }

    int count = GetRequiredParam<int>(arguments, "count");

    if (count < 1 || count > 1000)
    {
        throw new ArgumentException("Count must be between 1 and 1000");
    }
}
```

### 2. Transaction Management

**Read-only operations** (no document changes):
```csharp
var result = await _context.ExecuteReadOnlyAsync(doc =>
{
    // Query data only
    return doc.Title;
});
```

**Modification operations** (changes document):
```csharp
var result = await _context.ExecuteWithTransactionAsync("Operation Name", doc =>
{
    // Create, modify, or delete elements
    // Transaction automatically committed or rolled back
    return element.Id.IntegerValue;
});
```

### 3. Error Handling

Errors are automatically caught by `ToolBase`, but you can provide more context:

```csharp
protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
{
    try
    {
        // Your code
    }
    catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
    {
        return CreateErrorResult($"Revit operation failed: {ex.Message}");
    }
}
```

### 4. Working with Categories

Common category patterns:

```csharp
// Get elements by built-in category
var walls = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Walls)
    .WhereElementIsNotElementType()
    .ToList();

// Category enum reference
BuiltInCategory.OST_Walls              // Walls
BuiltInCategory.OST_Floors             // Floors
BuiltInCategory.OST_Doors              // Doors
BuiltInCategory.OST_Windows            // Windows
BuiltInCategory.OST_Rooms              // Rooms
BuiltInCategory.OST_StructuralColumns  // Columns
BuiltInCategory.OST_StructuralFraming  // Beams
BuiltInCategory.OST_MEPSpaces          // MEP Spaces
BuiltInCategory.OST_DuctCurves         // Ducts
BuiltInCategory.OST_PipeCurves         // Pipes
```

### 5. Working with Parameters

```csharp
// Get parameter value
Parameter param = element.LookupParameter("Comments");
if (param != null && param.HasValue)
{
    string value = ElementHelper.GetParameterValueAsString(param);
}

// Set parameter value
Parameter param = element.LookupParameter("Comments");
if (param != null && !param.IsReadOnly)
{
    bool success = ElementHelper.SetParameterValue(param, "New value");
}

// Get built-in parameter
Parameter levelParam = element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
```

### 6. Geometry Operations

```csharp
// Create points
XYZ point = new XYZ(x, y, z);
XYZ point2 = GeometryHelper.CreatePoint(x, y, z);

// Create lines
Line line = Line.CreateBound(startPoint, endPoint);
Line line2 = GeometryHelper.CreateLine(startPoint, endPoint);

// Transformations
Transform translation = Transform.CreateTranslation(new XYZ(5, 0, 0));
Transform rotation = GeometryHelper.CreateRotation(origin, axis, angleDegrees);

// Unit conversions
double mm = GeometryHelper.FeetToMm(feet);
double feet = GeometryHelper.MmToFeet(mm);
```

---

## Tool Schema Reference

### Basic Property Types

```csharp
// String
CreateProperty("string", "Description")

// Number
CreateNumberProperty("Description", minValue, maxValue)

// Boolean
CreateProperty("boolean", "Description", defaultValue)

// Enum (dropdown)
CreateEnumProperty("Description", "option1", "option2", "option3")
```

### Complex Schemas

```csharp
// Object with nested properties
new JObject
{
    ["point"] = new JObject
    {
        ["type"] = "object",
        ["properties"] = new JObject
        {
            ["x"] = CreateNumberProperty("X coordinate"),
            ["y"] = CreateNumberProperty("Y coordinate"),
            ["z"] = CreateNumberProperty("Z coordinate")
        },
        ["required"] = new JArray { "x", "y" }
    }
}

// Array of values
new JObject
{
    ["elementIds"] = new JObject
    {
        ["type"] = "array",
        ["items"] = new JObject
        {
            ["type"] = "integer"
        },
        ["description"] = "List of element IDs"
    }
}
```

---

## Debugging Your Tools

### 1. Use Visual Studio Debugger

1. Set breakpoint in your tool's `ExecuteToolAsync` method
2. Press **F5** to start debugging
3. Revit launches with debugger attached
4. Call your tool from Claude
5. Execution stops at breakpoint

### 2. Add Logging

```csharp
protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
{
    Logger.Log($"Tool {Name} called with arguments: {arguments}");

    // Your code

    Logger.Log($"Tool {Name} completed successfully");
    return CreateTextResult(result);
}
```

Check logs at: `%AppData%\RevitClaudeMCP\Logs\`

### 3. Test Tool Directly

Create a test command to call your tool without Claude:

```csharp
[Transaction(TransactionMode.Manual)]
public class TestToolCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var context = Application.Instance.ContextManager;
        var tool = new YourNewTool(context);

        var args = new JObject
        {
            ["paramName"] = "value"
        };

        var result = tool.ExecuteAsync(args).Result;

        TaskDialog.Show("Test Result", result.Content[0].Text);

        return Result.Succeeded;
    }
}
```

---

## Tool Categories Organization

Organize tools by function:

- **Tools/Document/** - Document info, views, settings
- **Tools/Creation/** - Create elements (walls, floors, etc.)
- **Tools/Modification/** - Modify existing elements
- **Tools/Query/** - Search and query elements
- **Tools/Analysis/** - Calculate, analyze, report
- **Tools/Import/** - Import data/files
- **Tools/Export/** - Export data/reports
- **Tools/Utility/** - Helper operations

---

## Next Steps

1. ✅ Start with simple query tools
2. ✅ Add creation tools for your workflow
3. ✅ Build specialized domain tools (MEP, Structure, etc.)
4. ✅ Share your tools with the community!

**Tip:** Look at existing tools in the codebase for more examples and patterns.
