using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace RevitClaudeMCP.Core.MCPServer
{
    /// <summary>
    /// Represents a JSON-RPC 2.0 request message
    /// </summary>
    public class MCPRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JObject Params { get; set; }

        /// <summary>
        /// Check if this is a notification (no response expected)
        /// </summary>
        public bool IsNotification => Id == null;
    }

    /// <summary>
    /// Represents a JSON-RPC 2.0 response message
    /// </summary>
    public class MCPResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public MCPError Error { get; set; }

        /// <summary>
        /// Create success response
        /// </summary>
        public static MCPResponse Success(object id, object result)
        {
            return new MCPResponse
            {
                Id = id,
                Result = result
            };
        }

        /// <summary>
        /// Create error response
        /// </summary>
        public static MCPResponse ErrorResponse(object id, int code, string message, object data = null)
        {
            return new MCPResponse
            {
                Id = id,
                Error = new MCPError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };
        }
    }

    /// <summary>
    /// Represents a JSON-RPC 2.0 error object
    /// </summary>
    public class MCPError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }
    }

    /// <summary>
    /// Standard JSON-RPC error codes
    /// </summary>
    public static class MCPErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;

        // Custom error codes (application-specific)
        public const int ToolNotFound = -32001;
        public const int ToolExecutionFailed = -32002;
        public const int TransactionFailed = -32003;
        public const int ScriptValidationFailed = -32004;
        public const int ScriptCompilationFailed = -32005;
        public const int ScriptRejected = -32006;
    }

    /// <summary>
    /// MCP Initialize request parameters
    /// </summary>
    public class InitializeParams
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("capabilities")]
        public ClientCapabilities Capabilities { get; set; }

        [JsonProperty("clientInfo")]
        public ClientInfo ClientInfo { get; set; }
    }

    /// <summary>
    /// Client capabilities
    /// </summary>
    public class ClientCapabilities
    {
        [JsonProperty("roots", NullValueHandling = NullValueHandling.Ignore)]
        public RootsCapability Roots { get; set; }

        [JsonProperty("sampling", NullValueHandling = NullValueHandling.Ignore)]
        public object Sampling { get; set; }

        [JsonProperty("experimental", NullValueHandling = NullValueHandling.Ignore)]
        public object Experimental { get; set; }
    }

    /// <summary>
    /// Roots capability
    /// </summary>
    public class RootsCapability
    {
        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    /// <summary>
    /// Client information
    /// </summary>
    public class ClientInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// MCP Initialize result
    /// </summary>
    public class InitializeResult
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2024-11-05";

        [JsonProperty("capabilities")]
        public ServerCapabilities Capabilities { get; set; }

        [JsonProperty("serverInfo")]
        public ServerInfo ServerInfo { get; set; }
    }

    /// <summary>
    /// Server capabilities
    /// </summary>
    public class ServerCapabilities
    {
        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public ToolsCapability Tools { get; set; }

        [JsonProperty("prompts", NullValueHandling = NullValueHandling.Ignore)]
        public object Prompts { get; set; }

        [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
        public object Resources { get; set; }

        [JsonProperty("logging", NullValueHandling = NullValueHandling.Ignore)]
        public object Logging { get; set; }

        [JsonProperty("experimental", NullValueHandling = NullValueHandling.Ignore)]
        public object Experimental { get; set; }
    }

    /// <summary>
    /// Tools capability
    /// </summary>
    public class ToolsCapability
    {
        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    /// <summary>
    /// Server information
    /// </summary>
    public class ServerInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// Tools list result
    /// </summary>
    public class ToolsListResult
    {
        [JsonProperty("tools")]
        public List<ToolDefinition> Tools { get; set; }
    }

    /// <summary>
    /// Tool definition
    /// </summary>
    public class ToolDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("inputSchema")]
        public JObject InputSchema { get; set; }
    }

    /// <summary>
    /// Tools call params
    /// </summary>
    public class ToolsCallParams
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments")]
        public JObject Arguments { get; set; }
    }

    /// <summary>
    /// Tools call result
    /// </summary>
    public class ToolsCallResult
    {
        [JsonProperty("content")]
        public List<ContentItem> Content { get; set; }

        [JsonProperty("isError", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsError { get; set; }
    }

    /// <summary>
    /// Content item (text, image, resource)
    /// </summary>
    public class ContentItem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public string Data { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }

        /// <summary>
        /// Create text content item
        /// </summary>
        public static ContentItem Text(string text)
        {
            return new ContentItem
            {
                Type = "text",
                Text = text
            };
        }

        /// <summary>
        /// Create image content item
        /// </summary>
        public static ContentItem Image(string base64Data, string mimeType = "image/png")
        {
            return new ContentItem
            {
                Type = "image",
                Data = base64Data,
                MimeType = mimeType
            };
        }
    }
}
