using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace RDPManager.Models
{
    public class RdpConnection : INotifyPropertyChanged
    {
        private string _name = "Nouvelle Connexion";
        private string _host = "";
        private string _connectionType = "RDP";
        private string _group = "Défaut";
        private string _username = "";
        private string _description = "";
        private int _port = 3389;
        private bool _isOnline;
        private bool _fullScreen = true;
        private bool _useMultimon;
        private bool _redirectClipboard = true;
        private bool _redirectPrinters;
        private bool _redirectDrives;
        private DateTime _lastConnected;
        private int _sortIndex;

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public int SortIndex
        {
            get { return _sortIndex; }
            set { _sortIndex = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        public string Host
        {
            get { return _host; }
            set { _host = value; OnPropertyChanged(); }
        }

        public string ConnectionType
        {
            get { return _connectionType; }
            set 
            { 
                if (_connectionType != value)
                {
                    _connectionType = value; 
                    if (string.Equals(_connectionType, "SSH", StringComparison.OrdinalIgnoreCase))
                    {
                        Port = 22;
                    }
                    else if (string.Equals(_connectionType, "RDP", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Port == 22) Port = 3389;
                    }
                    OnPropertyChanged(); 
                }
            }
        }

        public string Group
        {
            get { return _group; }
            set { _group = value; OnPropertyChanged(); }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get { return _port; }
            set { _port = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsOnline
        {
            get { return _isOnline; }
            set { _isOnline = value; OnPropertyChanged(); }
        }

        public bool FullScreen
        {
            get { return _fullScreen; }
            set { _fullScreen = value; OnPropertyChanged(); }
        }

        public bool UseMultimon
        {
            get { return _useMultimon; }
            set { _useMultimon = value; OnPropertyChanged(); }
        }

        public bool RedirectClipboard
        {
            get { return _redirectClipboard; }
            set { _redirectClipboard = value; OnPropertyChanged(); }
        }

        public bool RedirectPrinters
        {
            get { return _redirectPrinters; }
            set { _redirectPrinters = value; OnPropertyChanged(); }
        }

        public bool RedirectDrives
        {
            get { return _redirectDrives; }
            set { _redirectDrives = value; OnPropertyChanged(); }
        }

        public DateTime LastConnected
        {
            get { return _lastConnected; }
            set { _lastConnected = value; OnPropertyChanged(); OnPropertyChanged("LastConnectedText"); }
        }

        [JsonIgnore]
        public string LastConnectedText
        {
            get
            {
                if (LastConnected == DateTime.MinValue) return "Jamais";
                var diff = DateTime.Now - LastConnected;
                if (diff.TotalMinutes < 1) return "À l'instant";
                if (diff.TotalHours < 1) return string.Format("{0}min", (int)diff.TotalMinutes);
                if (diff.TotalDays < 1) return string.Format("{0}h", (int)diff.TotalHours);
                return LastConnected.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
