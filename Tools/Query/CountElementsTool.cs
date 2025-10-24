using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using Autodesk.Revit.DB;

namespace RevitClaudeMCP.Tools.Query
{
    /// <summary>
    /// Count elements by category
    /// </summary>
    public class CountElementsTool : ToolBase
    {
        public override string Name => "count_elements";

        public override string Description =>
            "Count elements by category in the active document.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "No parameters required",
            new JObject { },
            required: new string[] { }
        );

        public CountElementsTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            var result = await _context.ExecuteReadOnlyAsync(doc =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== Element Count by Category ===\n");

                var categories = new[]
                {
                    (BuiltInCategory.OST_Walls, "Walls"),
                    (BuiltInCategory.OST_Floors, "Floors"),
                    (BuiltInCategory.OST_Ceilings, "Ceilings"),
                    (BuiltInCategory.OST_Roofs, "Roofs"),
                    (BuiltInCategory.OST_Doors, "Doors"),
                    (BuiltInCategory.OST_Windows, "Windows"),
                    (BuiltInCategory.OST_Rooms, "Rooms"),
                    (BuiltInCategory.OST_Stairs, "Stairs"),
                    (BuiltInCategory.OST_StructuralColumns, "Structural Columns"),
                    (BuiltInCategory.OST_StructuralFraming, "Structural Framing"),
                    (BuiltInCategory.OST_StructuralFoundation, "Structural Foundations"),
                    (BuiltInCategory.OST_Furniture, "Furniture"),
                    (BuiltInCategory.OST_LightingFixtures, "Lighting Fixtures"),
                    (BuiltInCategory.OST_PlumbingFixtures, "Plumbing Fixtures"),
                    (BuiltInCategory.OST_MechanicalEquipment, "Mechanical Equipment"),
                    (BuiltInCategory.OST_ElectricalFixtures, "Electrical Fixtures")
                };

                int totalCount = 0;

                foreach (var (category, name) in categories)
                {
                    int count = new FilteredElementCollector(doc)
                        .OfCategory(category)
                        .WhereElementIsNotElementType()
                        .GetElementCount();

                    if (count > 0)
                    {
                        sb.AppendLine($"{name}: {count}");
                        totalCount += count;
                    }
                }

                sb.AppendLine($"\nTotal Counted: {totalCount}");

                // Total elements in document
                int allElements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .GetElementCount();

                sb.AppendLine($"Total Elements in Document: {allElements}");

                return sb.ToString();
            });

            return CreateTextResult(result);
        }
    }
}
