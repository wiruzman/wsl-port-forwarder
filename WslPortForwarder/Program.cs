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
                    options.ServiceName = ".NET Joke Service";
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<WindowsBackgroundService>();
                    services.AddSingleton<WslService>();
                });
    }
}