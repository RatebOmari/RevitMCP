using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP.Tools.Base
{
    /// <summary>
    /// Registry for managing all available MCP tools
    /// </summary>
    public class ToolRegistry
    {
        private readonly Dictionary<string, ITool> _tools;
        private readonly RevitContextManager _context;

        public ToolRegistry(RevitContextManager context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Register a tool
        /// </summary>
        public void RegisterTool(ITool tool)
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            if (string.IsNullOrWhiteSpace(tool.Name))
                throw new ArgumentException("Tool name cannot be empty");

            if (_tools.ContainsKey(tool.Name))
            {
                Logger.LogWarning($"Tool '{tool.Name}' is already registered. Replacing with new instance.");
                _tools[tool.Name] = tool;
            }
            else
            {
                _tools.Add(tool.Name, tool);
                Logger.Log($"Registered tool: {tool.Name}");
            }
        }

        /// <summary>
        /// Unregister a tool
        /// </summary>
        public bool UnregisterTool(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name cannot be empty");

            if (_tools.Remove(toolName))
            {
                Logger.Log($"Unregistered tool: {toolName}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get tool by name
        /// </summary>
        public ITool GetTool(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name cannot be empty");

            if (_tools.TryGetValue(toolName, out ITool tool))
            {
                return tool;
            }

            throw new ToolNotFoundException(toolName);
        }

        /// <summary>
        /// Check if tool exists
        /// </summary>
        public bool HasTool(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                return false;

            return _tools.ContainsKey(toolName);
        }

        /// <summary>
        /// Get all registered tools
        /// </summary>
        public IReadOnlyDictionary<string, ITool> GetAllTools()
        {
            return _tools;
        }

        /// <summary>
        /// Get tool count
        /// </summary>
        public int GetToolCount()
        {
            return _tools.Count;
        }

        /// <summary>
        /// Get all tool definitions (for tools/list response)
        /// </summary>
        public List<ToolDefinition> GetAllToolDefinitions()
        {
            return _tools.Values.Select(tool => new ToolDefinition
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.InputSchema
            }).ToList();
        }

        /// <summary>
        /// Execute a tool by name
        /// </summary>
        public async Task<ToolsCallResult> ExecuteTool(string toolName, JObject arguments)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name cannot be empty");

            // Get tool
            ITool tool = GetTool(toolName);

            // Execute
            Logger.Log($"Executing tool: {toolName}");

            try
            {
                var result = await tool.ExecuteAsync(arguments);
                Logger.Log($"Tool execution completed: {toolName}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Tool execution failed: {toolName} - {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get tool names as list
        /// </summary>
        public List<string> GetToolNames()
        {
            return _tools.Keys.ToList();
        }

        /// <summary>
        /// Clear all tools
        /// </summary>
        public void Clear()
        {
            _tools.Clear();
            Logger.Log("Tool registry cleared");
        }
    }
}
