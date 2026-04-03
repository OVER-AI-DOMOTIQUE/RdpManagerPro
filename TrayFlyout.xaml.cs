using System;
using System.Windows;
using System.Windows.Input;
using RDPManager.Models;
using RDPManager.ViewModels;

namespace RDPManager
{
    public partial class TrayFlyout : Window
    {
        private MainViewModel _viewModel;

        public TrayFlyout(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void Connection_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border != null)
            {
                var connection = border.DataContext as RdpConnection;
                if (connection != null)
                {
                    _viewModel.SelectedConnection = connection;
                    _viewModel.ConnectCommand.Execute(null);
                    Hide();
                }
            }
        }

        private void OpenMain_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
            Application.Current.MainWindow.Activate();
            Hide();
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public void ShowAtCursor()
        {
            // Simple positioning logic: bottom-right above taskbar
            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - Width - 10;
            Top = workingArea.Bottom - Height - 10;
            
            Show();
            Activate();
        }
    }
}
