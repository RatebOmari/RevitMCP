using System.Windows.Controls;
using Autodesk.Revit.UI;

namespace RevitClaudeMCP.UI
{
    /// <summary>
    /// Dockable pane for MCP server status monitoring
    /// </summary>
    public class MCPStatusPane : IDockablePaneProvider
    {
        private MCPStatusView _view;

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            // Set pane properties
            data.FrameworkElement = GetView();
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed
            };
        }

        /// <summary>
        /// Get or create the view
        /// </summary>
        private FrameworkElement GetView()
        {
            if (_view == null)
            {
                _view = new MCPStatusView();
            }

            return _view;
        }
    }
}
