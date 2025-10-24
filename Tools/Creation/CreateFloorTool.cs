using System.Collections.Generic;
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
    /// Create a rectangular floor
    /// </summary>
    public class CreateFloorTool : ToolBase
    {
        public override string Name => "create_floor";

        public override string Description =>
            "Create a rectangular floor at specified coordinates and size.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for creating a floor",
            new JObject
            {
                ["x"] = CreateNumberProperty("Floor origin X coordinate (feet)"),
                ["y"] = CreateNumberProperty("Floor origin Y coordinate (feet)"),
                ["z"] = CreateNumberProperty("Floor level Z coordinate (feet)", 0),
                ["width"] = CreateNumberProperty("Floor width (feet)", 0),
                ["length"] = CreateNumberProperty("Floor length (feet)", 0),
                ["floorTypeName"] = CreateProperty("string",
                    "Floor type name (optional, uses first available if not specified)", null)
            },
            required: new[] { "x", "y", "width", "length" }
        );

        public CreateFloorTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            double x = GetRequiredParam<double>(arguments, "x");
            double y = GetRequiredParam<double>(arguments, "y");
            double z = GetOptionalParam<double>(arguments, "z", 0);
            double width = GetRequiredParam<double>(arguments, "width");
            double length = GetRequiredParam<double>(arguments, "length");
            string floorTypeName = GetOptionalParam<string>(arguments, "floorTypeName", null);

            var result = await _context.ExecuteWithTransactionAsync("Create Floor", doc =>
            {
                // Get floor type
                FloorType floorType = null;
                if (!string.IsNullOrEmpty(floorTypeName))
                {
                    floorType = new FilteredElementCollector(doc)
                        .OfClass(typeof(FloorType))
                        .Cast<FloorType>()
                        .FirstOrDefault(ft => ft.Name.Equals(floorTypeName,
                            System.StringComparison.OrdinalIgnoreCase));

                    if (floorType == null)
                    {
                        throw new System.Exception($"Floor type '{floorTypeName}' not found");
                    }
                }
                else
                {
                    floorType = new FilteredElementCollector(doc)
                        .OfClass(typeof(FloorType))
                        .Cast<FloorType>()
                        .FirstOrDefault();

                    if (floorType == null)
                    {
                        throw new System.Exception("No floor types found in document");
                    }
                }

                // Get level
                Level level = new FilteredElementCollector(doc)
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

                // Create rectangular profile
                var profile = new List<CurveLoop>();
                var curveLoop = new CurveLoop();

                XYZ p1 = new XYZ(x, y, z);
                XYZ p2 = new XYZ(x + width, y, z);
                XYZ p3 = new XYZ(x + width, y + length, z);
                XYZ p4 = new XYZ(x, y + length, z);

                curveLoop.Append(Line.CreateBound(p1, p2));
                curveLoop.Append(Line.CreateBound(p2, p3));
                curveLoop.Append(Line.CreateBound(p3, p4));
                curveLoop.Append(Line.CreateBound(p4, p1));

                profile.Add(curveLoop);

                // Create floor
                Floor floor = Floor.Create(doc, profile, floorType.Id, level.Id);

                if (floor == null)
                {
                    throw new System.Exception("Failed to create floor");
                }

                string resultMsg = $"Floor created successfully!\n";
                resultMsg += $"Element ID: {floor.Id.IntegerValue}\n";
                resultMsg += $"Floor Type: {floorType.Name}\n";
                resultMsg += $"Level: {level.Name}\n";
                resultMsg += $"Origin: {GeometryHelper.FormatPoint(p1)}\n";
                resultMsg += $"Width: {width:F2} ft\n";
                resultMsg += $"Length: {length:F2} ft\n";
                resultMsg += $"Area: {(width * length):F2} sq ft";

                return resultMsg;
            });

            return CreateTextResult(result);
        }
    }
}
