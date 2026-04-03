using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RDPManager.Models;

namespace RDPManager.Services
{
    public class RdpService
    {
        private readonly string _storagePath;
        private readonly string _settingsPath;

        public RdpService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RDPManagerPro");
            Directory.CreateDirectory(folder);
            _storagePath = Path.Combine(folder, "connections.json");
            _settingsPath = Path.Combine(folder, "settings.json");
        }

        public List<RdpConnection> LoadConnections()
        {
            if (!File.Exists(_storagePath)) return new List<RdpConnection>();
            try
            {
                var json = File.ReadAllText(_storagePath);
                return JsonConvert.DeserializeObject<List<RdpConnection>>(json) ?? new List<RdpConnection>();
            }
            catch
            {
                return new List<RdpConnection>();
            }
        }

        public void SaveConnections(List<RdpConnection> connections)
        {
            var json = JsonConvert.SerializeObject(connections, Formatting.Indented);
            File.WriteAllText(_storagePath, json);
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsPath)) return new AppSettings();
            try
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }

        public void LaunchSession(RdpConnection connection)
        {
            if (string.IsNullOrWhiteSpace(connection.Host)) return;

            if (connection.ConnectionType == "SSH")
            {
                LaunchSshSession(connection);
                return;
            }

            var rdpContent = new StringBuilder();

            // Core address with port
            string address = connection.Host;
            if (connection.Port != 3389)
                address = string.Format("{0}:{1}", connection.Host, connection.Port);
            rdpContent.AppendLine(string.Format("full address:s:{0}", address));

            // Username
            if (!string.IsNullOrWhiteSpace(connection.Username))
                rdpContent.AppendLine(string.Format("username:s:{0}", connection.Username));

            // Display
            rdpContent.AppendLine(string.Format("use multimon:i:{0}", connection.UseMultimon ? 1 : 0));
            rdpContent.AppendLine(string.Format("screen mode id:i:{0}", connection.FullScreen ? 2 : 1));

            // Resources
            rdpContent.AppendLine(string.Format("redirectclipboard:i:{0}", connection.RedirectClipboard ? 1 : 0));
            rdpContent.AppendLine(string.Format("redirectprinters:i:{0}", connection.RedirectPrinters ? 1 : 0));
            rdpContent.AppendLine(string.Format("drivestoredirect:s:{0}", connection.RedirectDrives ? "*" : ""));

            var tempFile = Path.Combine(Path.GetTempPath(), string.Format("rdp_manager_{0}.rdp", connection.Id));
            File.WriteAllText(tempFile, rdpContent.ToString());

            Process.Start(new ProcessStartInfo
            {
                FileName = "mstsc.exe",
                Arguments = string.Format("\"{0}\"", tempFile),
                UseShellExecute = false
            });

            // Update last connected
            connection.LastConnected = DateTime.Now;
        }

        private void LaunchSshSession(RdpConnection connection)
        {
            var args = new StringBuilder();
            
            if (connection.Port != 22 && connection.Port > 0)
                args.AppendFormat("-p {0} ", connection.Port);

            if (!string.IsNullOrWhiteSpace(connection.Username))
                args.AppendFormat("{0}@{1}", connection.Username, connection.Host);
            else
                args.Append(connection.Host);

            // On Windows, ssh.exe is built-in. We launch it in a new command prompt window.
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = string.Format("/c start cmd.exe /k ssh {0}", args.ToString()),
                UseShellExecute = false
            });

            connection.LastConnected = DateTime.Now;
        }
    }
}
