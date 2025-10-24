using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using RevitClaudeMCP.Utils;
using Autodesk.Revit.DB;

namespace RevitClaudeMCP.Tools.Modification
{
    /// <summary>
    /// Move an element by translation vector
    /// </summary>
    public class MoveElementTool : ToolBase
    {
        public override string Name => "move_element";

        public override string Description =>
            "Move an element by a translation vector.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for moving an element",
            new JObject
            {
                ["elementId"] = CreateNumberProperty("Element ID to move"),
                ["dx"] = CreateNumberProperty("Translation in X direction (feet)"),
                ["dy"] = CreateNumberProperty("Translation in Y direction (feet)"),
                ["dz"] = CreateNumberProperty("Translation in Z direction (feet)", 0)
            },
            required: new[] { "elementId", "dx", "dy" }
        );

        public MoveElementTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            int elementId = GetRequiredParam<int>(arguments, "elementId");
            double dx = GetRequiredParam<double>(arguments, "dx");
            double dy = GetRequiredParam<double>(arguments, "dy");
            double dz = GetOptionalParam<double>(arguments, "dz", 0);

            var result = await _context.ExecuteWithTransactionAsync("Move Element", doc =>
            {
                Element element = ElementHelper.GetElementById(doc, elementId);
                if (element == null)
                {
                    throw new System.Exception($"Element with ID {elementId} not found");
                }

                XYZ translation = new XYZ(dx, dy, dz);
                ElementTransformUtils.MoveElement(doc, element.Id, translation);

                string resultMsg = $"Element moved successfully!\n";
                resultMsg += $"Element ID: {elementId}\n";
                resultMsg += $"Element: {element.Name}\n";
                resultMsg += $"Translation: {GeometryHelper.FormatPoint(translation)}";

                return resultMsg;
            });

            return CreateTextResult(result);
        }
    }
}
