using System.Linq;
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
    /// Delete an element from the document
    /// </summary>
    public class DeleteElementTool : ToolBase
    {
        public override string Name => "delete_element";

        public override string Description =>
            "Delete an element from the document by its ID.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for deleting an element",
            new JObject
            {
                ["elementId"] = CreateNumberProperty("Element ID to delete")
            },
            required: new[] { "elementId" }
        );

        public DeleteElementTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            int elementId = GetRequiredParam<int>(arguments, "elementId");

            var result = await _context.ExecuteWithTransactionAsync("Delete Element", doc =>
            {
                Element element = ElementHelper.GetElementById(doc, elementId);
                if (element == null)
                {
                    throw new System.Exception($"Element with ID {elementId} not found");
                }

                string elementName = element.Name;
                string category = ElementHelper.GetCategoryName(element);

                // Delete element (returns collection of deleted element IDs)
                var deletedIds = doc.Delete(element.Id);

                string resultMsg = $"Element deleted successfully!\n";
                resultMsg += $"Deleted Element ID: {elementId}\n";
                resultMsg += $"Element Name: {elementName}\n";
                resultMsg += $"Category: {category}\n";
                resultMsg += $"Total items deleted: {deletedIds.Count} (including dependent elements)";

                return resultMsg;
            });

            return CreateTextResult(result);
        }
    }
}
