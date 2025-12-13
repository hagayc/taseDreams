using Microsoft.Extensions.Options;
using TaseDreams.Api.Configuration;
using LibGit2Sharp;
using System.Text;

namespace TaseDreams.Api.Services;

public class ArgoCDService : IArgoCDService
{
    private readonly ArgoCDConfig _config;

    public ArgoCDService(IOptions<ArgoCDConfig> config)
    {
        _config = config.Value;
    }

    public async Task<bool> UpdateArgoCDConfigAsync(string projectName, string projectKey, string backendLanguage, string frontendFramework)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var repoUrl = _config.ConfigRepoUrl;

            // Clone the repository
            Repository.Clone(repoUrl, tempPath, new CloneOptions
            {
                CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                {
                    Username = _config.Username,
                    Password = _config.Password
                }
            });

            // Ensure directories exist
            var applicationsDir = Path.Combine(tempPath, "applications");
            var projectsDir = Path.Combine(tempPath, "projects");
            Directory.CreateDirectory(applicationsDir);
            Directory.CreateDirectory(projectsDir);

            // Update ArgoCD application configuration
            var appConfigPath = Path.Combine(applicationsDir, $"{projectName}.yaml");
            var appConfig = GenerateArgoCDApplicationConfig(projectName, projectKey, backendLanguage, frontendFramework);
            await File.WriteAllTextAsync(appConfigPath, appConfig);

            // Update project configuration if needed
            var projectConfigPath = Path.Combine(projectsDir, $"{projectName}-project.yaml");
            var projectConfig = GenerateArgoCDProjectConfig(projectName, projectKey);
            await File.WriteAllTextAsync(projectConfigPath, projectConfig);

            // Commit and push changes
            using (var repo = new Repository(tempPath))
            {
                Commands.Stage(repo, "*");

                var author = new Signature("TaseDreams", "tasedreams@system.local", DateTimeOffset.Now);
                var committer = author;

                repo.Commit($"Add ArgoCD configuration for {projectName}", author, committer);

                var remote = repo.Network.Remotes["origin"];
                repo.Network.Push(remote, $"refs/heads/{_config.ConfigRepoBranch}", new PushOptions
                {
                    CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                    {
                        Username = _config.Username,
                        Password = _config.Password
                    }
                });
            }

            // Cleanup
            Directory.Delete(tempPath, true);

            return true;
        }
        catch
        {
            // Log error
            return false;
        }
    }

    private string GenerateArgoCDApplicationConfig(string projectName, string projectKey, string backendLanguage, string frontendFramework)
    {
        var sb = new StringBuilder();
        sb.AppendLine("apiVersion: argoproj.io/v1alpha1");
        sb.AppendLine("kind: Application");
        sb.AppendLine("metadata:");
        sb.AppendLine($"  name: {projectName}");
        sb.AppendLine($"  namespace: argocd");
        sb.AppendLine("spec:");
        sb.AppendLine("  project: default");
        sb.AppendLine("  source:");
        sb.AppendLine($"    repoURL: {_config.ConfigRepoUrl.Replace("argocd-config", $"{projectKey}/{projectName}-cd")}");
        sb.AppendLine("    targetRevision: main");
        sb.AppendLine("    path: k8s");
        sb.AppendLine("  destination:");
        sb.AppendLine($"    server: https://kubernetes.default.svc");
        sb.AppendLine($"    namespace: oc-{projectName}");
        sb.AppendLine("  syncPolicy:");
        sb.AppendLine("    automated:");
        sb.AppendLine("      prune: true");
        sb.AppendLine("      selfHeal: true");
        return sb.ToString();
    }

    private string GenerateArgoCDProjectConfig(string projectName, string projectKey)
    {
        var sb = new StringBuilder();
        sb.AppendLine("apiVersion: argoproj.io/v1alpha1");
        sb.AppendLine("kind: AppProject");
        sb.AppendLine("metadata:");
        sb.AppendLine($"  name: {projectName}");
        sb.AppendLine("spec:");
        sb.AppendLine("  description: Project created by TaseDreams");
        sb.AppendLine("  sourceRepos:");
        sb.AppendLine($"  - '{_config.ConfigRepoUrl.Replace("argocd-config", projectKey)}'");
        sb.AppendLine("  destinations:");
        sb.AppendLine($"  - namespace: oc-{projectName}");
        sb.AppendLine("    server: https://kubernetes.default.svc");
        return sb.ToString();
    }
}

