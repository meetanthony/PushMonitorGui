using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public enum GitRepoStatus
{
    Undefined,
    Uncommitted,
    Unpushed,
    Pushed
}

public class RepoStatus
{
    public string Path { get; }
    public GitRepoStatus Status { get; }

    public RepoStatus(string path, GitRepoStatus status)
    {
        Path = path;
        Status = status;
    }
}

public static class PushMonitor
{
    public static List<RepoStatus> CheckReposStatus(string rootPath)
    {
        var repos = FindGitRepos(rootPath);
        var result = new List<RepoStatus>();

        foreach (var repo in repos)
        {
            var status = GetRepoStatus(repo);
            result.Add(new RepoStatus(repo, status));
        }

        return result;
    }

    private static List<string> FindGitRepos(string root)
    {
        var repos = new List<string>();

        void CheckDir(string dir)
        {
            var gitPath = Path.Combine(dir, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                repos.Add(dir);
            }
        }

        // проверяем саму базовую директорию
        CheckDir(root);

        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            CheckDir(dir);
        }

        return repos;
    }

    private static GitRepoStatus GetRepoStatus(string repoPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "status --porcelain --branch",
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            return GitRepoStatus.Undefined;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        bool hasChanges = false;
        bool ahead = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("##"))
            {
                if (line.Contains("ahead"))
                    ahead = true;
            }
            else
            {
                hasChanges = true;
            }
        }

        if (hasChanges)
            return GitRepoStatus.Uncommitted;

        if (ahead)
            return GitRepoStatus.Unpushed;

        return GitRepoStatus.Pushed;
    }
}