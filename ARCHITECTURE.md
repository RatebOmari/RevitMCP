# Revit Claude MCP System - Architecture

## Overview

This system enables Claude Desktop to control Autodesk Revit through the Model Context Protocol (MCP). The implementation uses an embedded MCP server running inside the Revit process, communicating via Named Pipes.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                       Claude Desktop                             │
│                    (User Interface)                              │
│                                                                   │
│  User types: "Create a wall from 0,0,0 to 10,0,0"               │
└─────────────────────┬───────────────────────────────────────────┘
                      │ stdio (stdin/stdout)
                      │ JSON-RPC 2.0 messages
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Node.js Bridge                                │
│              (bridge.js - Translation Layer)                     │
│                                                                   │
│  • Reads from stdin (Claude Desktop)                             │
│  • Connects to Named Pipe                                        │
│  • Translates stdio ↔ Named Pipe                                 │
│  • Handles connection lifecycle                                  │
└─────────────────────┬───────────────────────────────────────────┘
                      │ Named Pipe: \\.\pipe\revit-claude-mcp
                      │ JSON-RPC 2.0 messages
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Revit Process Space                             │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │            NamedPipeMCPServer                              │  │
│  │         (MCP Protocol Implementation)                      │  │
│  │                                                            │  │
│  │  • Listens on Named Pipe                                  │  │
│  │  • Implements JSON-RPC 2.0 protocol                       │  │
│  │  • Handles: initialize, tools/list, tools/call            │  │
│  │  • Routes tool calls to appropriate handlers              │  │
│  └──────────────────┬────────────────────────────────────────┘  │
│                     │                                            │
│  ┌──────────────────┴────────────────────────────────────────┐  │
│  │              Tool Dispatcher                               │  │
│  │                                                            │  │
│  │  ┌────────────────┐  ┌──────────────────┐               │  │
│  │  │ Predefined     │  │ Dynamic Script   │               │  │
│  │  │ Tools          │  │ Execution        │               │  │
│  │  │                │  │                  │               │  │
│  │  │ • DocumentInfo │  │ • ScriptValidator│               │  │
│  │  │ • CreateWall   │  │ • ScriptCompiler │               │  │
│  │  │ • ModifyParam  │  │ • ScriptExecutor │               │  │
│  │  │ • GetElements  │  │ • Approval UI    │               │  │
│  │  └────────┬───────┘  └────────┬─────────┘               │  │
│  │           │                    │                          │  │
│  └───────────┴────────────────────┴──────────────────────────┘  │
│               │                    │                             │
│  ┌────────────┴────────────────────┴─────────────────────────┐  │
│  │              RevitContext                                  │  │
│  │      (Thread-Safe Revit API Access)                       │  │
│  │                                                            │  │
│  │  • Uses ExternalEvent for thread safety                   │  │
│  │  • Manages transactions (undo support)                    │  │
│  │  • Queues operations to Revit main thread                 │  │
│  └──────────────────┬─────────────────────────────────────────┘  │
│                     │                                            │
│  ┌──────────────────┴─────────────────────────────────────────┐  │
│  │              Revit API                                     │  │
│  │                                                            │  │
│  │  • Document operations                                     │  │
│  │  • Element creation/modification                           │  │
│  │  • Parameter access                                        │  │
│  │  • Geometry operations                                     │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │         MCPStatusPane (WPF UI)                            │  │
│  │                                                            │  │
│  │  • Shows connection status                                 │  │
│  │  • Real-time log display                                   │  │
│  │  • Server controls (Start/Stop)                            │  │
│  │  • Dockable in Revit UI                                    │  │
│  └────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Communication Flow

### 1. Initialization Flow

```
Claude Desktop                Node.js Bridge              Revit MCP Server
     │                              │                            │
     │──{"method":"initialize"}───>│                            │
     │                              │──{"method":"initialize"}─>│
     │                              │                            │
     │                              │<──{serverInfo, capabilities}
     │<─{serverInfo, capabilities}──│                            │
     │                              │                            │
```

### 2. Tool List Flow

```
Claude Desktop                Node.js Bridge              Revit MCP Server
     │                              │                            │
     │──{"method":"tools/list"}──>│                            │
     │                              │──{"method":"tools/list"}─>│
     │                              │                            │
     │                              │<──{tools: [...]}───────────│
     │<─{tools: [...]}──────────────│                            │
     │                              │                            │
```

### 3. Tool Execution Flow

```
Claude Desktop                Node.js Bridge              Revit MCP Server
     │                              │                            │
     │──{"method":"tools/call"}──>│                            │
     │   {name:"create_wall"}       │                            │
     │   {params:{...}}             │──{"method":"tools/call"}─>│
     │                              │   {name:"create_wall"}     │
     │                              │   {params:{...}}           │
     │                              │                            │
     │                              │                    [Queue to ExternalEvent]
     │                              │                            │
     │                              │                    [Execute in Transaction]
     │                              │                            │
     │                              │<──{content:[{type:"text"}]}
     │<─{content:[{type:"text"}]}───│                            │
```

## Key Components Explained

### 1. Claude Desktop
- User interface where you chat with Claude
- Sends MCP requests over stdio (standard input/output)
- Expects MCP responses in JSON-RPC 2.0 format

### 2. Node.js Bridge (bridge.js)
**Why it's needed**: Claude Desktop only supports stdio communication, but Revit can't use stdio (it's not a console app). The bridge translates between these two transports.

**What it does**:
- Listens on stdin for messages from Claude Desktop
- Connects to Named Pipe (\\.\pipe\revit-claude-mcp)
- Forwards messages bidirectionally
- Handles connection errors and reconnection

### 3. NamedPipeMCPServer
**Why Named Pipes**:
- Revit add-ins run inside the Revit process (not separate .exe)
- Named Pipes allow inter-process communication on Windows
- Suitable for local, fast communication

**What it does**:
- Creates and listens on Named Pipe
- Parses JSON-RPC 2.0 messages
- Implements MCP protocol methods:
  - `initialize`: Server handshake
  - `tools/list`: Returns available tools
  - `tools/call`: Executes a tool
- Routes tool calls to appropriate handlers

### 4. RevitContext
**Why it's needed**: Revit API can only be called from the main UI thread. External calls need special handling.

**What it does**:
- Uses Revit's ExternalEvent mechanism
- Queues operations to execute on main thread
- Wraps operations in transactions (for undo support)
- Provides thread-safe API access

### 5. Tool System

#### Predefined Tools
Safe, pre-built operations:
- `get_document_info`: Document properties
- `create_wall`: Create wall elements
- `modify_parameter`: Change element parameters
- `get_elements`: Query elements by category
- More...

#### Dynamic Script Tools
User-provided C# code executed at runtime:
- `execute_script`: Compiles and runs C# code
- Validated for security (blocks file I/O, network, process spawning)
- Requires user approval before execution
- Full Revit API access within constraints

### 6. Security Validation
**ScriptValidator** checks for dangerous operations:
- ❌ File I/O: System.IO.File, Directory, Path operations
- ❌ Network: HttpClient, WebClient, Socket
- ❌ Process spawning: Process.Start
- ❌ Reflection: Assembly.Load, Type.GetType
- ✅ Revit API: All Autodesk.Revit.* allowed
- ✅ LINQ, collections, math: Standard .NET allowed

### 7. MCPStatusPane
WPF dockable pane showing:
- Server status (Running/Stopped/Error)
- Connection status (Connected/Disconnected)
- Real-time log messages
- Start/Stop controls

## Protocol Details: JSON-RPC 2.0

### Message Format
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "create_wall",
    "arguments": {
      "startX": 0,
      "startY": 0,
      "startZ": 0,
      "endX": 10,
      "endY": 0,
      "endZ": 0
    }
  }
}
```

### Response Format
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Wall created successfully with ID: 123456"
      }
    ]
  }
}
```

### Error Response
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32603,
    "message": "Internal error: Transaction failed"
  }
}
```

## Threading Model

```
┌─────────────────────────────────────────────────────────────┐
│                    Revit Main Thread                         │
│  • UI updates                                                │
│  • Document modifications                                    │
│  • Transaction execution                                     │
│  • ExternalEvent handlers                                    │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │ ExternalEvent
                              │
┌─────────────────────────────┴───────────────────────────────┐
│                Background Thread Pool                        │
│  • Named Pipe server listening                               │
│  • Message parsing                                           │
│  • Request queuing                                           │
│  • Response formatting                                       │
└─────────────────────────────────────────────────────────────┘
```

**Important**: All Revit API calls MUST happen on the main thread via ExternalEvent. The MCP server receives requests on a background thread and queues them for execution.

## Security Model

### Layer 1: Script Validation
- Static analysis of C# code
- Blocks known dangerous patterns
- Prevents before compilation

### Layer 2: Compilation
- Uses Roslyn compiler
- Restricted assembly references
- No dynamic loading

### Layer 3: User Approval
- Shows script to user before execution
- User must click "Approve" or "Reject"
- Stores approval decisions (optional)

### Layer 4: Transaction Boundaries
- All operations in transactions
- Automatic rollback on error
- Preserves undo stack

## Transaction Lifecycle

```
1. Tool called
   ↓
2. Create Transaction
   ↓
3. Execute operation
   ↓
4. Success? → Commit → Return result
   ↓
5. Error? → Rollback → Return error
```

## Extension Points

### Adding New Predefined Tools
1. Create tool class implementing `ITool`
2. Register in `ToolRegistry`
3. Tool automatically appears in `tools/list`

### Adding Dynamic Capabilities
1. Implement validator rules in `ScriptValidator`
2. Add helper methods in `ScriptExecutionContext`
3. Update compilation references

## Performance Considerations

- **Named Pipe**: Low latency (~1ms for local communication)
- **Roslyn Compilation**: ~200-500ms first compile, cached thereafter
- **Transaction Overhead**: ~10-50ms depending on operation
- **ExternalEvent Queue**: Minimal latency (<10ms typically)

## Debugging Strategy

1. **MCP Protocol Issues**: Check bridge.js logs
2. **Server Issues**: Check MCPStatusPane logs
3. **Tool Issues**: Check Revit debug output
4. **Transaction Issues**: Check transaction exceptions
5. **Thread Issues**: Check for InvalidOperationException

## File Locations

```
C:\ProgramData\Autodesk\Revit\Addins\2024\
  └── RevitClaudeMCP.addin          (Manifest)

C:\Program Files\RevitClaudeMCP\
  └── RevitClaudeMCP.dll             (Main assembly)
  └── Dependencies\
      ├── Newtonsoft.Json.dll
      ├── Microsoft.CodeAnalysis.dll
      └── ... (other dependencies)

C:\Users\{User}\AppData\Roaming\Claude\
  └── claude_desktop_config.json     (Claude config)

{Project Root}\
  └── bridge\
      └── bridge.js                  (Node.js bridge)
```

## Compatibility

- **Revit Version**: 2024 (adaptable to 2022, 2023, 2025)
- **.NET Framework**: 4.8
- **Node.js**: 14+ required for bridge
- **Windows**: 10/11 (Named Pipes are Windows-only)
- **Claude Desktop**: Latest version

## Next Steps

With this architecture understood, you can now:
1. Set up the Visual Studio project
2. Implement each component
3. Configure Claude Desktop
4. Test the integration
5. Add custom tools
6. Deploy to production
