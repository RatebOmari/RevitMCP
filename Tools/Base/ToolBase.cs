using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP.Tools.Base
{
    /// <summary>
    /// Base class for MCP tools - provides common functionality
    /// </summary>
    public abstract class ToolBase : ITool
    {
        protected readonly RevitContextManager _context;

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract JObject InputSchema { get; }

        protected ToolBase(RevitContextManager context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Execute the tool (implements error handling and logging)
        /// </summary>
        public async Task<ToolsCallResult> ExecuteAsync(JObject arguments)
        {
            try
            {
                Logger.Log($"[Tool: {Name}] Executing...");

                // Validate arguments
                ValidateArguments(arguments);

                // Execute tool implementation
                var result = await ExecuteToolAsync(arguments);

                Logger.Log($"[Tool: {Name}] Completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Tool: {Name}] Error: {ex.Message}", ex);
                return CreateErrorResult($"Tool execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Tool-specific implementation (override in derived classes)
        /// </summary>
        protected abstract Task<ToolsCallResult> ExecuteToolAsync(JObject arguments);

        /// <summary>
        /// Validate tool arguments (override for custom validation)
        /// </summary>
        protected virtual void ValidateArguments(JObject arguments)
        {
            // Default: no validation
            // Override in derived classes for specific validation
        }

        /// <summary>
        /// Create success result with text content
        /// </summary>
        protected ToolsCallResult CreateTextResult(string text)
        {
            return new ToolsCallResult
            {
                Content = new List<ContentItem>
                {
                    ContentItem.Text(text)
                }
            };
        }

        /// <summary>
        /// Create error result
        /// </summary>
        protected ToolsCallResult CreateErrorResult(string message)
        {
            return new ToolsCallResult
            {
                Content = new List<ContentItem>
                {
                    ContentItem.Text($"Error: {message}")
                },
                IsError = true
            };
        }

        /// <summary>
        /// Get required parameter
        /// </summary>
        protected T GetRequiredParam<T>(JObject arguments, string paramName)
        {
            if (arguments == null || !arguments.ContainsKey(paramName))
            {
                throw new ArgumentException($"Missing required parameter: {paramName}");
            }

            try
            {
                return arguments[paramName].ToObject<T>();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid parameter '{paramName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Get optional parameter with default value
        /// </summary>
        protected T GetOptionalParam<T>(JObject arguments, string paramName, T defaultValue = default(T))
        {
            if (arguments == null || !arguments.ContainsKey(paramName))
            {
                return defaultValue;
            }

            try
            {
                return arguments[paramName].ToObject<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Create JSON schema for parameters
        /// </summary>
        protected static JObject CreateSchema(string type, string description, JObject properties, string[] required = null)
        {
            var schema = new JObject
            {
                ["type"] = type,
                ["description"] = description,
                ["properties"] = properties
            };

            if (required != null && required.Length > 0)
            {
                schema["required"] = JArray.FromObject(required);
            }

            return schema;
        }

        /// <summary>
        /// Create property definition for schema
        /// </summary>
        protected static JObject CreateProperty(string type, string description, object defaultValue = null)
        {
            var prop = new JObject
            {
                ["type"] = type,
                ["description"] = description
            };

            if (defaultValue != null)
            {
                prop["default"] = JToken.FromObject(defaultValue);
            }

            return prop;
        }

        /// <summary>
        /// Create enum property for schema
        /// </summary>
        protected static JObject CreateEnumProperty(string description, params string[] values)
        {
            return new JObject
            {
                ["type"] = "string",
                ["description"] = description,
                ["enum"] = JArray.FromObject(values)
            };
        }

        /// <summary>
        /// Create number property with range
        /// </summary>
        protected static JObject CreateNumberProperty(string description, double? minimum = null, double? maximum = null)
        {
            var prop = new JObject
            {
                ["type"] = "number",
                ["description"] = description
            };

            if (minimum.HasValue)
                prop["minimum"] = minimum.Value;

            if (maximum.HasValue)
                prop["maximum"] = maximum.Value;

            return prop;
        }
    }
}
