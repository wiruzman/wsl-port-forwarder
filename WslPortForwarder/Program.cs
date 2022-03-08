using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WslPortForwarder
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new DockerClientConfiguration()
                .CreateClient();

            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters());
            var filteredContainers = containers.Where(c => !c.Names.Any(cn => cn.StartsWith("/k8s")));
            var filteredContainersPorts =
                filteredContainers.SelectMany(fc => fc.Ports.Select(p => p.PublicPort)).Where(p => p != 0).Distinct();

            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "WSL Port Forwarder";
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<WindowsBackgroundService>();
                    services.AddSingleton<WslService>();
                });
    }
}