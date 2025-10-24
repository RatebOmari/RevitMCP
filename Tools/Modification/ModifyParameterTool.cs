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
    /// Modify a parameter value of an element
    /// </summary>
    public class ModifyParameterTool : ToolBase
    {
        public override string Name => "modify_parameter";

        public override string Description =>
            "Modify a parameter value of an element by element ID.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for modifying element parameter",
            new JObject
            {
                ["elementId"] = CreateNumberProperty("Element ID"),
                ["parameterName"] = CreateProperty("string", "Parameter name"),
                ["value"] = CreateProperty("string", "New parameter value (will be converted to appropriate type)")
            },
            required: new[] { "elementId", "parameterName", "value" }
        );

        public ModifyParameterTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            int elementId = GetRequiredParam<int>(arguments, "elementId");
            string parameterName = GetRequiredParam<string>(arguments, "parameterName");
            string value = GetRequiredParam<string>(arguments, "value");

            var result = await _context.ExecuteWithTransactionAsync("Modify Parameter", doc =>
            {
                Element element = ElementHelper.GetElementById(doc, elementId);
                if (element == null)
                {
                    throw new System.Exception($"Element with ID {elementId} not found");
                }

                Parameter param = element.LookupParameter(parameterName);
                if (param == null)
                {
                    throw new System.Exception($"Parameter '{parameterName}' not found on element {elementId}");
                }

                if (param.IsReadOnly)
                {
                    throw new System.Exception($"Parameter '{parameterName}' is read-only");
                }

                string oldValue = ElementHelper.GetParameterValueAsString(param);

                // Set value based on storage type
                bool success = ElementHelper.SetParameterValue(param, value);

                if (!success)
                {
                    throw new System.Exception($"Failed to set parameter value");
                }

                string newValue = ElementHelper.GetParameterValueAsString(param);

                string resultMsg = $"Parameter modified successfully!\n";
                resultMsg += $"Element ID: {elementId}\n";
                resultMsg += $"Element: {element.Name}\n";
                resultMsg += $"Parameter: {parameterName}\n";
                resultMsg += $"Old Value: {oldValue}\n";
                resultMsg += $"New Value: {newValue}";

                return resultMsg;
            });

            return CreateTextResult(result);
        }
    }
}
