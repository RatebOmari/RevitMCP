using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using RevitClaudeMCP.Utils;
using Autodesk.Revit.DB;

namespace RevitClaudeMCP.Tools.Creation
{
    /// <summary>
    /// Create a wall in the active document
    /// </summary>
    public class CreateWallTool : ToolBase
    {
        public override string Name => "create_wall";

        public override string Description =>
            "Create a wall from start point to end point. Coordinates are in feet (Revit's internal unit).";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for creating a wall",
            new JObject
            {
                ["startX"] = CreateNumberProperty("Start point X coordinate (feet)"),
                ["startY"] = CreateNumberProperty("Start point Y coordinate (feet)"),
                ["startZ"] = CreateNumberProperty("Start point Z coordinate (feet)", 0),
                ["endX"] = CreateNumberProperty("End point X coordinate (feet)"),
                ["endY"] = CreateNumberProperty("End point Y coordinate (feet)"),
                ["endZ"] = CreateNumberProperty("End point Z coordinate (feet)", 0),
                ["height"] = CreateNumberProperty("Wall height (feet)", 0, 100),
                ["wallTypeName"] = CreateProperty("string",
                    "Wall type name (optional, uses first available if not specified)", null)
            },
            required: new[] { "startX", "startY", "endX", "endY" }
        );

        public CreateWallTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            // Get parameters
            double startX = GetRequiredParam<double>(arguments, "startX");
            double startY = GetRequiredParam<double>(arguments, "startY");
            double startZ = GetOptionalParam<double>(arguments, "startZ", 0);
            double endX = GetRequiredParam<double>(arguments, "endX");
            double endY = GetRequiredParam<double>(arguments, "endY");
            double endZ = GetOptionalParam<double>(arguments, "endZ", 0);
            double height = GetOptionalParam<double>(arguments, "height", 10.0);
            string wallTypeName = GetOptionalParam<string>(arguments, "wallTypeName", null);

            // Create wall
            var result = await _context.ExecuteWithTransactionAsync("Create Wall", doc =>
            {
                // Create line for wall location
                XYZ start = new XYZ(startX, startY, startZ);
                XYZ end = new XYZ(endX, endY, endZ);
                Line wallLine = GeometryHelper.CreateLine(start, end);

                // Get wall type
                WallType wallType = null;
                if (!string.IsNullOrEmpty(wallTypeName))
                {
                    // Find wall type by name
                    wallType = new FilteredElementCollector(doc)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .FirstOrDefault(wt => wt.Name.Equals(wallTypeName,
                            System.StringComparison.OrdinalIgnoreCase));

                    if (wallType == null)
                    {
                        throw new System.Exception($"Wall type '{wallTypeName}' not found");
                    }
                }
                else
                {
                    // Use first available wall type
                    wallType = new FilteredElementCollector(doc)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .FirstOrDefault();

                    if (wallType == null)
                    {
                        throw new System.Exception("No wall types found in document");
                    }
                }

                // Get level (use level 0 or first level)
                Level level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .FirstOrDefault();

                if (level == null)
                {
                    throw new System.Exception("No levels found in document");
                }

                // Create wall
                Wall wall = Wall.Create(doc, wallLine, wallType.Id, level.Id, height, 0, false, false);

                if (wall == null)
                {
                    throw new System.Exception("Failed to create wall");
                }

                // Format result
                string resultMsg = $"Wall created successfully!\n";
                resultMsg += $"Element ID: {wall.Id.IntegerValue}\n";
                resultMsg += $"Wall Type: {wallType.Name}\n";
                resultMsg += $"Level: {level.Name}\n";
                resultMsg += $"Start: {GeometryHelper.FormatPoint(start)}\n";
                resultMsg += $"End: {GeometryHelper.FormatPoint(end)}\n";
                resultMsg += $"Height: {height:F2} ft\n";
                resultMsg += $"Length: {wallLine.Length:F2} ft";

                return resultMsg;
            });

            return CreateTextResult(result);
        }
    }
}
