using System.Linq;
using System.Threading.Tasks;
using k8s;

namespace WslPortForwarder
{
    public class KubernetesService
    {
        private readonly Kubernetes _client;

        public KubernetesService()
        {
            var kubeConfig = KubernetesClientConfiguration.LoadKubeConfig();
            kubeConfig.CurrentContext = "rancher-desktop";
            var kubernetesClientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigObject(kubeConfig);
            _client = new Kubernetes(kubernetesClientConfiguration);
        }

        public async Task<string> GetNodeIp()
        {
            var nodes = await _client.ListNodeAsync();
            var node = nodes.Items.First();
            var address = node.Status.Addresses.First(a => a.Type == "InternalIP");
            return address.Address;
        }
    }
}