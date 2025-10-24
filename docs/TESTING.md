# Testing Guide

This guide walks you through testing each component of the Revit Claude MCP system to ensure everything works correctly.

## Prerequisites Checklist

Before testing, ensure you have:

- [ ] ✅ Built the project successfully in Visual Studio
- [ ] ✅ Revit 2024 installed
- [ ] ✅ Node.js 14+ installed
- [ ] ✅ Claude Desktop installed
- [ ] ✅ Add-in deployed to `C:\Program Files\RevitClaudeMCP\`
- [ ] ✅ Manifest file in `C:\ProgramData\Autodesk\Revit\Addins\2024\`

## Test Sequence

### Phase 1: Build and Deployment

#### 1.1 Build the Project

1. Open `RevitClaudeMCP.sln` in Visual Studio 2022
2. Set configuration to **Debug** and platform to **x64**
3. Build → **Build Solution** (Ctrl+Shift+B)
4. Check for errors in Output window

**Expected Result:**
```
========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
```

**Troubleshooting:**
- ❌ "Could not find RevitAPI.dll" → Check Revit 2024 is installed at `C:\Program Files\Autodesk\Revit 2024\`
- ❌ NuGet errors → Right-click solution → Restore NuGet Packages
- ❌ Platform mismatch → Ensure Platform Target is set to **x64**

#### 1.2 Verify Deployment

Check that post-build event copied files:

```cmd
dir "C:\Program Files\RevitClaudeMCP\"
```

**Expected Files:**
- `RevitClaudeMCP.dll`
- `Newtonsoft.Json.dll`
- `Microsoft.CodeAnalysis.dll`
- `Microsoft.CodeAnalysis.CSharp.dll`
- `Microsoft.CodeAnalysis.CSharp.Scripting.dll`
- `Microsoft.CodeAnalysis.Scripting.dll`

```cmd
dir "C:\ProgramData\Autodesk\Revit\Addins\2024\"
```

**Expected Files:**
- `RevitClaudeMCP.addin`

---

### Phase 2: Revit Integration Testing

#### 2.1 Load Add-in in Revit

1. Launch **Revit 2024**
2. Create a new project or open existing
3. Check for add-in load in the Revit console

**Expected Result:**
- No error dialogs on startup
- "Add-Ins" tab appears in ribbon
- "Claude MCP" panel visible with "MCP Status" button

**Troubleshooting:**
- ❌ No ribbon panel → Check .addin manifest file path
- ❌ Load error dialog → Check log at `%AppData%\Autodesk\Revit\Autodesk Revit 2024\Journals\`
- ❌ "Could not load file or assembly" → Ensure all DLL dependencies are in `C:\Program Files\RevitClaudeMCP\`

#### 2.2 Open Status Monitor

1. Click **Add-Ins** tab → **Claude MCP** panel → **MCP Status** button
2. Dockable pane should appear

**Expected Result:**
- Status pane opens showing:
  - Server Status: **Running** (green indicator)
  - Client Connected: **No**
  - Tools Registered: **12** (or your count)
  - Pipe Name: `\\.\pipe\revit-claude-mcp`
- Activity log shows initialization messages

**Troubleshooting:**
- ❌ Pane doesn't open → Check debug output in Visual Studio for exceptions
- ❌ Server Status: Stopped → Click **Start Server** button
- ❌ Exception on startup → Check Windows Event Viewer for .NET errors

#### 2.3 Verify Server is Running

Open Command Prompt and check if Named Pipe exists:

```cmd
PowerShell -Command "Get-ChildItem \\.\pipe\ | Where-Object {$_.Name -eq 'revit-claude-mcp'}"
```

**Expected Result:**
```
Name: revit-claude-mcp
```

---

### Phase 3: Bridge Testing

#### 3.1 Test Bridge Standalone

Before connecting Claude, test the bridge can connect to the Named Pipe.

1. Open Command Prompt in bridge directory:
```cmd
cd C:\path\to\RevitClaudeMCP\bridge
```

2. Run bridge:
```cmd
node bridge.js
```

**Expected Output:**
```
[Bridge] 2024-01-15T10:30:00.000Z - ==================================
[Bridge] 2024-01-15T10:30:00.001Z - Revit MCP Bridge starting...
[Bridge] 2024-01-15T10:30:00.001Z - Pipe path: \\.\pipe\revit-claude-mcp
[Bridge] 2024-01-15T10:30:00.001Z - ==================================
[Bridge] 2024-01-15T10:30:00.002Z - Connecting to pipe: \\.\pipe\revit-claude-mcp
[Bridge] 2024-01-15T10:30:00.015Z - Connected to Revit MCP server
[Bridge] 2024-01-15T10:30:00.015Z - Bridge is running. Press Ctrl+C to exit.
```

**Check Revit Status Monitor:**
- Client Connected should change to: **Yes**
- Activity log should show: `Client connected to MCP server`

**Troubleshooting:**
- ❌ "ENOENT" or "ECONNREFUSED" error → Revit MCP server not running, check status monitor
- ❌ Bridge crashes → Ensure Node.js version is 14+: `node --version`
- ❌ Keeps reconnecting → Named pipe not available, restart Revit

3. **Test Manual Message** (Advanced):

In another Command Prompt window:
```cmd
echo {"jsonrpc":"2.0","id":1,"method":"initialize","params":{}} | node bridge.js
```

This should receive an initialization response.

4. Stop the bridge with **Ctrl+C**

---

### Phase 4: Claude Desktop Integration

#### 4.1 Configure Claude Desktop

1. Locate Claude Desktop config file:
```
C:\Users\<YourUsername>\AppData\Roaming\Claude\claude_desktop_config.json
```

2. Edit the file (create if doesn't exist):
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

**IMPORTANT:** Replace `C:\\path\\to\\` with actual path. Use double backslashes `\\`.

3. Save the file

#### 4.2 Restart Claude Desktop

1. **Completely quit** Claude Desktop (right-click system tray → Exit)
2. Wait 5 seconds
3. Launch Claude Desktop again

#### 4.3 Verify Connection

1. Open Claude Desktop
2. Look for MCP indicator (usually bottom-left or settings)
3. Check that "revit" server appears in connected servers list

**Expected Result:**
- MCP server "revit" shows as **Connected**
- Green indicator next to server name

**Troubleshooting:**
- ❌ Server not listed → Check config file path and JSON syntax
- ❌ Server listed but disconnected → Check bridge.js path is correct
- ❌ Connection error → Check Revit is running and MCP server is started

4. **Check Revit Status Monitor:**
- Client Connected: **Yes**
- Activity log shows initialization

---

### Phase 5: Tool Testing

Now let's test each category of tools.

#### 5.1 Test Document Tools

In Claude Desktop, type:

```
Get information about the active Revit document
```

Claude should call the `get_document_info` tool.

**Expected Response:**
```
=== Document Information ===
Title: Project1
Path: [project path]
Is Family: False
...
```

**More Tests:**
```
What view am I currently in?
List all floor plan views in this project
```

#### 5.2 Test Creation Tools

```
Create a wall from point (0, 0, 0) to point (10, 0, 0) with height 10 feet
```

**Expected Result:**
- Wall created in Revit
- Response shows Element ID and details
- Wall visible in active view

**More Tests:**
```
Create a 20x30 foot floor at origin
Create a column at point (5, 5, 0)
```

#### 5.3 Test Query Tools

```
Show me all walls in the project
```

**Expected Response:**
- List of walls with IDs, names, locations

**More Tests:**
```
Count all elements by category
Get parameters of element ID 123456
```

#### 5.4 Test Modification Tools

First, create an element and note its ID. Then:

```
Move element ID [id] by 5 feet in X direction
```

**Expected Result:**
- Element moves in Revit
- Response confirms operation

**More Tests:**
```
Change the "Comments" parameter of element [id] to "Modified by Claude"
Delete element ID [id]
```

---

### Phase 6: Dynamic Script Testing

#### 6.1 Test Simple Script

```
Execute this script:
var doc = ScriptContext.GetActiveDocument(uiApp);
ScriptContext.ShowMessage("Hello", "Script executed! Total elements: " + new FilteredElementCollector(doc).WhereElementIsNotElementType().GetElementCount());
```

**Expected Result:**
1. Script approval dialog appears showing code
2. Click **Approve and Execute**
3. Revit shows TaskDialog with element count
4. Claude receives success message

#### 6.2 Test Security Validation

Try a blocked operation:

```
Execute this script:
System.IO.File.WriteAllText("C:\\test.txt", "data");
```

**Expected Result:**
- Script validation fails
- Error message: "Script validation failed: File I/O operations are not allowed"
- No approval dialog shown (blocked before compilation)

#### 6.3 Test Script Rejection

```
Execute this script:
ScriptContext.ShowMessage("Test", "This will be rejected");
```

In approval dialog, click **Reject**.

**Expected Result:**
- Response: "Script execution rejected by user"
- Script does not execute

---

### Phase 7: Stress Testing

#### 7.1 Rapid Tool Calls

Execute multiple tools quickly:

```
Count elements, get document info, list views, count again
```

**Expected Result:**
- All tools execute successfully
- No threading errors
- Status monitor shows all operations

#### 7.2 Large Data Sets

```
Get all walls in the project (in a project with 500+ walls)
```

**Expected Result:**
- Tool completes (may take a few seconds)
- Response truncated if too large (limit parameter)

#### 7.3 Error Handling

Try invalid operations:

```
Get parameter of element ID 999999999
```

**Expected Result:**
- Error message: "Element with ID 999999999 not found"
- Claude handles error gracefully
- MCP server remains running

---

## Test Results Checklist

After completing all tests, verify:

- [ ] ✅ Add-in loads in Revit without errors
- [ ] ✅ MCP server starts and shows "Running"
- [ ] ✅ Bridge connects to Named Pipe
- [ ] ✅ Claude Desktop recognizes "revit" server
- [ ] ✅ Document tools work correctly
- [ ] ✅ Creation tools create elements
- [ ] ✅ Query tools return data
- [ ] ✅ Modification tools change elements
- [ ] ✅ Script validation blocks dangerous code
- [ ] ✅ Script approval workflow functions
- [ ] ✅ Approved scripts execute successfully
- [ ] ✅ Errors are handled gracefully
- [ ] ✅ Status monitor shows real-time updates
- [ ] ✅ Server survives errors and continues running

---

## Performance Benchmarks

Expected performance metrics:

| Operation | Expected Time |
|-----------|--------------|
| Tool call (simple) | < 100ms |
| Tool call (complex) | < 500ms |
| Script compilation | 200-500ms (first time) |
| Script execution | Variable (depends on script) |
| Bridge latency | < 10ms |
| Named Pipe latency | < 5ms |

---

## Logging and Diagnostics

### Check Logs

**Revit Add-in Log:**
```
%AppData%\RevitClaudeMCP\Logs\
```

Latest log file shows all operations.

**Bridge Console Output:**
Run bridge with output visible to see all messages.

**Revit Journal:**
```
%AppData%\Autodesk\Revit\Autodesk Revit 2024\Journals\
```

Shows Revit-level errors.

### Debug Mode

To enable verbose logging:

1. In Visual Studio, set breakpoints
2. Press **F5** to debug
3. Revit launches with debugger attached
4. All exceptions caught in Visual Studio

---

## Common Test Failures and Solutions

### Test Failed: Bridge Won't Connect

**Symptoms:**
- Bridge shows repeated "Reconnecting..." messages
- Claude Desktop shows "revit" server disconnected

**Solutions:**
1. Check Revit is running
2. Open status monitor, verify server is **Running**
3. Restart MCP server (click Stop, then Start)
4. Check Windows Firewall isn't blocking Node.js

### Test Failed: Tools Return Errors

**Symptoms:**
- Claude calls tools but receives error responses
- Status monitor shows red error messages

**Solutions:**
1. Check active document exists in Revit
2. Verify transaction mode for modification tools
3. Check element IDs are valid
4. Review tool-specific parameters

### Test Failed: Scripts Won't Compile

**Symptoms:**
- Approval dialog appears, but compilation fails
- Error messages about missing references

**Solutions:**
1. Ensure Roslyn packages are deployed
2. Check script syntax
3. Verify using directives are correct
4. Review compilation error details in response

### Test Failed: UI Doesn't Update

**Symptoms:**
- Status monitor shows stale data
- Client connected status doesn't change

**Solutions:**
1. Close and reopen status pane
2. Restart Revit
3. Check for UI thread exceptions in logs

---

## Next Steps

Once all tests pass:

1. ✅ System is production-ready!
2. 📚 Read [CUSTOM_TOOLS.md](CUSTOM_TOOLS.md) to add your own tools
3. 🎯 Read [DYNAMIC_SCRIPTS.md](DYNAMIC_SCRIPTS.md) for advanced scripting
4. 🔧 Keep [TROUBLESHOOTING.md](TROUBLESHOOTING.md) handy for issues
5. 🚀 Start using Claude to control Revit!

---

## Automated Testing Script (Optional)

For advanced users, create a PowerShell script to automate basic checks:

```powershell
# test-revit-mcp.ps1
Write-Host "Testing Revit MCP System..." -ForegroundColor Cyan

# Check files exist
$dllPath = "C:\Program Files\RevitClaudeMCP\RevitClaudeMCP.dll"
$addinPath = "C:\ProgramData\Autodesk\Revit\Addins\2024\RevitClaudeMCP.addin"

if (Test-Path $dllPath) {
    Write-Host "✓ DLL found" -ForegroundColor Green
} else {
    Write-Host "✗ DLL not found at $dllPath" -ForegroundColor Red
}

if (Test-Path $addinPath) {
    Write-Host "✓ Manifest found" -ForegroundColor Green
} else {
    Write-Host "✗ Manifest not found at $addinPath" -ForegroundColor Red
}

# Check Node.js
$nodeVersion = node --version 2>$null
if ($nodeVersion) {
    Write-Host "✓ Node.js installed: $nodeVersion" -ForegroundColor Green
} else {
    Write-Host "✗ Node.js not found" -ForegroundColor Red
}

# Check Revit running
$revitProcess = Get-Process -Name "Revit" -ErrorAction SilentlyContinue
if ($revitProcess) {
    Write-Host "✓ Revit is running" -ForegroundColor Green
} else {
    Write-Host "✗ Revit is not running" -ForegroundColor Yellow
}

# Check Named Pipe
$pipeExists = Get-ChildItem \\.\pipe\ | Where-Object {$_.Name -eq "revit-claude-mcp"}
if ($pipeExists) {
    Write-Host "✓ Named Pipe exists" -ForegroundColor Green
} else {
    Write-Host "✗ Named Pipe not found (server may not be started)" -ForegroundColor Yellow
}

Write-Host "`nTest complete!" -ForegroundColor Cyan
```

Run with:
```powershell
.\test-revit-mcp.ps1
```
