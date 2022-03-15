using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WslPortForwarder
{
    public sealed class WindowsBackgroundService : BackgroundService
    {
        private readonly KubernetesService _kubernetesService;
        private readonly DockerService _dockerService;
        private readonly ILogger<WindowsBackgroundService> _logger;
        private readonly IList<TcpForwarder> _tcpForwarders = new List<TcpForwarder>();

        public WindowsBackgroundService(KubernetesService kubernetesService, DockerService dockerService, ILogger<WindowsBackgroundService> logger) =>
            (_kubernetesService, _dockerService, _logger) = (kubernetesService, dockerService, logger);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var ip = await _kubernetesService.GetNodeIp();
                    var ports = await _dockerService.GetPublicPortForContainers();

                    _logger.LogDebug("Ip: {0}, Ports: {1}", ip, string.Join(",", ports));

                    foreach (var port in ports)
                    {
                        if (IsPortCreated(port))
                        {
                            _logger.LogDebug("Listening port: {0}", port);
                            continue;
                        }

                        _logger.LogDebug("Adding port: {0}", port);
                        var tcpForwarder = new TcpForwarder(RemovePort, ip, port);
                        var thread = new Thread(() => tcpForwarder.Start())
                        {
                            IsBackground = true,
                            Name = $"WslPort-{port}"
                        };
                        _tcpForwarders.Add(tcpForwarder);
                        thread.Start();
                    }

                    var unusedPorts = GetRemovedPorts(ports);
                    foreach (var unusedPort in unusedPorts)
                    {
                        _logger.LogDebug("Removing port: {0}", unusedPort);
                        var tcpForward = _tcpForwarders.FirstOrDefault(tf => tf.Port == unusedPort);
                        tcpForward?.Stop();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        private bool IsPortCreated(ushort port)
        {
            return _tcpForwarders.Any(tf => tf.Port == port);
        }

        private ushort[] GetRemovedPorts(ushort[] currentPorts)
        {
            var openPorts = _tcpForwarders.Select(tf => tf.Port);
            return openPorts.Except(currentPorts).ToArray();
        }

        private void RemovePort(ushort port)
        {
            var tcpForwarder = _tcpForwarders.FirstOrDefault(tf => tf.Port == port);
            if (tcpForwarder is not null)
            {
                _tcpForwarders.Remove(tcpForwarder);
            }
        }
    }
}