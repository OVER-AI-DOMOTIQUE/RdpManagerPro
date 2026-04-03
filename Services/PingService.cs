using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RDPManager.Models;

namespace RDPManager.Services
{
    public class PingService : IDisposable
    {
        private Timer _timer;
        private List<RdpConnection> _connections;
        private bool _isRunning;

        public void Start(List<RdpConnection> connections, int intervalSeconds)
        {
            _connections = connections;
            _timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
                _timer = null;
            }
        }

        public void UpdateConnections(List<RdpConnection> connections)
        {
            _connections = connections;
        }

        public void TriggerRefresh()
        {
            Task.Run(() => OnTimerElapsed(null));
        }

        public async Task RefreshConnectionAsync(RdpConnection connection)
        {
            if (connection == null || string.IsNullOrWhiteSpace(connection.Host)) return;
            await PingHostAsync(connection);
        }

        private async void OnTimerElapsed(object state)
        {
            if (_isRunning || _connections == null) return;
            _isRunning = true;

            try
            {
                var tasks = new List<Task>();
                var snapshot = _connections.ToArray();
                foreach (var conn in snapshot)
                {
                    if (!string.IsNullOrWhiteSpace(conn.Host))
                    {
                        tasks.Add(PingHostAsync(conn));
                    }
                }
                
                if (tasks.Count > 0)
                    await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PingService Error: " + ex.Message);
            }
            finally
            {
                _isRunning = false;
            }
        }

        private async Task PingHostAsync(RdpConnection connection)
        {
            if (connection == null) return;

            try
            {
                string host = connection.Host;
                int port = connection.Port;
                if (string.IsNullOrWhiteSpace(host)) return;

                // Strip port if present in host field
                if (host.Contains(":"))
                {
                    var parts = host.Split(':');
                    host = parts[0];
                    if (parts.Length > 1 && int.TryParse(parts[1], out int p))
                        port = p;
                }

                bool online = false;
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    try 
                    {
                        // Use a short timeout of 2 seconds for the status check
                        var connectTask = tcp.ConnectAsync(host, port);
                        if (await Task.WhenAny(connectTask, Task.Delay(2000)) == connectTask)
                        {
                            await connectTask;
                            online = tcp.Connected;
                        }
                    }
                    catch { online = false; }
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    connection.IsOnline = online;
                }));
            }
            catch
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    connection.IsOnline = false;
                }));
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
