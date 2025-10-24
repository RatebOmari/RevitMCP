using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using Autodesk.Revit.DB;
using System.Text;

namespace RevitClaudeMCP.Tools.Document
{
    /// <summary>
    /// Get information about the active Revit document
    /// </summary>
    public class DocumentInfoTool : ToolBase
    {
        public override string Name => "get_document_info";

        public override string Description =>
            "Get information about the active Revit document including title, path, and basic statistics.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for getting document information",
            new JObject
            {
                ["includeStats"] = CreateProperty("boolean",
                    "Include element count statistics", true)
            },
            required: new string[] { }
        );

        public DocumentInfoTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            bool includeStats = GetOptionalParam(arguments, "includeStats", true);

            var info = await _context.ExecuteReadOnlyAsync(doc =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== Document Information ===");
                sb.AppendLine($"Title: {doc.Title}");
                sb.AppendLine($"Path: {(doc.IsWorkshared ? doc.GetWorksharingCentralModelPath().ToString() : doc.PathName)}");
                sb.AppendLine($"Is Family: {doc.IsFamilyDocument}");
                sb.AppendLine($"Is Workshared: {doc.IsWorkshared}");
                sb.AppendLine($"Is Modified: {doc.IsModified}");

                // Project information
                ProjectInfo projInfo = doc.ProjectInformation;
                if (projInfo != null)
                {
                    sb.AppendLine("\n=== Project Information ===");
                    sb.AppendLine($"Name: {projInfo.Name}");
                    sb.AppendLine($"Number: {projInfo.Number}");
                    sb.AppendLine($"Address: {projInfo.Address}");
                    sb.AppendLine($"Author: {projInfo.Author}");
                    sb.AppendLine($"Client: {projInfo.ClientName}");
                }

                if (includeStats)
                {
                    sb.AppendLine("\n=== Element Statistics ===");

                    // Count elements by category
                    var categories = new[]
                    {
                        (BuiltInCategory.OST_Walls, "Walls"),
                        (BuiltInCategory.OST_Floors, "Floors"),
                        (BuiltInCategory.OST_Doors, "Doors"),
                        (BuiltInCategory.OST_Windows, "Windows"),
                        (BuiltInCategory.OST_Rooms, "Rooms"),
                        (BuiltInCategory.OST_StructuralColumns, "Structural Columns"),
                        (BuiltInCategory.OST_StructuralFraming, "Structural Framing")
                    };

                    foreach (var (category, name) in categories)
                    {
                        int count = new FilteredElementCollector(doc)
                            .OfCategory(category)
                            .WhereElementIsNotElementType()
                            .GetElementCount();

                        sb.AppendLine($"{name}: {count}");
                    }

                    // Total element count
                    int totalElements = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .GetElementCount();

                    sb.AppendLine($"\nTotal Elements: {totalElements}");
                }

                return sb.ToString();
            });

            return CreateTextResult(info);
        }
    }
}
