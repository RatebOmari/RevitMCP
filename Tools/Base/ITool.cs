using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;

namespace RevitClaudeMCP.Tools.Base
{
    /// <summary>
    /// Interface for MCP tools
    /// </summary>
    public interface ITool
    {
        /// <summary>
        /// Tool name (must be unique)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Tool description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// JSON schema for tool parameters
        /// </summary>
        JObject InputSchema { get; }

        /// <summary>
        /// Execute the tool
        /// </summary>
        /// <param name="arguments">Tool arguments</param>
        /// <returns>Tool execution result</returns>
        Task<ToolsCallResult> ExecuteAsync(JObject arguments);
    }
}
