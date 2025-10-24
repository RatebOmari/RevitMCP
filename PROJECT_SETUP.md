# Project Setup Guide

## Prerequisites

Before starting, ensure you have:

- [ ] **Visual Studio 2022** (Community, Professional, or Enterprise)
- [ ] **Autodesk Revit 2024** installed
- [ ] **.NET Framework 4.8 SDK** (included with Visual Studio)
- [ ] **Node.js 14+** installed (for bridge script)
- [ ] **Claude Desktop** installed
- [ ] **Administrator access** (for add-in deployment)

## Step 1: Create Visual Studio Solution

### 1.1 Create New Project

1. Open **Visual Studio 2022**
2. Click **Create a new project**
3. Search for **"Class Library (.NET Framework)"**
4. Click **Next**

**Project Settings**:
- **Project name**: `RevitClaudeMCP`
- **Location**: Choose your preferred location (e.g., `C:\Dev\RevitClaudeMCP`)
- **Solution name**: `RevitClaudeMCP`
- **Framework**: `.NET Framework 4.8`

5. Click **Create**

### 1.2 Initial Project Cleanup

1. **Delete** the default `Class1.cs` file (we'll create our own structure)
2. Right-click the project in Solution Explorer → **Properties**
3. Go to **Application** tab:
   - **Assembly name**: `RevitClaudeMCP`
   - **Default namespace**: `RevitClaudeMCP`
   - **Target framework**: `.NET Framework 4.8`
4. Go to **Build** tab:
   - **Platform target**: `x64` (Revit is 64-bit)
   - **Warning level**: `4`
   - **Treat warnings as errors**: ✅ All

## Step 2: Project Structure

Create the following folder structure in your project:

```
RevitClaudeMCP/                    (Solution root)
├── RevitClaudeMCP/                (C# project root)
│   ├── Properties/
│   │   └── AssemblyInfo.cs        (Auto-generated)
│   ├── Core/                      (Core MCP server components)
│   │   ├── MCPServer/
│   │   │   ├── NamedPipeMCPServer.cs
│   │   │   ├── MCPMessage.cs
│   │   │   ├── MCPResponse.cs
│   │   │   └── MCPProtocol.cs
│   │   ├── RevitContext/
│   │   │   ├── RevitContext.cs
│   │   │   ├── RevitEventHandler.cs
│   │   │   └── RevitOperation.cs
│   │   └── Server/
│   │       ├── MCPServerManager.cs
│   │       └── ServerConfig.cs
│   ├── Tools/                     (MCP tools implementation)
│   │   ├── Base/
│   │   │   ├── ITool.cs
│   │   │   ├── ToolBase.cs
│   │   │   ├── ToolRegistry.cs
│   │   │   └── ToolParameter.cs
│   │   ├── Document/
│   │   │   ├── DocumentInfoTool.cs
│   │   │   ├── GetActiveTool.cs
│   │   │   └── ListViewsTool.cs
│   │   ├── Creation/
│   │   │   ├── CreateWallTool.cs
│   │   │   ├── CreateFloorTool.cs
│   │   │   └── CreateColumnTool.cs
│   │   ├── Modification/
│   │   │   ├── ModifyParameterTool.cs
│   │   │   ├── MoveElementTool.cs
│   │   │   └── DeleteElementTool.cs
│   │   ├── Query/
│   │   │   ├── GetElementsTool.cs
│   │   │   ├── GetParameterTool.cs
│   │   │   └── CountElementsTool.cs
│   │   └── Scripting/
│   │       ├── ExecuteScriptTool.cs
│   │       └── ScriptLibraryTool.cs
│   ├── Scripting/                 (Dynamic script execution)
│   │   ├── ScriptValidator.cs
│   │   ├── ScriptCompiler.cs
│   │   ├── ScriptExecutor.cs
│   │   ├── ScriptExecutionContext.cs
│   │   └── ValidationRule.cs
│   ├── UI/                        (WPF user interface)
│   │   ├── MCPStatusPane.cs       (Dockable pane)
│   │   ├── MCPStatusViewModel.cs  (MVVM view model)
│   │   ├── MCPStatusView.xaml     (UI layout)
│   │   ├── MCPStatusView.xaml.cs  (Code-behind)
│   │   ├── ScriptApprovalDialog.xaml
│   │   ├── ScriptApprovalDialog.xaml.cs
│   │   └── Converters/
│   │       └── StatusToColorConverter.cs
│   ├── Utils/                     (Utility classes)
│   │   ├── Logger.cs
│   │   ├── GeometryHelper.cs
│   │   ├── ElementHelper.cs
│   │   └── TransactionHelper.cs
│   ├── Application.cs             (Main add-in entry point)
│   ├── Command.cs                 (External command)
│   └── RevitClaudeMCP.csproj      (Project file)
├── bridge/                        (Node.js bridge)
│   ├── bridge.js                  (Main bridge script)
│   ├── package.json
│   └── package-lock.json
├── deployment/                    (Deployment files)
│   ├── RevitClaudeMCP.addin       (Revit manifest)
│   └── install.bat                (Installation script)
├── docs/                          (Documentation)
│   ├── ARCHITECTURE.md
│   ├── DEVELOPMENT.md
│   ├── TOOLS.md
│   └── EXAMPLES.md
├── tests/                         (Test files)
│   └── sample-scripts/
│       ├── rotate-elements.cs
│       ├── color-code.cs
│       └── tag-untagged.cs
├── .gitignore
├── README.md
└── LICENSE
```

### Create Folders in Visual Studio

Right-click the project → **Add** → **New Folder** for each folder above.

## Step 3: Add NuGet Packages

### 3.1 Using NuGet Package Manager

1. Right-click the project → **Manage NuGet Packages**
2. Click **Browse** tab
3. Install the following packages:

#### Required Packages

| Package Name | Version | Purpose |
|-------------|---------|---------|
| `Newtonsoft.Json` | 13.0.3 | JSON serialization for MCP protocol |
| `Microsoft.CodeAnalysis.CSharp` | 4.8.0 | Roslyn compiler for script compilation |
| `Microsoft.CodeAnalysis.CSharp.Scripting` | 4.8.0 | Script execution support |
| `System.IO.Pipes` | 4.3.0 | Named Pipes (usually built-in, but explicit) |

#### Installation Commands (Package Manager Console)

Alternatively, use the Package Manager Console:

```powershell
Install-Package Newtonsoft.Json -Version 13.0.3
Install-Package Microsoft.CodeAnalysis.CSharp -Version 4.8.0
Install-Package Microsoft.CodeAnalysis.CSharp.Scripting -Version 4.8.0
```

### 3.2 Add Revit API References

1. Right-click **References** → **Add Reference**
2. Click **Browse** → **Browse...**
3. Navigate to Revit installation folder:
   ```
   C:\Program Files\Autodesk\Revit 2024\
   ```
4. Select these DLLs:
   - `RevitAPI.dll`
   - `RevitAPIUI.dll`
5. Click **Add** → **OK**

### 3.3 Set Revit References to "Copy Local = False"

**Important**: Revit DLLs should NOT be copied to output.

1. In Solution Explorer, expand **References**
2. Select `RevitAPI` → **Properties** (F4)
3. Set **Copy Local** = `False`
4. Repeat for `RevitAPIUI`

### 3.4 Add WPF References

1. Right-click **References** → **Add Reference**
2. Click **Assemblies** → **Framework**
3. Check:
   - `PresentationCore`
   - `PresentationFramework`
   - `System.Xaml`
   - `WindowsBase`
4. Click **OK**

## Step 4: Configure Project File (.csproj)

The complete `.csproj` file will be provided in the next section. It includes:
- All references configured correctly
- Post-build events for deployment
- Debug settings for Revit
- Resource configurations for XAML

## Step 5: Configure Debugging

### 5.1 Set Debug Properties

1. Right-click project → **Properties**
2. Go to **Debug** tab:
   - **Start Action**: Select **Start external program**
   - **Start external program**:
     ```
     C:\Program Files\Autodesk\Revit 2024\Revit.exe
     ```
   - **Command line arguments**: (leave empty, or add `/language ENU` for English)

### 5.2 Save Debug Settings

Press **Ctrl+S** to save. Now when you press **F5**, Visual Studio will:
1. Build the project
2. Launch Revit
3. Attach the debugger
4. Load your add-in

## Step 6: Create .addin Manifest File

1. Create folder: `deployment/`
2. Create file: `RevitClaudeMCP.addin`

Contents (will be provided in configuration files section).

## Step 7: Setup Node.js Bridge

### 7.1 Create Bridge Folder

In your solution root, create:
```
bridge/
├── bridge.js
├── package.json
└── package-lock.json
```

### 7.2 Initialize NPM

Open terminal in `bridge/` folder:

```bash
cd bridge
npm init -y
```

### 7.3 No Dependencies Needed

The bridge uses only Node.js built-in modules (`readline`, `net`), so no npm packages required.

## Step 8: Post-Build Automation

We'll configure the project to automatically copy files to Revit add-ins folder after build.

Add to `.csproj` (will be in complete project file):

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="xcopy /Y /R &quot;$(TargetPath)&quot; &quot;C:\Program Files\RevitClaudeMCP\&quot;" />
  <Exec Command="xcopy /Y /R &quot;$(ProjectDir)deployment\RevitClaudeMCP.addin&quot; &quot;C:\ProgramData\Autodesk\Revit\Addins\2024\&quot;" />
</Target>
```

## Step 9: Git Configuration

### 9.1 Initialize Git Repository

```bash
cd /path/to/RevitClaudeMCP
git init
```

### 9.2 Create .gitignore

(Will be provided in configuration files section)

## Step 10: Verify Setup Checklist

Before writing code, verify:

- [ ] ✅ Visual Studio solution created
- [ ] ✅ All folders created
- [ ] ✅ NuGet packages installed
- [ ] ✅ Revit API references added (Copy Local = False)
- [ ] ✅ WPF references added
- [ ] ✅ Debug settings configured (Revit.exe)
- [ ] ✅ Platform target set to x64
- [ ] ✅ Bridge folder created with package.json
- [ ] ✅ Deployment folder created
- [ ] ✅ Git initialized

## Next Steps

With the project structure in place, you're ready to:
1. Implement the core components (Part 2)
2. Create the UI (Part 3)
3. Set up the bridge (Part 4)
4. Configure deployment (Part 5)

## Common Setup Issues

### Issue: "Could not find Revit API"
**Solution**: Verify Revit 2024 is installed at `C:\Program Files\Autodesk\Revit 2024\`

### Issue: "Platform mismatch warning"
**Solution**: Ensure Platform Target is `x64`, not `Any CPU`

### Issue: "Cannot find PresentationCore"
**Solution**: Add WPF references (PresentationCore, PresentationFramework, System.Xaml, WindowsBase)

### Issue: "NuGet package restore failed"
**Solution**: Enable NuGet package restore in Visual Studio settings

### Issue: "Post-build event fails"
**Solution**: Run Visual Studio as Administrator (needed to copy to Program Files)

## Adapting for Other Revit Versions

To target Revit 2022, 2023, or 2025:

1. **Change Revit API references** to match version:
   ```
   C:\Program Files\Autodesk\Revit 2023\RevitAPI.dll
   ```

2. **Update .addin file location**:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\2023\
   ```

3. **Update post-build paths** in .csproj

4. **Change debug Revit.exe path**:
   ```
   C:\Program Files\Autodesk\Revit 2023\Revit.exe
   ```

That's it! The code is version-agnostic for Revit 2022-2025.

## Performance Tips

- **Build Configuration**: Use `Debug` for development, `Release` for production
- **Incremental Build**: Enable in Tools → Options → Projects and Solutions → Build and Run
- **Parallel Build**: Enable maximum parallel builds for faster compilation

## IDE Extensions (Optional but Recommended)

- **ReSharper** or **Rider**: Advanced C# refactoring
- **XAML Styler**: Auto-format XAML files
- **Productivity Power Tools**: Enhanced Visual Studio features
- **GitLens** (VS Code) or **Git Extensions**: Better Git integration

You're all set! Let's move on to implementing the actual code.
