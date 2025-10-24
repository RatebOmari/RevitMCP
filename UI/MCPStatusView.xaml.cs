using System.Windows.Controls;

namespace RevitClaudeMCP.UI
{
    /// <summary>
    /// Interaction logic for MCPStatusView.xaml
    /// </summary>
    public partial class MCPStatusView : UserControl
    {
        public MCPStatusView()
        {
            InitializeComponent();

            // Set view model
            DataContext = new MCPStatusViewModel();

            // Auto-scroll log to bottom when updated
            var viewModel = (MCPStatusViewModel)DataContext;
            viewModel.LogUpdated += (s, e) =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    LogScrollViewer.ScrollToEnd();
                });
            };
        }
    }
}
