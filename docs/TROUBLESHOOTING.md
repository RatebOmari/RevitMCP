# Troubleshooting Guide

This guide helps you diagnose and fix common issues with the Revit Claude MCP system.

## Quick Diagnosis

Use this checklist to quickly identify the problem area:

```
Is Revit running? → YES → Continue
                 → NO  → Start Revit

Is add-in loaded? → YES → Continue
                  → NO  → See "Add-in Won't Load"

Is MCP server running? → YES → Continue
                       → NO  → See "Server Won't Start"

Is bridge connected? → YES → Continue
                     → NO  → See "Bridge Connection Issues"

Is Claude connected? → YES → Continue
                     → NO  → See "Claude Desktop Issues"

Are tools failing? → YES → See "Tool Execution Issues"
                   → NO  → System working!
```

---

## Build and Deployment Issues

### Issue: Project Won't Build

**Symptoms:**
- Visual Studio shows errors
- Build fails with missing references

**Solutions:**

1. **Missing Revit API References**
   ```
   Error: Could not find RevitAPI.dll
   ```
   **Fix:** Verify Revit 2024 installed at `C:\Program Files\Autodesk\Revit 2024\`

2. **NuGet Package Errors**
   ```
   Error: Package 'Newtonsoft.Json' not found
   ```
   **Fix:**
   - Right-click solution → **Restore NuGet Packages**
   - Tools → NuGet Package Manager → **Manage NuGet Packages for Solution**
   - Reinstall packages

3. **Platform Mismatch**
   ```
   Warning: Platform mismatch
   ```
   **Fix:**
   - Build → Configuration Manager
   - Set **Platform** to **x64**
   - Ensure **Active solution platform** is **x64**

4. **WPF References Missing**
   ```
   Error: Type 'UserControl' not found
   ```
   **Fix:** Add references:
   - PresentationCore
   - PresentationFramework
   - System.Xaml
   - WindowsBase

5. **Roslyn Compilation Errors**
   ```
   Error: Cannot find Microsoft.CodeAnalysis
   ```
   **Fix:**
   - Update NuGet packages
   - Ensure version 4.8.0 or compatible

### Issue: Post-Build Event Fails

**Symptoms:**
- Build succeeds but files not copied
- "Access denied" errors

**Solutions:**

1. **Run Visual Studio as Administrator**
   - Right-click Visual Studio → **Run as administrator**
   - Required for copying to `C:\Program Files\`

2. **Check Target Paths Exist**
   ```cmd
   mkdir "C:\Program Files\RevitClaudeMCP"
   mkdir "C:\ProgramData\Autodesk\Revit\Addins\2024"
   ```

3. **Manual Copy** (if automated fails)
   ```cmd
   copy "bin\Debug\*.dll" "C:\Program Files\RevitClaudeMCP\"
   copy "deployment\RevitClaudeMCP.addin" "C:\ProgramData\Autodesk\Revit\Addins\2024\"
   ```

---

## Revit Add-in Issues

### Issue: Add-in Won't Load

**Symptoms:**
- No ribbon panel appears
- Error dialog on Revit startup
- Add-in not listed in Add-in Manager

**Diagnostic Steps:**

1. **Check Manifest File**
   ```cmd
   notepad "C:\ProgramData\Autodesk\Revit\Addins\2024\RevitClaudeMCP.addin"
   ```

   Verify:
   - File exists
   - XML is well-formed
   - Path to DLL is correct

2. **Check DLL Files**
   ```cmd
   dir "C:\Program Files\RevitClaudeMCP\"
   ```

   Required files:
   - RevitClaudeMCP.dll
   - Newtonsoft.Json.dll
   - Microsoft.CodeAnalysis.dll
   - Microsoft.CodeAnalysis.CSharp.dll
   - Microsoft.CodeAnalysis.CSharp.Scripting.dll
   - Microsoft.CodeAnalysis.Scripting.dll

3. **Check Revit Journal**
   ```
   %AppData%\Autodesk\Revit\Autodesk Revit 2024\Journals\
   ```

   Open latest journal file and search for "RevitClaudeMCP" or "error"

**Common Solutions:**

1. **Wrong Revit Version**
   - Manifest is in `Addins\2024\` but running Revit 2023?
   - Copy manifest to correct version folder

2. **DLL Blocked by Windows**
   - Right-click each DLL → **Properties**
   - Check "Unblock" if shown → **Apply**

3. **Missing Dependencies**
   - Install .NET Framework 4.8
   - Install Visual C++ Redistributable 2015-2022

4. **.NET Framework Version Mismatch**
   - Check project targets .NET Framework 4.8
   - Revit 2024 requires 4.8

5. **Path Issues**
   - Manifest `<Assembly>` path must be absolute
   - Use `C:\Program Files\RevitClaudeMCP\RevitClaudeMCP.dll`
   - NOT relative paths

### Issue: Add-in Loads But Crashes

**Symptoms:**
- Add-in appears briefly then disappears
- Exception dialogs
- Revit becomes unstable

**Solutions:**

1. **Enable Debug Mode**
   - Visual Studio → Debug → Attach to Process
   - Select `Revit.exe`
   - Exceptions will break in Visual Studio

2. **Check Logs**
   ```
   %AppData%\RevitClaudeMCP\Logs\
   ```

   Look for stack traces and exceptions

3. **Common Crash Causes:**

   **Thread Safety Issues:**
   - Ensure all Revit API calls use `RevitContextManager`
   - Never call Revit API from background threads directly

   **Transaction Errors:**
   - Check operations that modify document use transactions
   - Verify transaction mode in tool implementation

   **Null Reference:**
   - Check for null documents, elements, parameters
   - Add null checks before accessing properties

---

## MCP Server Issues

### Issue: Server Won't Start

**Symptoms:**
- Status monitor shows "Stopped"
- "Start Server" button doesn't work
- Error messages in log

**Solutions:**

1. **Check Named Pipe Availability**
   ```powershell
   Get-ChildItem \\.\pipe\ | Where-Object {$_.Name -like "*revit*"}
   ```

   If pipe already exists from another instance:
   - Close all Revit instances
   - Wait 10 seconds
   - Restart Revit

2. **Port/Pipe Conflicts**
   - Another application using the pipe name?
   - Change pipe name in `NamedPipeMCPServer.cs` if needed

3. **Permissions Issues**
   - Run Revit as Administrator
   - Check Windows Firewall not blocking Named Pipes

4. **Check Exception Details**
   - Open status monitor
   - Look for exception messages in activity log
   - Check `%AppData%\RevitClaudeMCP\Logs\` for details

### Issue: Server Starts Then Stops

**Symptoms:**
- Server shows "Running" briefly
- Changes to "Stopped" after a few seconds
- No errors shown

**Solutions:**

1. **Background Exception**
   - Check logs for unhandled exceptions
   - Common cause: thread exceptions

2. **Resource Exhaustion**
   - Close other applications
   - Restart Revit with fresh session

3. **Rebuild and Redeploy**
   - Clean solution
   - Rebuild
   - Restart Revit

---

## Bridge Connection Issues

### Issue: Bridge Won't Connect

**Symptoms:**
- Bridge shows "Reconnecting..." repeatedly
- "ENOENT" or "ECONNREFUSED" errors
- Never connects to pipe

**Solutions:**

1. **Verify Server Running**
   - Open Revit status monitor
   - Confirm "Server Status: Running"
   - If stopped, click "Start Server"

2. **Check Pipe Exists**
   ```powershell
   Get-ChildItem \\.\pipe\ | Where-Object {$_.Name -eq "revit-claude-mcp"}
   ```

   If pipe missing:
   - Server not started
   - Restart MCP server in Revit

3. **Firewall Blocking**
   - Windows Defender Firewall → Allow an app
   - Add `node.exe`
   - Allow both private and public networks

4. **Node.js Issues**
   ```cmd
   node --version
   ```

   Should show v14.0.0 or higher
   - If not, install/update Node.js from nodejs.org

5. **Bridge Script Path**
   - Verify `bridge.js` path is correct
   - Check no syntax errors:
     ```cmd
     node --check bridge.js
     ```

### Issue: Bridge Connects Then Disconnects

**Symptoms:**
- Bridge connects successfully
- Disconnects after a few seconds or minutes
- No obvious errors

**Solutions:**

1. **Timeout Issues**
   - Increase reconnection attempts in bridge.js
   - Check for network/pipe latency

2. **Server Crash**
   - Check Revit status monitor for errors
   - Review Revit logs

3. **Memory Issues**
   - Close unnecessary applications
   - Restart Revit

---

## Claude Desktop Issues

### Issue: "revit" Server Not Listed

**Symptoms:**
- Claude Desktop opens fine
- MCP servers section doesn't show "revit"
- No error messages

**Solutions:**

1. **Check Config File**
   ```
   C:\Users\<YourUsername>\AppData\Roaming\Claude\claude_desktop_config.json
   ```

   Verify:
   - File exists
   - JSON is valid (use jsonlint.com to check)
   - Path to bridge.js is correct
   - Uses double backslashes: `C:\\path\\to\\bridge.js`

2. **Correct Config Format**
   ```json
   {
     "mcpServers": {
       "revit": {
         "command": "node",
         "args": [
           "C:\\Users\\YourName\\RevitClaudeMCP\\bridge\\bridge.js"
         ],
         "env": {}
       }
     }
   }
   ```

3. **Restart Claude Desktop**
   - Completely quit (right-click system tray → Exit)
   - Wait 5-10 seconds
   - Launch again

4. **Check Claude Logs**
   ```
   %AppData%\Claude\logs\
   ```

   Look for MCP-related errors

### Issue: Server Shows as Disconnected

**Symptoms:**
- "revit" server appears in list
- Shows red/disconnected status
- No connection established

**Solutions:**

1. **Bridge Not Running**
   - Config triggers bridge, but bridge can't start
   - Test bridge manually:
     ```cmd
     node C:\path\to\bridge.js
     ```
   - Check for errors

2. **Path Issues**
   - Verify bridge.js path in config is absolute
   - Check path exists
   - Try running manually to test

3. **Node.js Not in PATH**
   - Open Command Prompt:
     ```cmd
     node --version
     ```
   - If "not recognized", add Node.js to PATH

4. **Permission Issues**
   - Claude Desktop needs permission to run Node.js
   - Check Windows Defender or antivirus settings

### Issue: Connection Established But Tools Don't Work

**Symptoms:**
- Claude shows "revit" server connected (green)
- Tools are listed
- Tool calls fail or timeout

**Solutions:**

1. **Server Not Initialized**
   - Check bridge connected to Revit
   - Verify Revit status monitor shows "Client Connected: Yes"

2. **Protocol Mismatch**
   - Ensure using same MCP protocol version
   - Update Claude Desktop to latest version

3. **Tool Registration Issues**
   - Check status monitor shows correct tool count
   - Restart Revit to re-register tools

---

## Tool Execution Issues

### Issue: Tools Return Errors

**Symptoms:**
- Tool calls fail
- Error messages returned
- Operations don't complete

**Common Errors and Fixes:**

1. **"No active document"**
   - Open a Revit project
   - Ensure document is active (not family editor)

2. **"Element not found"**
   - Element ID doesn't exist
   - Element deleted
   - Check ID is correct

3. **"Parameter is read-only"**
   - Trying to modify built-in parameter
   - Some parameters can't be set by API

4. **"Transaction already started"**
   - Tool implementation issue
   - Check not manually starting transactions

5. **"Insufficient permissions"**
   - Document is read-only
   - Workshared document not editable

### Issue: Scripts Won't Compile

**Symptoms:**
- Script approval dialog appears
- After approval, compilation fails
- Error messages about missing references

**Solutions:**

1. **Syntax Errors**
   - Review generated code
   - Check for typos, missing semicolons

2. **Missing Using Directives**
   - Add required using statements:
     ```csharp
     using Autodesk.Revit.DB.Architecture; // For Room class
     using Autodesk.Revit.DB.Mechanical; // For MEP
     ```

3. **Roslyn Issues**
   - Check Microsoft.CodeAnalysis DLLs present
   - Version 4.8.0 required

4. **Memory Issues**
   - Very large scripts may fail to compile
   - Break into smaller operations

### Issue: Scripts Compile But Don't Execute

**Symptoms:**
- Compilation succeeds
- Execution fails immediately
- No changes in Revit

**Solutions:**

1. **Runtime Exceptions**
   - Check logs for exception details
   - Add try-catch in script for debugging

2. **Transaction Issues**
   - Operations succeed but not committed?
   - Transaction automatically managed

3. **Element Visibility**
   - Created elements not visible in current view?
   - Check view filters, phase settings

---

## Performance Issues

### Issue: Slow Tool Execution

**Symptoms:**
- Tools take very long time
- Revit becomes unresponsive
- Timeout errors

**Solutions:**

1. **Large Data Sets**
   - Use `limit` parameter in query tools
   - Filter elements before processing

2. **Complex Operations**
   - Break into smaller batches
   - Add progress feedback

3. **Memory Leaks**
   - Restart Revit periodically
   - Monitor memory usage in Task Manager

### Issue: Bridge Latency

**Symptoms:**
- Delayed responses
- Messages taking long to transmit

**Solutions:**

1. **Check System Resources**
   - Close unnecessary applications
   - Check CPU/memory usage

2. **Network Issues**
   - Named Pipes are local only
   - Check for disk I/O issues

3. **Logging Overhead**
   - Reduce verbose logging
   - Clear old log files

---

## Diagnostic Commands

### Check System Status

```powershell
# Check Revit running
Get-Process -Name "Revit" -ErrorAction SilentlyContinue

# Check Named Pipe exists
Get-ChildItem \\.\pipe\ | Where-Object {$_.Name -eq "revit-claude-mcp"}

# Check Node.js version
node --version

# Check files deployed
Test-Path "C:\Program Files\RevitClaudeMCP\RevitClaudeMCP.dll"
Test-Path "C:\ProgramData\Autodesk\Revit\Addins\2024\RevitClaudeMCP.addin"
```

### View Logs

```cmd
REM Revit MCP logs
explorer "%AppData%\RevitClaudeMCP\Logs"

REM Revit journal
explorer "%AppData%\Autodesk\Revit\Autodesk Revit 2024\Journals"

REM Claude logs
explorer "%AppData%\Claude\logs"
```

### Test Bridge Manually

```cmd
cd C:\path\to\RevitClaudeMCP\bridge
node bridge.js
```

Leave running and check for connection messages.

### Test Tool Manually

Create test command in Revit:

```csharp
[Transaction(TransactionMode.ReadOnly)]
public class TestCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var manager = Application.Instance.ServerManager;
            var status = manager.GetStatus();

            TaskDialog.Show("Test",
                $"Server Running: {status.IsRunning}\n" +
                $"Client Connected: {status.IsClientConnected}\n" +
                $"Tool Count: {status.ToolCount}");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", ex.ToString());
            return Result.Failed;
        }
    }
}
```

---

## Getting Help

If issues persist after trying these solutions:

1. **Check Logs** - Always include log files when asking for help
2. **Document Steps** - Write down exact steps to reproduce
3. **System Info** - Note Revit version, Windows version, .NET version
4. **Error Messages** - Screenshot exact error text

### Log Locations

```
Application logs:   %AppData%\RevitClaudeMCP\Logs\
Revit journals:     %AppData%\Autodesk\Revit\Autodesk Revit 2024\Journals\
Windows Event Log:  Event Viewer → Windows Logs → Application
```

### Useful Debug Info

```
- Revit version: [e.g., 2024.0.1]
- Windows version: [e.g., Windows 11 22H2]
- .NET version: [e.g., 4.8]
- Node.js version: [e.g., v18.17.0]
- Claude Desktop version: [e.g., 0.7.0]
```

---

## Emergency Reset

If everything is broken and you need to start fresh:

### 1. Complete Uninstall

```cmd
REM Stop Revit
taskkill /F /IM Revit.exe

REM Delete add-in files
rd /s /q "C:\Program Files\RevitClaudeMCP"
del "C:\ProgramData\Autodesk\Revit\Addins\2024\RevitClaudeMCP.addin"

REM Delete logs and config
rd /s /q "%AppData%\RevitClaudeMCP"
```

### 2. Clean Build

```
1. Open Visual Studio
2. Build → Clean Solution
3. Close Visual Studio
4. Delete bin/ and obj/ folders
5. Reopen Visual Studio
6. Build → Rebuild Solution
```

### 3. Fresh Install

```
1. Run Visual Studio as Administrator
2. Build project (copies files)
3. Restart Revit
4. Verify add-in loads
5. Open status monitor
6. Start server
7. Test bridge connection
8. Configure Claude Desktop
9. Test tools
```

---

## Prevention Tips

1. **Always run Visual Studio as Administrator** when building
2. **Check status monitor regularly** for early warning signs
3. **Keep logs** for debugging
4. **Test after changes** before committing
5. **Backup working configuration** before major changes
6. **Update gradually** - test each component separately
7. **Monitor resource usage** - restart Revit periodically

---

## Still Having Issues?

The system is complex with many components. Don't get discouraged!

**Systematic Debugging:**
1. Identify which layer is failing
2. Test each component independently
3. Work from bottom up (Revit → Server → Bridge → Claude)
4. Check logs at each level
5. Make one change at a time
6. Document what you tried

Good luck! 🚀
