using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using Autodesk.Revit.DB;

namespace RevitClaudeMCP.Tools.Document
{
    /// <summary>
    /// Get information about the active view
    /// </summary>
    public class GetActiveViewTool : ToolBase
    {
        public override string Name => "get_active_view";

        public override string Description =>
            "Get information about the currently active view in Revit.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "No parameters required",
            new JObject { },
            required: new string[] { }
        );

        public GetActiveViewTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            var viewInfo = await _context.ExecuteAsync(uiApp =>
            {
                var activeView = uiApp.ActiveUIDocument.ActiveView;

                if (activeView == null)
                    return "No active view";

                var info = $"Active View Information:\n";
                info += $"Name: {activeView.Name}\n";
                info += $"View Type: {activeView.ViewType}\n";
                info += $"View ID: {activeView.Id.IntegerValue}\n";
                info += $"Scale: 1:{activeView.Scale}\n";
                info += $"Detail Level: {activeView.DetailLevel}\n";
                info += $"Is Template: {activeView.IsTemplate}\n";

                if (activeView is ViewPlan viewPlan)
                {
                    info += $"View Plan Type: {viewPlan.ViewType}\n";
                    info += $"Associated Level: {viewPlan.GenLevel?.Name ?? "None"}\n";
                }
                else if (activeView is View3D view3D)
                {
                    info += $"Is Perspective: {view3D.IsPerspective}\n";
                }

                return info;
            });

            return CreateTextResult(viewInfo);
        }
    }
}
