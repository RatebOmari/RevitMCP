# Revit Claude MCP

> **AI-Powered Revit Automation** - Control Autodesk Revit with natural language using Claude Desktop

[![Revit 2024](https://img.shields.io/badge/Revit-2024-blue.svg)](https://www.autodesk.com/products/revit/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 🚀 Overview

Revit Claude MCP is a production-ready MCP (Model Context Protocol) server that runs **inside** Autodesk Revit, enabling Claude Desktop to interact with Revit through natural language.

Chat with Claude to create walls, modify parameters, query elements, and even execute dynamic C# scripts - all without leaving the conversation.

### What is MCP?

[Model Context Protocol (MCP)](https://modelcontextprotocol.io/) is an open protocol that standardizes how applications provide context to LLMs. It enables Claude Desktop to securely connect to external tools and data sources.

---

## ✨ Features

### 🔧 **12+ Predefined Tools**

- **Document Tools**: Get project info, list views, check active view
- **Creation Tools**: Create walls, floors, columns, rooms
- **Modification Tools**: Move elements, modify parameters, delete elements
- **Query Tools**: Find elements by category, get parameters, count elements

### 💻 **Dynamic Script Execution**

- Write and execute C# code at runtime using Roslyn
- Full Revit API access within scripts
- Security validation blocks dangerous operations
- User approval workflow for safety
- Script examples included

### 🔒 **Security First**

- File I/O operations blocked
- Network access restricted
- Process spawning prevented
- Transaction management ensures undo support
- All modifications reversible

### 🎨 **Status Monitor UI**

- Real-time connection status
- Live activity logs
- Server controls (start/stop/restart)
- Tool count and metrics
- WPF dockable pane integrated into Revit

### 🌉 **Seamless Integration**

- MCP server runs **inside** Revit process (embedded)
- Named Pipes for fast local communication
- Node.js bridge connects Claude Desktop
- No external servers or cloud dependencies

---

## 📋 Prerequisites

- **Autodesk Revit 2024** (adaptable to 2022, 2023, 2025)
- **Visual Studio 2022** (Community or higher)
- **.NET Framework 4.8**
- **Node.js 14+**
- **Claude Desktop** (latest version)
- **Windows 10/11**

---

## 🎯 Quick Start

### 1. Build the Project

```bash
# Clone repository
git clone https://github.com/YOUR_USERNAME/RevitClaudeMCP.git
cd RevitClaudeMCP

# Open in Visual Studio 2022
start RevitClaudeMCP.sln

# Build (ensure Platform Target is x64)
# Run as Administrator if post-build event needs to copy files
```

### 2. Deploy Add-in

Files automatically copied by post-build event:

- DLLs → `C:\Program Files\RevitClaudeMCP\`
- Manifest → `C:\ProgramData\Autodesk\Revit\Addins\2024\`

### 3. Configure Claude Desktop

Edit `%AppData%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "revit": {
      "command": "node",
      "args": [
        "C:\\path\\to\\RevitClaudeMCP\\bridge\\bridge.js"
      ],
      "env": {}
    }
  }
}
```

### 4. Start Using

1. Launch **Revit 2024**
2. Click **Add-Ins** tab → **Claude MCP** → **MCP Status**
3. Verify server status is **Running**
4. Restart **Claude Desktop**
5. Check "revit" server shows as **Connected**
6. Start chatting with Claude!

---

## 💬 Example Usage

### Create Elements

```
Claude, create a wall from point (0,0,0) to (10,0,0) with height 10 feet
```

```
Create a 20 by 30 foot floor at the origin
```

### Query Information

```
What version of Revit am I using?
```

```
Show me all walls in this project grouped by type
```

### Modify Elements

```
Move element ID 123456 by 5 feet in the X direction
```

```
Change the "Comments" parameter of element 123456 to "Reviewed"
```

### Dynamic Scripts

```
Execute a script that colors all walls red if they're taller than 10 feet,
yellow if between 5-10 feet, and green if shorter than 5 feet
```

```
Create room tags for all untagged rooms in the current view
```

---

## 📚 Documentation

Comprehensive guides included:

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System design and component overview
- **[PROJECT_SETUP.md](PROJECT_SETUP.md)** - Detailed setup instructions
- **[docs/TESTING.md](docs/TESTING.md)** - Complete testing procedures
- **[docs/CUSTOM_TOOLS.md](docs/CUSTOM_TOOLS.md)** - How to create your own tools
- **[docs/DYNAMIC_SCRIPTS.md](docs/DYNAMIC_SCRIPTS.md)** - Script examples and patterns
- **[docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)** - Fix common issues
- **[docs/GITHUB_UPLOAD.md](docs/GITHUB_UPLOAD.md)** - Share your project

---

## 🏗️ Architecture

```
Claude Desktop (stdio)
    ↕
Node.js Bridge (stdio ↔ Named Pipe)
    ↕
Named Pipe: \\.\pipe\revit-claude-mcp
    ↕
NamedPipeMCPServer (C# in Revit)
    ↕
RevitContext (Thread-safe API access)
    ↕
Revit API
```

### Key Components

- **NamedPipeMCPServer**: MCP protocol implementation over Named Pipes
- **RevitContextManager**: Thread-safe Revit API access using ExternalEvent
- **ToolRegistry**: Dynamic tool registration and routing
- **ScriptValidator**: Security validation for dynamic scripts
- **ScriptCompiler**: Roslyn-based runtime C# compilation
- **MCPStatusPane**: WPF monitoring interface

---

## 🛠️ Adding Custom Tools

Create a new tool in 3 steps:

### Step 1: Create Tool Class

```csharp
public class MyCustomTool : ToolBase
{
    public override string Name => "my_custom_tool";

    public override string Description =>
        "Description of what this tool does";

    public override JObject InputSchema => CreateSchema(
        "object",
        "Parameters",
        new JObject
        {
            ["param1"] = CreateProperty("string", "Parameter description")
        },
        required: new[] { "param1" }
    );

    public MyCustomTool(RevitContextManager context) : base(context) { }

    protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
    {
        string param1 = GetRequiredParam<string>(arguments, "param1");

        var result = await _context.ExecuteReadOnlyAsync(doc =>
        {
            // Your Revit API code here
            return "Result message";
        });

        return CreateTextResult(result);
    }
}
```

### Step 2: Register Tool

In `MCPServerManager.cs`:

```csharp
_toolRegistry.RegisterTool(new MyCustomTool(_contextManager));
```

### Step 3: Build and Test

Rebuild project, restart Revit, and your tool is available!

See [docs/CUSTOM_TOOLS.md](docs/CUSTOM_TOOLS.md) for detailed examples.

---

## 🔧 Tool Categories

| Category | Tools | Description |
|----------|-------|-------------|
| **Document** | `get_document_info`, `get_active_view`, `list_views` | Project information and views |
| **Creation** | `create_wall`, `create_floor`, `create_column` | Create new elements |
| **Modification** | `modify_parameter`, `move_element`, `delete_element` | Modify existing elements |
| **Query** | `get_elements`, `get_parameter`, `count_elements` | Search and query |
| **Scripting** | `execute_script` | Dynamic C# execution |

---

## 🔒 Security

### What's Blocked

- ❌ File I/O (`System.IO.File`, `Directory`, etc.)
- ❌ Network access (`HttpClient`, `WebClient`, `Socket`)
- ❌ Process spawning (`Process.Start`)
- ❌ Dynamic assembly loading
- ❌ Registry access
- ❌ Unsafe code

### What's Allowed

- ✅ Full Revit API access
- ✅ LINQ and collections
- ✅ Math operations
- ✅ Element creation/modification
- ✅ Parameter access
- ✅ User dialogs

All script execution requires explicit user approval.

---

## 🐛 Troubleshooting

Having issues? Check these resources:

1. **[docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)** - Comprehensive problem-solving guide
2. **[docs/TESTING.md](docs/TESTING.md)** - Verify each component works
3. **GitHub Issues** - Search existing issues or create new one

### Common Issues

- **Add-in won't load**: Check DLLs are in `C:\Program Files\RevitClaudeMCP\`
- **Server won't start**: Run Visual Studio as Administrator
- **Bridge won't connect**: Verify Revit MCP server is running
- **Claude disconnected**: Check `claude_desktop_config.json` path

---

## 📊 Compatibility

| Revit Version | Status | Notes |
|---------------|--------|-------|
| 2024 | ✅ Tested | Primary target |
| 2023 | ⚠️ Compatible | Change references |
| 2022 | ⚠️ Compatible | Change references |
| 2025 | ⚠️ Compatible | Change references |

See [PROJECT_SETUP.md](PROJECT_SETUP.md) for version adaptation instructions.

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🌟 Acknowledgments

- **Anthropic** - Claude AI and MCP protocol
- **Autodesk** - Revit API
- **Roslyn** - C# compilation at runtime
- **Community** - Feedback and contributions

---

## 🔗 Resources

- **MCP Protocol**: https://modelcontextprotocol.io/
- **Revit API**: https://www.revitapidocs.com/
- **Claude Desktop**: https://claude.ai/
- **Roslyn**: https://github.com/dotnet/roslyn

---

## ⭐ Show Your Support

If this project helped you, please star ⭐ the repository and share it with others!

---

Made with ❤️ for the Revit and AI communities.

**Happy Building!** 🏗️🤖
