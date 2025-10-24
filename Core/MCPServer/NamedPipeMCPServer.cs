using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Utils;
using RevitClaudeMCP.Tools.Base;

namespace RevitClaudeMCP.Core.MCPServer
{
    /// <summary>
    /// MCP Server implementation using Named Pipes for communication
    /// </summary>
    public class NamedPipeMCPServer : IDisposable
    {
        private const string PIPE_NAME = "revit-claude-mcp";
        private NamedPipeServerStream _pipeServer;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _serverTask;
        private bool _isRunning;
        private bool _isInitialized;

        private readonly ToolRegistry _toolRegistry;

        public bool IsRunning => _isRunning;
        public bool IsClientConnected => _pipeServer?.IsConnected ?? false;

        public event EventHandler<string> OnLog;
        public event EventHandler<string> OnError;
        public event EventHandler OnClientConnected;
        public event EventHandler OnClientDisconnected;

        /// <summary>
        /// Constructor
        /// </summary>
        public NamedPipeMCPServer(ToolRegistry toolRegistry)
        {
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        }

        /// <summary>
        /// Start the MCP server
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                Log("Server is already running");
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;
                _isInitialized = false;

                // Start server task
                _serverTask = Task.Run(() => ServerLoop(_cancellationTokenSource.Token));

                Log($"MCP Server started on pipe: \\\\.\\pipe\\{PIPE_NAME}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to start server: {ex.Message}", ex);
                _isRunning = false;
                throw;
            }
        }

        /// <summary>
        /// Stop the MCP server
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            try
            {
                Log("Stopping MCP Server...");
                _isRunning = false;

                // Cancel the server task
                _cancellationTokenSource?.Cancel();

                // Close pipe
                _writer?.Close();
                _reader?.Close();
                _pipeServer?.Close();

                // Wait for task to complete (with timeout)
                _serverTask?.Wait(TimeSpan.FromSeconds(5));

                Log("MCP Server stopped");
            }
            catch (Exception ex)
            {
                LogError($"Error stopping server: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Main server loop - listens for connections and processes messages
        /// </summary>
        private async Task ServerLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Create new pipe server instance
                    _pipeServer = new NamedPipeServerStream(
                        PIPE_NAME,
                        PipeDirection.InOut,
                        1,  // Max connections: 1 (one client at a time)
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous
                    );

                    Log("Waiting for client connection...");

                    // Wait for client connection
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    Log("Client connected!");
                    OnClientConnected?.Invoke(this, EventArgs.Empty);

                    // Create reader/writer
                    _reader = new StreamReader(_pipeServer, Encoding.UTF8);
                    _writer = new StreamWriter(_pipeServer, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };

                    // Process messages while connected
                    await ProcessMessages(cancellationToken);

                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    LogError($"Server loop error: {ex.Message}", ex);
                    await Task.Delay(1000, cancellationToken); // Wait before retry
                }
                finally
                {
                    // Cleanup connection
                    try
                    {
                        _writer?.Close();
                        _reader?.Close();
                        _pipeServer?.Close();
                        _pipeServer?.Dispose();

                        Log("Client disconnected");
                        OnClientDisconnected?.Invoke(this, EventArgs.Empty);
                    }
                    catch { }
                }
            }

            Log("Server loop exited");
        }

        /// <summary>
        /// Process incoming messages from client
        /// </summary>
        private async Task ProcessMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _pipeServer.IsConnected)
            {
                try
                {
                    // Read message
                    string message = await ReadMessageAsync(cancellationToken);
                    if (string.IsNullOrEmpty(message))
                    {
                        break; // Connection closed
                    }

                    Log($"Received: {message}");

                    // Process message
                    string response = await ProcessMessage(message);

                    // Send response
                    if (!string.IsNullOrEmpty(response))
                    {
                        await WriteMessageAsync(response, cancellationToken);
                        Log($"Sent: {response}");
                    }
                }
                catch (IOException)
                {
                    // Pipe disconnected
                    break;
                }
                catch (Exception ex)
                {
                    LogError($"Error processing message: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Read a complete message from the pipe
        /// </summary>
        private async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            StringBuilder messageBuilder = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Read line
                string line = await _reader.ReadLineAsync();
                if (line == null)
                {
                    break; // End of stream
                }

                messageBuilder.AppendLine(line);

                // Check if this is a complete JSON-RPC message
                string currentMessage = messageBuilder.ToString().Trim();
                if (IsCompleteJsonMessage(currentMessage))
                {
                    return currentMessage;
                }
            }

            return messageBuilder.ToString().Trim();
        }

        /// <summary>
        /// Write message to pipe
        /// </summary>
        private async Task WriteMessageAsync(string message, CancellationToken cancellationToken)
        {
            await _writer.WriteLineAsync(message);
            await _writer.FlushAsync();
        }

        /// <summary>
        /// Check if string is a complete JSON message
        /// </summary>
        private bool IsCompleteJsonMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            try
            {
                JToken.Parse(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Process a single MCP message
        /// </summary>
        private async Task<string> ProcessMessage(string messageText)
        {
            try
            {
                // Parse JSON-RPC request
                var request = JsonConvert.DeserializeObject<MCPRequest>(messageText);

                if (request == null)
                {
                    return SerializeResponse(MCPResponse.ErrorResponse(
                        null,
                        MCPErrorCodes.ParseError,
                        "Failed to parse JSON-RPC request"
                    ));
                }

                // Route based on method
                MCPResponse response = await RouteMessage(request);

                // Serialize response (unless notification)
                if (!request.IsNotification)
                {
                    return SerializeResponse(response);
                }

                return null;
            }
            catch (JsonException ex)
            {
                return SerializeResponse(MCPResponse.ErrorResponse(
                    null,
                    MCPErrorCodes.ParseError,
                    $"JSON parse error: {ex.Message}"
                ));
            }
            catch (Exception ex)
            {
                LogError($"Error processing message: {ex.Message}", ex);
                return SerializeResponse(MCPResponse.ErrorResponse(
                    null,
                    MCPErrorCodes.InternalError,
                    $"Internal error: {ex.Message}"
                ));
            }
        }

        /// <summary>
        /// Route message to appropriate handler based on method
        /// </summary>
        private async Task<MCPResponse> RouteMessage(MCPRequest request)
        {
            switch (request.Method)
            {
                case "initialize":
                    return HandleInitialize(request);

                case "tools/list":
                    return HandleToolsList(request);

                case "tools/call":
                    return await HandleToolsCall(request);

                case "ping":
                    return HandlePing(request);

                default:
                    return MCPResponse.ErrorResponse(
                        request.Id,
                        MCPErrorCodes.MethodNotFound,
                        $"Method not found: {request.Method}"
                    );
            }
        }

        /// <summary>
        /// Handle initialize request
        /// </summary>
        private MCPResponse HandleInitialize(MCPRequest request)
        {
            try
            {
                var initParams = request.Params?.ToObject<InitializeParams>();

                var result = new InitializeResult
                {
                    ProtocolVersion = "2024-11-05",
                    Capabilities = new ServerCapabilities
                    {
                        Tools = new ToolsCapability
                        {
                            ListChanged = false
                        }
                    },
                    ServerInfo = new ServerInfo
                    {
                        Name = "revit-claude-mcp",
                        Version = "1.0.0"
                    }
                };

                _isInitialized = true;
                Log("Client initialized successfully");

                return MCPResponse.Success(request.Id, result);
            }
            catch (Exception ex)
            {
                LogError($"Initialize error: {ex.Message}", ex);
                return MCPResponse.ErrorResponse(
                    request.Id,
                    MCPErrorCodes.InternalError,
                    $"Initialize failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Handle tools/list request
        /// </summary>
        private MCPResponse HandleToolsList(MCPRequest request)
        {
            try
            {
                if (!_isInitialized)
                {
                    return MCPResponse.ErrorResponse(
                        request.Id,
                        MCPErrorCodes.InvalidRequest,
                        "Server not initialized. Call 'initialize' first."
                    );
                }

                var tools = _toolRegistry.GetAllToolDefinitions();

                var result = new ToolsListResult
                {
                    Tools = tools
                };

                Log($"Returning {tools.Count} tools");

                return MCPResponse.Success(request.Id, result);
            }
            catch (Exception ex)
            {
                LogError($"Tools list error: {ex.Message}", ex);
                return MCPResponse.ErrorResponse(
                    request.Id,
                    MCPErrorCodes.InternalError,
                    $"Failed to list tools: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Handle tools/call request
        /// </summary>
        private async Task<MCPResponse> HandleToolsCall(MCPRequest request)
        {
            try
            {
                if (!_isInitialized)
                {
                    return MCPResponse.ErrorResponse(
                        request.Id,
                        MCPErrorCodes.InvalidRequest,
                        "Server not initialized. Call 'initialize' first."
                    );
                }

                var callParams = request.Params?.ToObject<ToolsCallParams>();
                if (callParams == null || string.IsNullOrEmpty(callParams.Name))
                {
                    return MCPResponse.ErrorResponse(
                        request.Id,
                        MCPErrorCodes.InvalidParams,
                        "Invalid tool call parameters"
                    );
                }

                Log($"Executing tool: {callParams.Name}");

                // Execute tool
                var result = await _toolRegistry.ExecuteTool(callParams.Name, callParams.Arguments);

                Log($"Tool execution completed: {callParams.Name}");

                return MCPResponse.Success(request.Id, result);
            }
            catch (ToolNotFoundException ex)
            {
                LogError($"Tool not found: {ex.Message}", ex);
                return MCPResponse.ErrorResponse(
                    request.Id,
                    MCPErrorCodes.ToolNotFound,
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                LogError($"Tool execution error: {ex.Message}", ex);
                return MCPResponse.ErrorResponse(
                    request.Id,
                    MCPErrorCodes.ToolExecutionFailed,
                    $"Tool execution failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Handle ping request (for connection testing)
        /// </summary>
        private MCPResponse HandlePing(MCPRequest request)
        {
            return MCPResponse.Success(request.Id, new { status = "ok", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Serialize response to JSON
        /// </summary>
        private string SerializeResponse(MCPResponse response)
        {
            return JsonConvert.SerializeObject(response, Formatting.None);
        }

        /// <summary>
        /// Log message
        /// </summary>
        private void Log(string message)
        {
            Logger.Log($"[MCP Server] {message}");
            OnLog?.Invoke(this, message);
        }

        /// <summary>
        /// Log error
        /// </summary>
        private void LogError(string message, Exception ex = null)
        {
            Logger.LogError($"[MCP Server] {message}", ex);
            OnError?.Invoke(this, message);
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
            _pipeServer?.Dispose();
        }
    }

    /// <summary>
    /// Tool not found exception
    /// </summary>
    public class ToolNotFoundException : Exception
    {
        public ToolNotFoundException(string toolName)
            : base($"Tool not found: {toolName}")
        {
        }
    }
}
