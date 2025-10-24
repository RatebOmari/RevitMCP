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
    /// Get elements by category
    /// </summary>
    public class GetElementsTool : ToolBase
    {
        public override string Name => "get_elements";

        public override string Description =>
            "Get all elements of a specified category.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for querying elements",
            new JObject
            {
                ["category"] = CreateEnumProperty(
                    "Element category",
                    "Walls", "Floors", "Doors", "Windows", "Rooms", "Columns", "Beams"
                ),
                ["limit"] = CreateNumberProperty("Maximum number of results (default: 100)", 1, 1000)
            },
            required: new[] { "category" }
        );

        public GetElementsTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            string category = GetRequiredParam<string>(arguments, "category");
            int limit = GetOptionalParam<int>(arguments, "limit", 100);

            var result = await _context.ExecuteReadOnlyAsync(doc =>
            {
                // Map category name to BuiltInCategory
                BuiltInCategory builtInCategory = category.ToLower() switch
                {
                    "walls" => BuiltInCategory.OST_Walls,
                    "floors" => BuiltInCategory.OST_Floors,
                    "doors" => BuiltInCategory.OST_Doors,
                    "windows" => BuiltInCategory.OST_Windows,
                    "rooms" => BuiltInCategory.OST_Rooms,
                    "columns" => BuiltInCategory.OST_StructuralColumns,
                    "beams" => BuiltInCategory.OST_StructuralFraming,
                    _ => throw new System.Exception($"Unknown category: {category}")
                };

                // Get elements
                var elements = new FilteredElementCollector(doc)
                    .OfCategory(builtInCategory)
                    .WhereElementIsNotElementType()
                    .Take(limit)
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine($"=== {category} ({elements.Count} of {limit} max) ===\n");

                foreach (var element in elements)
                {
                    sb.AppendLine($"ID: {element.Id.IntegerValue}");
                    sb.AppendLine($"  Name: {element.Name}");
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
