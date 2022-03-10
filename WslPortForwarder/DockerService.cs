using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace WslPortForwarder
{
    public class DockerService
    {
        private readonly DockerClient _client;

        public DockerService()
        {
            _client = new DockerClientConfiguration().CreateClient();
        }

        public async Task<ushort[]> GetPublicPortForContainers()
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters());
            var filteredContainers = containers.Where(c => !c.Names.Any(cn => cn.StartsWith("/k8s")));
            return filteredContainers.SelectMany(fc => fc.Ports.Select(p => p.PublicPort)).Where(p => p != 0).Distinct().ToArray();
        }
    }
}