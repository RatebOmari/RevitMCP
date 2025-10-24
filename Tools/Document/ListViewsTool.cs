using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using Autodesk.Revit.DB;

namespace RevitClaudeMCP.Tools.Document
{
    /// <summary>
    /// List all views in the document
    /// </summary>
    public class ListViewsTool : ToolBase
    {
        public override string Name => "list_views";

        public override string Description =>
            "List all views in the document, optionally filtered by view type.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for listing views",
            new JObject
            {
                ["viewType"] = CreateEnumProperty(
                    "Filter by view type (optional)",
                    "all", "floorplan", "3d", "section", "elevation", "schedule", "sheet"
                )
            },
            required: new string[] { }
        );

        public ListViewsTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            string viewType = GetOptionalParam(arguments, "viewType", "all").ToLower();

            var result = await _context.ExecuteReadOnlyAsync(doc =>
            {
                var collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate);

                // Filter by type if specified
                if (viewType != "all")
                {
                    collector = viewType switch
                    {
                        "floorplan" => collector.Where(v => v.ViewType == ViewType.FloorPlan),
                        "3d" => collector.Where(v => v.ViewType == ViewType.ThreeD),
                        "section" => collector.Where(v => v.ViewType == ViewType.Section),
                        "elevation" => collector.Where(v => v.ViewType == ViewType.Elevation),
                        "schedule" => collector.Where(v => v.ViewType == ViewType.Schedule),
                        "sheet" => collector.Where(v => v.ViewType == ViewType.DrawingSheet),
                        _ => collector
                    };
                }

                var views = collector.OrderBy(v => v.ViewType).ThenBy(v => v.Name).ToList();

                var sb = new StringBuilder();
                sb.AppendLine($"=== Views ({views.Count}) ===\n");

                var groupedByType = views.GroupBy(v => v.ViewType);
                foreach (var group in groupedByType)
                {
                    sb.AppendLine($"[{group.Key}]");
                    foreach (var view in group)
                    {
                        sb.AppendLine($"  • {view.Name} (ID: {view.Id.IntegerValue})");
                    }
                    sb.AppendLine();
                }

                return sb.ToString();
            });

            return CreateTextResult(result);
        }
    }
}
