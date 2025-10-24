using System.Linq;
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
    /// Get parameter value from an element
    /// </summary>
    public class GetParameterTool : ToolBase
    {
        public override string Name => "get_parameter";

        public override string Description =>
            "Get a parameter value from an element by element ID and parameter name.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for getting element parameter",
            new JObject
            {
                ["elementId"] = CreateNumberProperty("Element ID"),
                ["parameterName"] = CreateProperty("string", "Parameter name (leave empty to get all parameters)")
            },
            required: new[] { "elementId" }
        );

        public GetParameterTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            int elementId = GetRequiredParam<int>(arguments, "elementId");
            string parameterName = GetOptionalParam<string>(arguments, "parameterName", null);

            var result = await _context.ExecuteReadOnlyAsync(doc =>
            {
                Element element = ElementHelper.GetElementById(doc, elementId);
                if (element == null)
                {
                    throw new System.Exception($"Element with ID {elementId} not found");
                }

                string resultMsg = $"Element ID: {elementId}\n";
                resultMsg += $"Element: {element.Name}\n";
                resultMsg += $"Category: {ElementHelper.GetCategoryName(element)}\n\n";

                if (string.IsNullOrEmpty(parameterName))
                {
                    // Get all parameters
                    resultMsg += "=== All Parameters ===\n";
                    var parameters = ElementHelper.GetAllParameters(element);
                    foreach (var param in parameters.OrderBy(p => p.Definition?.Name))
                    {
                        string name = param.Definition?.Name ?? "Unknown";
                        string value = ElementHelper.GetParameterValueAsString(param);
                        string type = param.StorageType.ToString();
                        string readOnly = param.IsReadOnly ? " (Read-Only)" : "";

                        resultMsg += $"{name}: {value} [{type}]{readOnly}\n";
                    }
                }
                else
                {
                    // Get specific parameter
                    Parameter param = element.LookupParameter(parameterName);
                    if (param == null)
                    {
                        throw new System.Exception($"Parameter '{parameterName}' not found on element {elementId}");
                    }

                    resultMsg += $"Parameter: {parameterName}\n";
                    resultMsg += $"Value: {ElementHelper.GetParameterValueAsString(param)}\n";
                    resultMsg += $"Storage Type: {param.StorageType}\n";
                    resultMsg += $"Read-Only: {param.IsReadOnly}\n";
                    resultMsg += $"Has Value: {param.HasValue}";
                }

                return resultMsg;
            });

            return CreateTextResult(result);
        }
    }
}
