using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RevitClaudeMCP.UI
{
    /// <summary>
    /// Script approval dialog
    /// </summary>
    public partial class ScriptApprovalDialog : Window, INotifyPropertyChanged
    {
        private string _scriptCode;
        private string _description;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ScriptCode
        {
            get => _scriptCode;
            set { _scriptCode = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public ScriptApprovalDialog(string scriptCode, string description)
        {
            InitializeComponent();
            DataContext = this;

            ScriptCode = scriptCode;
            Description = description ?? "Execute C# script in Revit";
        }

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
