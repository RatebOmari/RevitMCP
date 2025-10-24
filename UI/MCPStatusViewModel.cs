using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using RevitClaudeMCP.Core.Server;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP.UI
{
    /// <summary>
    /// View model for MCP status pane
    /// </summary>
    public class MCPStatusViewModel : INotifyPropertyChanged
    {
        private readonly MCPServerManager _serverManager;
        private readonly StringBuilder _logBuilder;
        private string _logText;
        private string _serverStatusText;
        private string _clientConnectedText;
        private int _toolCount;
        private bool _canStart;
        private bool _canStop;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler LogUpdated;

        // Properties
        public string LogText
        {
            get => _logText;
            set { _logText = value; OnPropertyChanged(); }
        }

        public string ServerStatusText
        {
            get => _serverStatusText;
            set { _serverStatusText = value; OnPropertyChanged(); OnPropertyChanged(nameof(ServerStatus)); }
        }

        public string ClientConnectedText
        {
            get => _clientConnectedText;
            set { _clientConnectedText = value; OnPropertyChanged(); }
        }

        public int ToolCount
        {
            get => _toolCount;
            set { _toolCount = value; OnPropertyChanged(); }
        }

        public bool CanStart
        {
            get => _canStart;
            set { _canStart = value; OnPropertyChanged(); }
        }

        public bool CanStop
        {
            get => _canStop;
            set { _canStop = value; OnPropertyChanged(); }
        }

        public bool CanRestart => _canStop;

        public string ServerStatus => _serverStatusText;

        // Commands
        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand RestartServerCommand { get; }
        public ICommand ClearLogCommand { get; }

        public MCPStatusViewModel()
        {
            _logBuilder = new StringBuilder();
            _serverManager = Application.Instance?.ServerManager;

            // Initialize commands
            StartServerCommand = new RelayCommand(StartServer);
            StopServerCommand = new RelayCommand(StopServer);
            RestartServerCommand = new RelayCommand(RestartServer);
            ClearLogCommand = new RelayCommand(ClearLog);

            // Subscribe to events
            if (_serverManager != null)
            {
                _serverManager.OnLog += ServerManager_OnLog;
                _serverManager.OnError += ServerManager_OnError;
                _serverManager.OnServerStarted += (s, e) => UpdateStatus();
                _serverManager.OnServerStopped += (s, e) => UpdateStatus();
                _serverManager.OnClientConnected += (s, e) => UpdateStatus();
                _serverManager.OnClientDisconnected += (s, e) => UpdateStatus();
            }

            // Subscribe to logger events
            Logger.OnLog += Logger_OnLog;
            Logger.OnError += Logger_OnError;

            // Initial status update
            UpdateStatus();
            AddLog("MCP Status Monitor initialized");
        }

        private void StartServer()
        {
            try
            {
                _serverManager?.StartServer();
            }
            catch (Exception ex)
            {
                AddLog($"ERROR: Failed to start server: {ex.Message}");
            }
        }

        private void StopServer()
        {
            try
            {
                _serverManager?.StopServer();
            }
            catch (Exception ex)
            {
                AddLog($"ERROR: Failed to stop server: {ex.Message}");
            }
        }

        private void RestartServer()
        {
            try
            {
                _serverManager?.RestartServer();
            }
            catch (Exception ex)
            {
                AddLog($"ERROR: Failed to restart server: {ex.Message}");
            }
        }

        private void ClearLog()
        {
            _logBuilder.Clear();
            LogText = string.Empty;
            AddLog("Log cleared");
        }

        private void UpdateStatus()
        {
            if (_serverManager == null)
            {
                ServerStatusText = "Not Available";
                ClientConnectedText = "N/A";
                ToolCount = 0;
                CanStart = false;
                CanStop = false;
                return;
            }

            var status = _serverManager.GetStatus();

            ServerStatusText = status.IsRunning ? "Running" : "Stopped";
            ClientConnectedText = status.IsClientConnected ? "Yes" : "No";
            ToolCount = status.ToolCount;
            CanStart = !status.IsRunning;
            CanStop = status.IsRunning;
            OnPropertyChanged(nameof(CanRestart));
        }

        private void ServerManager_OnLog(object sender, string message)
        {
            AddLog($"[Server] {message}");
        }

        private void ServerManager_OnError(object sender, string message)
        {
            AddLog($"[ERROR] {message}");
        }

        private void Logger_OnLog(object sender, LogEventArgs e)
        {
            AddLog($"[{e.Timestamp:HH:mm:ss}] {e.Message}");
        }

        private void Logger_OnError(object sender, LogEventArgs e)
        {
            AddLog($"[{e.Timestamp:HH:mm:ss}] ERROR: {e.Message}");
        }

        private void AddLog(string message)
        {
            _logBuilder.AppendLine(message);

            // Keep last 1000 lines only
            const int maxLines = 1000;
            string[] lines = _logBuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            if (lines.Length > maxLines)
            {
                _logBuilder.Clear();
                for (int i = lines.Length - maxLines; i < lines.Length; i++)
                {
                    _logBuilder.AppendLine(lines[i]);
                }
            }

            LogText = _logBuilder.ToString();
            LogUpdated?.Invoke(this, EventArgs.Empty);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Simple relay command implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
