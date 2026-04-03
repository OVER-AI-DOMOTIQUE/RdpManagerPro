using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using RDPManager.Models;
using RDPManager.ViewModels;

namespace RDPManager
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool _isExplicitExit;
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            InitializeNotifyIcon();
        }

        #region Title Bar

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && _viewModel.MinimizeToTray && !_isExplicitExit)
            {
                Hide();
                _notifyIcon.Visible = true;
            }
            else
            {
                ExitApplication();
            }
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaxBtn.Content = "□";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaxBtn.Content = "❐";
            }
        }

        #endregion

        #region Notify Icon

        private TrayFlyout _trayFlyout;

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Text = "RDP Manager Pro";
            try
            {
                // Loading from the generated ICO file (most reliable for NotifyIcon)
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app_icon.ico");
                if (File.Exists(icoPath))
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(icoPath);
                }
                else
                {
                    // Fallback using PNG resource (more robust if file is missing)
                    var iconUri = new Uri("pack://application:,,,/RDPManager;component/Resources/app_icon.png");
                    var iconStream = System.Windows.Application.GetResourceStream(iconUri).Stream;
                    using (var bitmap = new System.Drawing.Bitmap(iconStream))
                    {
                        IntPtr hIcon = bitmap.GetHicon();
                        _notifyIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Icon load failed: " + ex.Message);
            }

            _notifyIcon.Visible = true;
            
            // On double click, show main window
            _notifyIcon.DoubleClick += (s, args) => ShowWindow();

            // On click, show our custom flyout
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left || e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    if (_trayFlyout == null)
                    {
                        _trayFlyout = new TrayFlyout(_viewModel);
                    }
                    
                    if (_trayFlyout.IsVisible)
                    {
                        _trayFlyout.Hide();
                    }
                    else
                    {
                        _trayFlyout.ShowAtCursor();
                    }
                }
            };
        }

        private void ExitApplication()
        {
            _isExplicitExit = true;
            if (_viewModel != null)
                _viewModel.Dispose();
            if (_notifyIcon != null)
                _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _notifyIcon.Visible = false;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit && _viewModel != null && _viewModel.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                _notifyIcon.Visible = true;
            }
            base.OnClosing(e);
        }

        #endregion

        #region Sidebar Events

        private void Connection_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border != null)
            {
                var connection = border.DataContext as RdpConnection;
                if (connection != null && _viewModel != null)
                {
                    _viewModel.SelectedConnection = connection;
                }
            }
        }

        private void GroupHeader_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border != null)
            {
                var group = border.Tag as ConnectionGroup;
                if (group != null)
                {
                    group.IsExpanded = !group.IsExpanded;
                }
            }
        }

        private void FilterAll_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null) _viewModel.FilterType = "Tous";
        }

        private void FilterRDP_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null) _viewModel.FilterType = "RDP";
        }

        private void FilterSSH_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null) _viewModel.FilterType = "SSH";
        }

        #endregion
    }
}