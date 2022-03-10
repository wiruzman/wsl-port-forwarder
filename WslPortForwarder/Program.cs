using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WslPortForwarder
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
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
                    services.AddSingleton<KubernetesService>();
                    services.AddSingleton<DockerService>();
                });
    }
}