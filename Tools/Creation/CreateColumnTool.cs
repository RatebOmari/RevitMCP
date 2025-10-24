using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using RevitClaudeMCP.Utils;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitClaudeMCP.Tools.Creation
{
    /// <summary>
    /// Create a structural column
    /// </summary>
    public class CreateColumnTool : ToolBase
    {
        public override string Name => "create_column";

        public override string Description =>
            "Create a structural column at specified location.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for creating a column",
            new JObject
            {
                ["x"] = CreateNumberProperty("Column X coordinate (feet)"),
                ["y"] = CreateNumberProperty("Column Y coordinate (feet)"),
                ["z"] = CreateNumberProperty("Column base Z coordinate (feet)", 0),
                ["height"] = CreateNumberProperty("Column height (feet)", 0, 100),
                ["columnTypeName"] = CreateProperty("string",
                    "Column type name (optional, uses first available if not specified)", null)
            },
            required: new[] { "x", "y" }
        );

        public CreateColumnTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            double x = GetRequiredParam<double>(arguments, "x");
            double y = GetRequiredParam<double>(arguments, "y");
            double z = GetOptionalParam<double>(arguments, "z", 0);
            double height = GetOptionalParam<double>(arguments, "height", 10.0);
            string columnTypeName = GetOptionalParam<string>(arguments, "columnTypeName", null);

            var result = await _context.ExecuteWithTransactionAsync("Create Column", doc =>
            {
                // Get column type (family symbol)
                FamilySymbol columnType = null;
                if (!string.IsNullOrEmpty(columnTypeName))
                {
                    columnType = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(BuiltInCategory.OST_StructuralColumns)
                        .Cast<FamilySymbol>()
                        .FirstOrDefault(fs => fs.Name.Equals(columnTypeName,
                            System.StringComparison.OrdinalIgnoreCase));

                    if (columnType == null)
                    {
                        throw new System.Exception($"Column type '{columnTypeName}' not found");
                    }
                }
                else
                {
                    columnType = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(BuiltInCategory.OST_StructuralColumns)
                        .Cast<FamilySymbol>()
                        .FirstOrDefault();

                    if (columnType == null)
                    {
                        throw new System.Exception("No column types found in document");
                    }
                }

                // Activate the column type if not already active
                if (!columnType.IsActive)
                {
                    columnType.Activate();
                }

                // Get base level
                Level baseLevel = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .Where(l => l.Elevation <= z)
                    .OrderByDescending(l => l.Elevation)
                    .FirstOrDefault();

                if (baseLevel == null)
                {
                    baseLevel = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .OrderBy(l => l.Elevation)
                        .FirstOrDefault();
                }

                if (baseLevel == null)
                {
                    throw new System.Exception("No levels found in document");
                }

                // Get top level or use base level
                Level topLevel = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .Where(l => l.Elevation > baseLevel.Elevation)
                    .OrderBy(l => l.Elevation)
                    .FirstOrDefault() ?? baseLevel;

                // Create column location
                XYZ location = new XYZ(x, y, z);

                // Create column
                FamilyInstance column = doc.Create.NewFamilyInstance(
                    location,
                    columnType,
                    baseLevel,
                    StructuralType.Column
                );

                if (column == null)
                {
                    throw new System.Exception("Failed to create column");
                }

                // Set top level and offset
                Parameter topLevelParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                if (topLevelParam != null)
                {
                    topLevelParam.Set(topLevel.Id);
                }

                Parameter topOffsetParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
                if (topOffsetParam != null)
                {
                    double offset = height - (topLevel.Elevation - baseLevel.Elevation);
                    topOffsetParam.Set(offset);
                }

                string resultMsg = $"Column created successfully!\n";
                resultMsg += $"Element ID: {column.Id.IntegerValue}\n";
                resultMsg += $"Column Type: {columnType.Name}\n";
                resultMsg += $"Base Level: {baseLevel.Name}\n";
                resultMsg += $"Top Level: {topLevel.Name}\n";
                resultMsg += $"Location: {GeometryHelper.FormatPoint(location)}\n";
                resultMsg += $"Height: {height:F2} ft";

                return resultMsg;
            });

            return CreateTextResult(result);
        }
    }
}
