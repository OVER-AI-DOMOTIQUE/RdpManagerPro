using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RDPManager.Models;
using RDPManager.Services;

namespace RDPManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly RdpService _rdpService;
        private readonly PingService _pingService;
        private AppSettings _appSettings;
        private RdpConnection _selectedConnection;
        private string _searchText = "";
        private string _filterType = "Tous";
        private bool _isSettingsVisible;
        private bool _isNotificationVisible;
        private string _notificationMessage;
        private bool _isDeleteConfirmationVisible;
        private string _deleteConfirmationMessage;
        private bool _isUndoVisible;
        private RdpConnection _lastDeletedConnection;
        private int _lastDeletedIndex;
        private CancellationTokenSource _notificationCts;
        private bool _isAddGroupVisible;
        private string _newGroupName = "";
        private bool _isTipVisible = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<RdpConnection> Connections { get; set; }
        public ObservableCollection<ConnectionGroup> Groups { get; set; }
        public ICollectionView FilteredConnections { get; private set; }

        #region Properties

        public RdpConnection SelectedConnection
        {
            get { return _selectedConnection; }
            set
            {
                _selectedConnection = value;
                OnPropertyChanged();
                OnPropertyChanged("IsConnectionSelected");
            }
        }

        public bool IsConnectionSelected
        {
            get { return SelectedConnection != null; }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged();
                RebuildGroups();
            }
        }

        public string FilterType
        {
            get { return _filterType; }
            set
            {
                if (_filterType != value)
                {
                    _filterType = value;
                    OnPropertyChanged();
                    RebuildGroups();
                }
            }
        }

        public bool StartWithWindows
        {
            get { return _appSettings.StartWithWindows; }
            set
            {
                if (_appSettings.StartWithWindows != value)
                {
                    _appSettings.StartWithWindows = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool MinimizeToTray
        {
            get { return _appSettings.MinimizeToTray; }
            set
            {
                if (_appSettings.MinimizeToTray != value)
                {
                    _appSettings.MinimizeToTray = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CheckOnlineStatus
        {
            get { return _appSettings.CheckOnlineStatus; }
            set
            {
                if (_appSettings.CheckOnlineStatus != value)
                {
                    _appSettings.CheckOnlineStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSettingsVisible
        {
            get { return _isSettingsVisible; }
            set { _isSettingsVisible = value; OnPropertyChanged(); }
        }

        public bool IsNotificationVisible
        {
            get { return _isNotificationVisible; }
            set { _isNotificationVisible = value; OnPropertyChanged(); }
        }

        public string NotificationMessage
        {
            get { return _notificationMessage; }
            set { _notificationMessage = value; OnPropertyChanged(); }
        }

        public bool IsDeleteConfirmationVisible
        {
            get { return _isDeleteConfirmationVisible; }
            set { _isDeleteConfirmationVisible = value; OnPropertyChanged(); }
        }

        public string DeleteConfirmationMessage
        {
            get { return _deleteConfirmationMessage; }
            set { _deleteConfirmationMessage = value; OnPropertyChanged(); }
        }

        public bool IsUndoVisible
        {
            get { return _isUndoVisible; }
            set { _isUndoVisible = value; OnPropertyChanged(); }
        }

        public bool IsAddGroupVisible
        {
            get { return _isAddGroupVisible; }
            set { _isAddGroupVisible = value; OnPropertyChanged(); }
        }

        public string NewGroupName
        {
            get { return _newGroupName; }
            set { _newGroupName = value; OnPropertyChanged(); }
        }

        public bool IsTipVisible
        {
            get { return _isTipVisible; }
            set { _isTipVisible = value; OnPropertyChanged(); }
        }

        // Dashboard Properties
        public int TotalConnections => Connections.Count;
        public int OnlineConnections => Connections.Count(c => c.IsOnline);
        public int OfflineConnections => Connections.Count(c => !c.IsOnline);

        public IEnumerable<RdpConnection> SortedConnections => Connections.OrderBy(c => c.SortIndex).ToList();

        public ObservableCollection<RdpConnection> RecentConnections 
        {
            get 
            {
                var recent = Connections
                    .Where(c => c.LastConnected != DateTime.MinValue)
                    .OrderByDescending(c => c.LastConnected)
                    .Take(3)
                    .ToList();
                return new ObservableCollection<RdpConnection>(recent);
            }
        }

        #endregion

        #region Commands

        public ICommand AddCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand ConnectCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand CloseSettingsCommand { get; private set; }
        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand ConfirmDeleteCommand { get; private set; }
        public ICommand CancelDeleteCommand { get; private set; }
        public ICommand UndoDeleteCommand { get; private set; }
        public ICommand ToggleGroupCommand { get; private set; }
        public ICommand ShowAddGroupCommand { get; private set; }
        public ICommand CreateGroupCommand { get; private set; }
        public ICommand CancelAddGroupCommand { get; private set; }
        public ICommand RefreshStatusCommand { get; private set; }
        public ICommand CloseTipCommand { get; private set; }
        public ICommand GoHomeCommand { get; private set; }
        public ICommand MoveUpCommand { get; private set; }
        public ICommand MoveDownCommand { get; private set; }

        #endregion

        public MainViewModel()
        {
            _rdpService = new RdpService();
            _pingService = new PingService();

            Connections = new ObservableCollection<RdpConnection>(_rdpService.LoadConnections());
            Groups = new ObservableCollection<ConnectionGroup>();
            _appSettings = _rdpService.LoadSettings();

            // Commands
            AddCommand = new RelayCommand(_ => AddConnection());
            DeleteCommand = new RelayCommand(_ => RequestDeleteConnection(), _ => IsConnectionSelected);
            SaveCommand = new RelayCommand(_ => SaveChanges());
            ConnectCommand = new RelayCommand(p => Connect(p), _ => SelectedConnection != null || _pingService != null);
            SettingsCommand = new RelayCommand(_ => IsSettingsVisible = true);
            CloseSettingsCommand = new RelayCommand(_ => IsSettingsVisible = false);
            SaveSettingsCommand = new RelayCommand(_ => ExecuteSaveSettings());
            ConfirmDeleteCommand = new RelayCommand(_ => ExecuteDeleteConnection());
            CancelDeleteCommand = new RelayCommand(_ => IsDeleteConfirmationVisible = false);
            UndoDeleteCommand = new RelayCommand(_ => UndoDelete());
            ToggleGroupCommand = new RelayCommand(ToggleGroup);
            ShowAddGroupCommand = new RelayCommand(_ => { IsAddGroupVisible = true; NewGroupName = ""; });
            CreateGroupCommand = new RelayCommand(_ => CreateGroup());
            CancelAddGroupCommand = new RelayCommand(_ => IsAddGroupVisible = false);
            RefreshStatusCommand = new RelayCommand(_ => ExecuteRefreshStatus());
            CloseTipCommand = new RelayCommand(_ => IsTipVisible = false);
            GoHomeCommand = new RelayCommand(_ => SelectedConnection = null);
            MoveUpCommand = new RelayCommand(p => MoveConnection(true, p), p => CanMove(true, p));
            MoveDownCommand = new RelayCommand(p => MoveConnection(false, p), p => CanMove(false, p));

            RebuildGroups();
            SubscribeToConnectionChanges();

            // Start ping service
            if (_appSettings.CheckOnlineStatus)
            {
                _pingService.Start(
                    new List<RdpConnection>(Connections),
                    _appSettings.PingIntervalSeconds);
            }
        }

        #region Group Management

        public void RebuildGroups()
        {
            var filtered = Connections.AsEnumerable<RdpConnection>();

            // Ensure SortIndex is initialized for all
            int maxIdx = Connections.Any() ? Connections.Max(c => c.SortIndex) : 0;
            foreach (var c in Connections.Where(x => x.SortIndex == 0))
            {
                c.SortIndex = ++maxIdx;
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(c =>
                    (c.Name != null && c.Name.ToLowerInvariant().Contains(search)) ||
                    (c.Host != null && c.Host.ToLowerInvariant().Contains(search)) ||
                    (c.Group != null && c.Group.ToLowerInvariant().Contains(search)));
            }
            
            // Sort
            filtered = filtered.OrderBy(c => c.SortIndex);
            OnPropertyChanged("SortedConnections");

            // Apply type filter
            if (!string.Equals(FilterType, "Tous", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(c => string.Equals(c.ConnectionType, FilterType, StringComparison.OrdinalIgnoreCase));
            }

            // Group by Group name
            var groupDict = new Dictionary<string, ConnectionGroup>(StringComparer.OrdinalIgnoreCase);
            foreach (var conn in filtered)
            {
                string groupName = string.IsNullOrWhiteSpace(conn.Group) ? "Défaut" : conn.Group;
                if (!groupDict.ContainsKey(groupName))
                {
                    // Preserve expansion state
                    var existing = Groups.FirstOrDefault(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
                    groupDict[groupName] = new ConnectionGroup
                    {
                        Name = groupName,
                        IsExpanded = existing != null ? existing.IsExpanded : true
                    };
                }
                groupDict[groupName].Connections.Add(conn);
            }

            Groups.Clear();
            foreach (var kvp in groupDict.OrderBy(g => g.Key))
            {
                Groups.Add(kvp.Value);
            }
        }

        private void ToggleGroup(object parameter)
        {
            var group = parameter as ConnectionGroup;
            if (group != null)
            {
                group.IsExpanded = !group.IsExpanded;
            }
        }

        private bool CanMove(bool up, object parameter = null)
        {
            var target = parameter as RdpConnection ?? SelectedConnection;
            if (target == null) return false;

            var groupConnections = Connections
                .Where(c => c.Group == target.Group)
                .OrderBy(c => c.SortIndex)
                .ToList();
            
            int idx = groupConnections.IndexOf(target);
            if (up) return idx > 0;
            return idx < groupConnections.Count - 1 && idx != -1;
        }

        private void MoveConnection(bool up, object parameter = null)
        {
            var targetConn = parameter as RdpConnection ?? SelectedConnection;
            if (targetConn == null) return;
            
            var groupConnections = Connections
                .Where(c => c.Group == targetConn.Group)
                .OrderBy(c => c.SortIndex)
                .ToList();

            int idx = groupConnections.IndexOf(targetConn);
            int targetIdx = up ? idx - 1 : idx + 1;

            if (targetIdx >= 0 && targetIdx < groupConnections.Count)
            {
                var other = groupConnections[targetIdx];
                int temp = targetConn.SortIndex;
                targetConn.SortIndex = other.SortIndex;
                other.SortIndex = temp;

                RebuildGroups();
                SaveChanges(false);
            }
        }

        private void CreateGroup()
        {
            if (!string.IsNullOrWhiteSpace(NewGroupName))
            {
                // Group will appear when a connection is assigned to it
                // For now, just add a dummy entry and rebuild
                var newConn = new RdpConnection
                {
                    Name = "Nouvelle Connexion",
                    Group = NewGroupName,
                    ConnectionType = "RDP"
                };
                Connections.Add(newConn);
                SelectedConnection = newConn;
                IsAddGroupVisible = false;
                SaveChanges(false);
                RebuildGroups();
            }
        }

        #endregion

        private void SubscribeToConnectionChanges()
        {
            foreach (var c in Connections)
            {
                c.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "IsOnline")
                    {
                        OnPropertyChanged("OnlineConnections");
                        OnPropertyChanged("OfflineConnections");
                    }
                    else if (e.PropertyName == "ConnectionType" || e.PropertyName == "Group" || e.PropertyName == "Name")
                    {
                        RebuildGroups();
                    }
                };
            }
            
            Connections.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged("TotalConnections");
                OnPropertyChanged("OnlineConnections");
                OnPropertyChanged("OfflineConnections");
                if (e.NewItems != null)
                {
                    foreach (RdpConnection item in e.NewItems)
                    {
                        item.PropertyChanged += (s2, e2) =>
                        {
                            if (e2.PropertyName == "IsOnline")
                            {
                                OnPropertyChanged("OnlineConnections");
                                OnPropertyChanged("OfflineConnections");
                            }
                        };
                    }
                }
            };
        }

        #region Connection CRUD

        private void AddConnection()
        {
            string group = "Défaut";
            // If a connection is selected, use its group
            if (SelectedConnection != null)
                group = SelectedConnection.Group;

            var newConn = new RdpConnection
            {
                Name = "Nouvelle Connexion",
                Group = group,
                ConnectionType = "RDP",
                SortIndex = Connections.Any() ? Connections.Max(c => c.SortIndex) + 1 : 1
            };
            Connections.Add(newConn);
            SelectedConnection = newConn;
            RebuildGroups();
        }

        private void RequestDeleteConnection()
        {
            if (SelectedConnection == null) return;
            DeleteConfirmationMessage = string.Format("Voulez-vous vraiment supprimer « {0} » ?", SelectedConnection.Name);
            IsDeleteConfirmationVisible = true;
        }

        private void ExecuteDeleteConnection()
        {
            if (SelectedConnection != null)
            {
                _lastDeletedConnection = SelectedConnection;
                _lastDeletedIndex = Connections.IndexOf(SelectedConnection);

                Connections.Remove(SelectedConnection);
                SelectedConnection = null;

                SaveChanges(false);
                RebuildGroups();

                ShowNotification(string.Format("{0} supprimé", _lastDeletedConnection.Name), true, 10);
            }
            IsDeleteConfirmationVisible = false;
        }

        private void UndoDelete()
        {
            if (_lastDeletedConnection != null)
            {
                if (_lastDeletedIndex >= 0 && _lastDeletedIndex <= Connections.Count)
                    Connections.Insert(_lastDeletedIndex, _lastDeletedConnection);
                else
                    Connections.Add(_lastDeletedConnection);

                SelectedConnection = _lastDeletedConnection;
                _lastDeletedConnection = null;

                IsNotificationVisible = false;
                if (_notificationCts != null)
                    _notificationCts.Cancel();

                SaveChanges(false);
                RebuildGroups();
            }
        }

        private void SaveChanges(bool showNotification = true)
        {
            _rdpService.SaveConnections(new List<RdpConnection>(Connections));
            _pingService.UpdateConnections(new List<RdpConnection>(Connections));

            if (showNotification)
            {
                ShowNotification("Enregistré !", false);
            }
        }

        private void Connect(object parameter = null)
        {
            var target = parameter as RdpConnection ?? SelectedConnection;
            if (target == null) return;
            
            SaveChanges(false);

            try
            {
                _rdpService.LaunchSession(target);
                SaveChanges(false); // Save LastConnected
                OnPropertyChanged("RecentConnections");
                ShowNotification(string.Format("Connexion à {0}...", target.Name), false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Erreur de connexion: {0}", ex.Message),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Settings

        private void ExecuteRefreshStatus()
        {
            ShowNotification("Vérification des statuts en cours...", false, 3);
            _pingService.UpdateConnections(new List<RdpConnection>(Connections));
            _pingService.TriggerRefresh();
            
            // Force update stats after a short delay
            Task.Run(async () => {
                await Task.Delay(2500); 
                OnPropertyChanged("OnlineConnections");
                OnPropertyChanged("OfflineConnections");
            });
        }

        private void ExecuteSaveSettings()
        {
            SetStartWithWindows(StartWithWindows);
            _rdpService.SaveSettings(_appSettings);

            // Update ping service
            if (_appSettings.CheckOnlineStatus)
            {
                _pingService.Stop();
                _pingService.Start(
                    new List<RdpConnection>(Connections),
                    _appSettings.PingIntervalSeconds);
            }
            else
            {
                _pingService.Stop();
            }

            ShowNotification("Paramètres enregistrés", false);
            IsSettingsVisible = false;
        }

        private void SetStartWithWindows(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            if (!path.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                            {
                                key.SetValue("RDPManagerPro", path);
                            }
                        }
                        else
                        {
                            key.DeleteValue("RDPManagerPro", false);
                        }
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Notifications

        private void ShowNotification(string message, bool isUndoable, int durationSeconds = 5)
        {
            if (_notificationCts != null)
                _notificationCts.Cancel();
            _notificationCts = new CancellationTokenSource();

            NotificationMessage = message;
            IsUndoVisible = isUndoable;
            IsNotificationVisible = true;

            var token = _notificationCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(durationSeconds * 1000, token);
                    if (!token.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsNotificationVisible = false;
                            IsUndoVisible = false;
                            _lastDeletedConnection = null;
                        });
                    }
                }
                catch (TaskCanceledException) { }
            });
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            
            // Auto-update stats when status or collection changes
            if (name == "IsOnline" || name == "Connections")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalConnections"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OnlineConnections"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OfflineConnections"));
            }
        }

        public void Dispose()
        {
            _pingService.Dispose();
        }
    }
}
