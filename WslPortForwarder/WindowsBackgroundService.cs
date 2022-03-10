using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private readonly IList<Thread> _threads = new List<Thread>();

        public WindowsBackgroundService(KubernetesService kubernetesService, DockerService dockerService, ILogger<WindowsBackgroundService> logger) =>
            (_kubernetesService, _dockerService, _logger) = (kubernetesService, dockerService, logger);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ip = await _kubernetesService.GetNodeIp();
                var ports = await _dockerService.GetPublicPortForContainers();

                _logger.LogInformation("Ip: {0}, Ports: {1}", ip, string.Join(",", ports));

                foreach (var port in ports)
                {
                    if (IsPortCreated(port))
                    {
                        _logger.LogInformation("Skipping port: {0}", port);
                        continue;
                    }

                    _logger.LogInformation("Adding port: {0}", port);
                    var tcpForwarder = new TcpForwarder();
                    var srcEndpoint = new IPEndPoint(IPAddress.Any, port);
                    var dstEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
                    var thread = new Thread(() => tcpForwarder.Start(srcEndpoint, dstEndpoint))
                    {
                        IsBackground = true,
                        Name = $"WslPort-{port}"
                    };
                    _threads.Add(thread);
                    thread.Start();
                }

                var unusedPorts = GetRemovedPorts(ports);
                foreach (var unusedPort in unusedPorts)
                {
                    _logger.LogInformation("Removing port: {0}", unusedPort);
                    var thread = ClosePort(unusedPort);
                    if (thread is not null)
                    {
                        _threads.Remove(thread);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        private bool IsPortCreated(ushort port)
        {
            var portName = $"WslPort-{port}";
            return _threads.Any(t => t.Name == portName);
        }

        private ushort[] GetRemovedPorts(ushort[] currentPorts)
        {
            var openPorts = _threads.Select(t => ushort.Parse(t.Name?.Split("-").Last() ?? "0")).Where(p => p != 0);
            return openPorts.Except(currentPorts).ToArray();
        }

        private Thread? ClosePort(ushort port)
        {
            var portName = $"WslPort-{port}";
            foreach (var thread in _threads)
            {
                if (thread.Name != portName) continue;
                thread.Interrupt();
                return thread;
            }

            return null;
        }
    }
}