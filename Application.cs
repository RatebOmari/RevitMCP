using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using RevitClaudeMCP.Core.Server;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.UI;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP
{
    /// <summary>
    /// Main Revit application entry point - loaded when Revit starts
    /// </summary>
    public class Application : IExternalApplication
    {
        private MCPServerManager _serverManager;
        private RevitContextManager _contextManager;
        private static Application _instance;

        public static Application Instance => _instance;
        public MCPServerManager ServerManager => _serverManager;
        public RevitContextManager ContextManager => _contextManager;

        /// <summary>
        /// Called when Revit starts - initialize the MCP server
        /// </summary>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                _instance = this;
                Logger.Log("RevitClaudeMCP starting up...");

                // Initialize logger
                Logger.Initialize();

                // Initialize Revit context manager (handles thread-safe API access)
                _contextManager = new RevitContextManager(application);
                Logger.Log("RevitContext initialized");

                // Initialize MCP server manager
                _serverManager = new MCPServerManager(_contextManager);
                Logger.Log("MCP Server Manager initialized");

                // Register dockable pane for status monitor UI
                RegisterDockablePane(application);

                // Add ribbon button to show/hide status pane
                CreateRibbonPanel(application);

                // Start the MCP server automatically
                _serverManager.StartServer();
                Logger.Log("MCP Server started successfully");

                Logger.Log("RevitClaudeMCP startup complete!");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Startup failed: {ex.Message}", ex);
                TaskDialog.Show("RevitClaudeMCP Error",
                    $"Failed to start MCP server:\n{ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Called when Revit shuts down - cleanup resources
        /// </summary>
        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                Logger.Log("RevitClaudeMCP shutting down...");

                // Stop the MCP server
                _serverManager?.StopServer();
                Logger.Log("MCP Server stopped");

                // Cleanup context manager
                _contextManager?.Dispose();
                Logger.Log("RevitContext disposed");

                // Close logger
                Logger.Close();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Shutdown error: {ex.Message}", ex);
                return Result.Failed;
            }
        }

        /// <summary>
        /// Register the dockable pane for status monitoring UI
        /// </summary>
        private void RegisterDockablePane(UIControlledApplication application)
        {
            try
            {
                // Create unique GUID for this dockable pane
                var paneId = new DockablePaneId(new Guid("F3F5F5F5-1234-5678-9ABC-DEF012345678"));

                // Register the pane
                application.RegisterDockablePane(
                    paneId,
                    "Claude MCP Status",
                    new MCPStatusPane()
                );

                Logger.Log("Dockable pane registered successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to register dockable pane: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Create ribbon panel with controls
        /// </summary>
        private void CreateRibbonPanel(UIControlledApplication application)
        {
            try
            {
                // Create ribbon panel
                string tabName = "Add-Ins";
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Claude MCP");

                // Add button to show status pane
                PushButtonData buttonData = new PushButtonData(
                    "ShowMCPStatus",
                    "MCP Status",
                    typeof(Application).Assembly.Location,
                    typeof(ShowStatusCommand).FullName
                );

                buttonData.ToolTip = "Show Claude MCP Server Status";
                buttonData.LongDescription = "Opens the status monitor for the Claude MCP server, " +
                    "showing connection status, logs, and server controls.";

                PushButton button = panel.AddItem(buttonData) as PushButton;

                Logger.Log("Ribbon panel created successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create ribbon panel: {ex.Message}", ex);
                // Not fatal - continue without ribbon
            }
        }

        /// <summary>
        /// Get the dockable pane ID
        /// </summary>
        public static DockablePaneId GetDockablePaneId()
        {
            return new DockablePaneId(new Guid("F3F5F5F5-1234-5678-9ABC-DEF012345678"));
        }
    }

    /// <summary>
    /// External command to show the MCP status pane
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]
    public class ShowStatusCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the dockable pane and show it
                DockablePaneId paneId = Application.GetDockablePaneId();
                DockablePane pane = commandData.Application.GetDockablePane(paneId);

                if (pane != null)
                {
                    pane.Show();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
