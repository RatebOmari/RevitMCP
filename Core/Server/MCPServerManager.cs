using System;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using RevitClaudeMCP.Tools.Document;
using RevitClaudeMCP.Tools.Creation;
using RevitClaudeMCP.Tools.Modification;
using RevitClaudeMCP.Tools.Query;
using RevitClaudeMCP.Tools.Scripting;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP.Core.Server
{
    /// <summary>
    /// Manages the MCP server lifecycle and tool registry
    /// </summary>
    public class MCPServerManager
    {
        private readonly RevitContextManager _contextManager;
        private ToolRegistry _toolRegistry;
        private NamedPipeMCPServer _mcpServer;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public bool IsClientConnected => _mcpServer?.IsClientConnected ?? false;
        public ToolRegistry ToolRegistry => _toolRegistry;

        public event EventHandler<string> OnLog;
        public event EventHandler<string> OnError;
        public event EventHandler OnServerStarted;
        public event EventHandler OnServerStopped;
        public event EventHandler OnClientConnected;
        public event EventHandler OnClientDisconnected;

        /// <summary>
        /// Constructor
        /// </summary>
        public MCPServerManager(RevitContextManager contextManager)
        {
            _contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
        }

        /// <summary>
        /// Start the MCP server
        /// </summary>
        public void StartServer()
        {
            if (_isRunning)
            {
                Log("Server already running");
                return;
            }

            try
            {
                Log("Starting MCP Server...");

                // Initialize tool registry
                InitializeToolRegistry();
                Log($"Tool registry initialized with {_toolRegistry.GetToolCount()} tools");

                // Create MCP server
                _mcpServer = new NamedPipeMCPServer(_toolRegistry);

                // Subscribe to events
                _mcpServer.OnLog += (s, msg) => Log(msg);
                _mcpServer.OnError += (s, msg) => LogError(msg);
                _mcpServer.OnClientConnected += (s, e) =>
                {
                    Log("Client connected to MCP server");
                    OnClientConnected?.Invoke(this, EventArgs.Empty);
                };
                _mcpServer.OnClientDisconnected += (s, e) =>
                {
                    Log("Client disconnected from MCP server");
                    OnClientDisconnected?.Invoke(this, EventArgs.Empty);
                };

                // Start server
                _mcpServer.Start();

                _isRunning = true;
                Log("MCP Server started successfully");

                OnServerStarted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogError($"Failed to start server: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Stop the MCP server
        /// </summary>
        public void StopServer()
        {
            if (!_isRunning)
            {
                return;
            }

            try
            {
                Log("Stopping MCP Server...");

                _mcpServer?.Stop();
                _mcpServer?.Dispose();
                _mcpServer = null;

                _isRunning = false;
                Log("MCP Server stopped");

                OnServerStopped?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogError($"Error stopping server: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Restart the MCP server
        /// </summary>
        public void RestartServer()
        {
            Log("Restarting MCP Server...");
            StopServer();
            System.Threading.Thread.Sleep(1000); // Brief pause
            StartServer();
        }

        /// <summary>
        /// Initialize and register all tools
        /// </summary>
        private void InitializeToolRegistry()
        {
            _toolRegistry = new ToolRegistry(_contextManager);

            // Register document tools
            _toolRegistry.RegisterTool(new DocumentInfoTool(_contextManager));
            _toolRegistry.RegisterTool(new GetActiveViewTool(_contextManager));
            _toolRegistry.RegisterTool(new ListViewsTool(_contextManager));

            // Register creation tools
            _toolRegistry.RegisterTool(new CreateWallTool(_contextManager));
            _toolRegistry.RegisterTool(new CreateFloorTool(_contextManager));
            _toolRegistry.RegisterTool(new CreateColumnTool(_contextManager));

            // Register modification tools
            _toolRegistry.RegisterTool(new ModifyParameterTool(_contextManager));
            _toolRegistry.RegisterTool(new MoveElementTool(_contextManager));
            _toolRegistry.RegisterTool(new DeleteElementTool(_contextManager));

            // Register query tools
            _toolRegistry.RegisterTool(new GetElementsTool(_contextManager));
            _toolRegistry.RegisterTool(new GetParameterTool(_contextManager));
            _toolRegistry.RegisterTool(new CountElementsTool(_contextManager));

            // Register scripting tools
            _toolRegistry.RegisterTool(new ExecuteScriptTool(_contextManager));

            Log($"Registered {_toolRegistry.GetToolCount()} tools");
        }

        /// <summary>
        /// Get server status information
        /// </summary>
        public ServerStatus GetStatus()
        {
            return new ServerStatus
            {
                IsRunning = _isRunning,
                IsClientConnected = IsClientConnected,
                ToolCount = _toolRegistry?.GetToolCount() ?? 0,
                PipeName = "\\\\.\\pipe\\revit-claude-mcp"
            };
        }

        /// <summary>
        /// Log message
        /// </summary>
        private void Log(string message)
        {
            Logger.Log($"[Server Manager] {message}");
            OnLog?.Invoke(this, message);
        }

        /// <summary>
        /// Log error
        /// </summary>
        private void LogError(string message, Exception ex = null)
        {
            Logger.LogError($"[Server Manager] {message}", ex);
            OnError?.Invoke(this, message);
        }
    }

    /// <summary>
    /// Server status information
    /// </summary>
    public class ServerStatus
    {
        public bool IsRunning { get; set; }
        public bool IsClientConnected { get; set; }
        public int ToolCount { get; set; }
        public string PipeName { get; set; }
    }
}
