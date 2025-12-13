using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;
using TaseDreams.Api.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaseDreams.Api.Services;

public class OCPService : IOCPService
{
    private readonly OCPConfig _config;
    private Kubernetes? _kubernetes;
    private readonly object _lock = new object();

    public OCPService(IOptions<OCPConfig> config)
    {
        _config = config.Value;
    }

    private Kubernetes GetKubernetesClient()
    {
        if (_kubernetes == null)
        {
            lock (_lock)
            {
                if (_kubernetes == null)
                {
                    try
                    {
                        KubernetesClientConfiguration k8sConfig;
                        
                        // Try to build from config file if available
                        if (File.Exists("/root/.kube/config") || File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kube", "config")))
                        {
                            k8sConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                        }
                        else if (!string.IsNullOrEmpty(_config.ApiUrl) && !string.IsNullOrEmpty(_config.Token))
                        {
                            // Use API URL and token from configuration
                            k8sConfig = new KubernetesClientConfiguration
                            {
                                Host = _config.ApiUrl,
                                AccessToken = _config.Token,
                                SkipTlsVerify = true // For development, adjust as needed
                            };
                        }
                        else
                        {
                            // Create a dummy config that will fail gracefully when used
                            k8sConfig = new KubernetesClientConfiguration
                            {
                                Host = "https://kubernetes.default.svc"
                            };
                        }
                        
                        _kubernetes = new Kubernetes(k8sConfig);
                    }
                    catch (Exception)
                    {
                        // If initialization fails, create a dummy client that will fail gracefully
                        _kubernetes = new Kubernetes(new KubernetesClientConfiguration
                        {
                            Host = "https://kubernetes.default.svc"
                        });
                    }
                }
            }
        }
        return _kubernetes;
    }

    public async Task<bool> CreateNamespaceAsync(string projectName, string developmentTeam)
    {
        try
        {
            var namespaceName = $"oc-{projectName}";
            var ns = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = namespaceName,
                    Labels = new Dictionary<string, string>
                    {
                        { "project", projectName },
                        { "team", developmentTeam },
                        { "managed-by", "tasedreams" }
                    }
                }
            };

            await GetKubernetesClient().CreateNamespaceAsync(ns);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> GrantAdminPermissionsAsync(string projectName, string developmentTeam)
    {
        try
        {
            var namespaceName = $"oc-{projectName}";
            
            // Create RoleBinding using CustomObjects API to avoid V1Subject type issues
            var roleBindingObj = new Dictionary<string, object>
            {
                ["apiVersion"] = "rbac.authorization.k8s.io/v1",
                ["kind"] = "RoleBinding",
                ["metadata"] = new Dictionary<string, object>
                {
                    ["name"] = $"{developmentTeam}-admin",
                    ["namespace"] = namespaceName
                },
                ["roleRef"] = new Dictionary<string, object>
                {
                    ["apiGroup"] = "rbac.authorization.k8s.io",
                    ["kind"] = "ClusterRole",
                    ["name"] = "admin"
                },
                ["subjects"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["kind"] = "Group",
                        ["name"] = developmentTeam,
                        ["apiGroup"] = "rbac.authorization.k8s.io"
                    }
                }
            };

            await GetKubernetesClient().CreateNamespacedCustomObjectAsync(
                roleBindingObj,
                "rbac.authorization.k8s.io",
                "v1",
                namespaceName,
                "rolebindings",
                $"{developmentTeam}-admin"
            );
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VerifyTektonBuildAsync(string projectName)
    {
        try
        {
            var namespaceName = $"oc-{projectName}";
            
            // Check for Tekton PipelineRun resources using generic API (CRD)
            var group = "tekton.dev";
            var version = "v1beta1";
            var plural = "pipelineruns";
            
            var response = await GetKubernetesClient().ListNamespacedCustomObjectAsync(
                group, version, namespaceName, plural,
                labelSelector: $"project={projectName}"
            );

            var responseJson = JsonSerializer.Serialize(response);
            var jsonDoc = JsonDocument.Parse(responseJson);
            var items = jsonDoc.RootElement.GetProperty("items");
            
            if (items.GetArrayLength() == 0)
                return false;

            // Find the latest run by creation timestamp
            var latestRun = items.EnumerateArray()
                .OrderByDescending(item => 
                {
                    if (item.TryGetProperty("metadata", out var metadata) &&
                        metadata.TryGetProperty("creationTimestamp", out var timestamp))
                    {
                        return timestamp.GetString();
                    }
                    return string.Empty;
                })
                .FirstOrDefault();

            // Check if pipeline completed successfully
            if (latestRun.TryGetProperty("status", out var status) &&
                status.TryGetProperty("conditions", out var conditions))
            {
                foreach (var condition in conditions.EnumerateArray())
                {
                    if (condition.TryGetProperty("type", out var type) &&
                        type.GetString() == "Succeeded" &&
                        condition.TryGetProperty("status", out var conditionStatus))
                    {
                        return conditionStatus.GetString() == "True";
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> AllocateNodePortAsync(string projectName, string serviceName)
    {
        try
        {
            // Get existing NodePort allocations
            var services = await GetKubernetesClient().ListServiceForAllNamespacesAsync();
            var nodePorts = services.Items
                .Where(s => s.Spec.Type == "NodePort")
                .SelectMany(s => s.Spec.Ports)
                .Where(p => p.NodePort.HasValue)
                .Select(p => p.NodePort!.Value)
                .ToList();

            // Find next available port (starting from 30000, which is Kubernetes default)
            int nextPort = 30000;
            while (nodePorts.Contains(nextPort))
            {
                nextPort++;
            }

            return nextPort;
        }
        catch
        {
            return 0;
        }
    }
}

