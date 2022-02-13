using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WslPortForwarder
{
    public sealed class WindowsBackgroundService : BackgroundService
    {
        private readonly WslService _wslService;
        private readonly ILogger<WindowsBackgroundService> _logger;

        public WindowsBackgroundService(WslService wslService, ILogger<WindowsBackgroundService> logger) =>
            (_wslService, _logger) = (wslService, logger);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var ip = _wslService.GetWslNetworkIpAddress();
                var ports = _wslService.GetDockerPorts();

                _logger.LogInformation("Ip: {0}, Ports: {1}", ip, string.Join(",", ports));

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}