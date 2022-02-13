using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace WslPortForwarder
{
    public class WslService
    {
        private const string WslDistroName = "rancher-desktop";

        public IPAddress GetWslNetworkIpAddress()
        {
            var processStartInfo = new ProcessStartInfo("wsl", $"-d {WslDistroName} sh -c \"ip addr show eth0 | grep 'inet\\\\b' | awk '{{print \\$2}}' | cut -d/ -f1\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit(5000);

            var ip = process.StandardOutput.ReadToEnd().Trim('\n');
            if (!IPAddress.TryParse(ip, out var ipAddress))
            {
                var processError = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"Ip is not valid: {ip}\nProcess error:\n{processError}");
            }

            return ipAddress;
        }

        public IEnumerable<int> GetDockerPorts()
        {
            var processStartInfo = new ProcessStartInfo("wsl", $"-d {WslDistroName} iptables -L DOCKER -n -4")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit(5000);

            var content = process.StandardOutput.ReadToEnd();
            var contentLines = content.Split("\n").Where(x => x.StartsWith("ACCEPT")).Select(x => x[(x.LastIndexOf(":", StringComparison.Ordinal) + 1)..]);
            return contentLines.Select(int.Parse);
        }
    }
}