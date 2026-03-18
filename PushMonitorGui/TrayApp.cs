using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class Settings
{
    public string[] Folders { get; set; } = [@"C:\repostore", @"D:\repostore"];
    public int UpdatePeriodSeconds { get; set; } = 5;
}

public class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private DateTime _lastRefresh = DateTime.MinValue;

    private string[] _folders = [];
    private const string SettingsFileName = "settings.json";

    public TrayApp()
    {
        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "PushMonitor",
            Icon = _scanIcon
        };
    }

    private void LocateConfig()
    {
        Process.Start("explorer.exe", $"/select,\"{SettingsFileName}\"");
    }

    public bool Init()
    {
        string json;
        Settings? settings;
        try
        {
            if (File.Exists(SettingsFileName) == false)
            {
                var defaultSettingsJson = JsonSerializer.Serialize(new Settings());
                try
                {
                    File.WriteAllLines(SettingsFileName, [defaultSettingsJson]);
                    MessageBox.Show($"The settings file was not found. A default settings file has been created.{Environment.NewLine}" +
                        $"Please set your own settings and restart the program.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    LocateConfig();
                    return false;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "Error writing the default settings file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LocateConfig();
                    return false;
                }
            }
            json = File.ReadAllText(SettingsFileName);
            settings = JsonSerializer.Deserialize<Settings>(json);
            if (settings == null)
                throw new Exception("Error deserializing the settings file.");
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString(), "Error reading the settings file.", MessageBoxButtons.OK, MessageBoxIcon.Error);

            LocateConfig();
            return false;
        }

        _folders = settings.Folders;
        _updateIntervalSeconds = settings.UpdatePeriodSeconds;

        var folders = string.Join(Environment.NewLine, _folders);

        _trayIcon.Text = $"PushMonitor{Environment.NewLine}" +
            $"Update interval : {_updateIntervalSeconds} seconds{Environment.NewLine}" +
            $"Scan folders:{Environment.NewLine}" +
            folders;

        UpdateMenu(new List<RepoStatus>());

        Task.Factory.StartNew(RefreshLoop);
        Refresh();

        return true;
    }

    private void Refresh()
    {
        _refreshPending = true;
    }

    private bool _refreshPending = true;
    private int _updateIntervalSeconds = 5;

    private void RefreshLoop()
    {
        while (true)
        {
            Thread.Sleep(1000);

            if (_refreshPending == false && (DateTime.Now - _lastRefresh).TotalSeconds < _updateIntervalSeconds)
                continue;

            _lastRefresh = DateTime.Now;
            _refreshPending = false;

            List<RepoStatus> repos = new List<RepoStatus>();

            foreach (var folder in _folders)
            {
                if (Directory.Exists(folder) == false)
                    continue;

                try
                {
                    var subrepos = PushMonitor.CheckReposStatus(folder);
                    repos.AddRange(subrepos);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    continue;
                }
            }

            GitRepoStatus status = GitRepoStatus.Pushed;
            status = repos.Any(r => r.Status == GitRepoStatus.Unpushed) ? GitRepoStatus.Unpushed :
                     repos.Any(r => r.Status == GitRepoStatus.Uncommitted) ? GitRepoStatus.Uncommitted :
                     GitRepoStatus.Pushed;
            UpdateIcon(status);
            UpdateMenu(repos);
        }
    }

    private Icon _errorIcon = new Icon("icons\\error.ico");
    private Icon _warningIcon = new Icon("icons\\warning.ico");
    private Icon _normalIcon = new Icon("icons\\normal.ico");
    private Icon _scanIcon = new Icon("icons\\scan.ico");

    private void UpdateIcon(GitRepoStatus status)
    {
        switch (status)
        {
            case GitRepoStatus.Uncommitted:
                _trayIcon.Icon = _warningIcon;
                break;

            case GitRepoStatus.Undefined:
            case GitRepoStatus.Unpushed:
                _trayIcon.Icon = _errorIcon;
                break;

            case GitRepoStatus.Pushed:
                _trayIcon.Icon = _normalIcon;
                break;

            default:
                break;
        }
    }

    private void UpdateMenu(List<RepoStatus> repos)
    {
        var menu = new ContextMenuStrip();

        var insertDelimiter = false;
        if (repos.Count > 0)
        {
            foreach (var repo in repos)
            {
                if (repo.Status == GitRepoStatus.Pushed || repo.Status == GitRepoStatus.Undefined)
                    continue;

                insertDelimiter = true;

                var item = new ToolStripMenuItem($"{repo.Path} [{repo.Status}]");

                item.Click += (_, __) => HandleRepoClick(repo);
                menu.Items.Add(item);
            }
        }
        if (insertDelimiter)
            menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Application config", null, (_, __) => LocateConfig());
        menu.Items.Add("Refresh", null, (_, __) => Refresh());
        menu.Items.Add("Exit", null, (_, __) => Exit());

        _trayIcon.ContextMenuStrip = menu;
    }

    private void HandleRepoClick(RepoStatus repo)
    {
        switch (repo.Status)
        {
            case GitRepoStatus.Unpushed:
                RunTortoiseGitPush(repo.Path);
                break;

            case GitRepoStatus.Uncommitted:
                RunTortoiseGitCommit(repo.Path);
                break;

            case GitRepoStatus.Pushed:
                // nothing to do
                break;
        }
    }

    private void RunTortoiseGitPush(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "TortoiseGitProc.exe",
            Arguments = $"/command:sync /path:\"{path}\"",
            UseShellExecute = true
        };

        Process.Start(psi);
    }

    private void RunTortoiseGitCommit(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "TortoiseGitProc.exe",
            Arguments = $"/command:commit /path:\"{path}\"",
            UseShellExecute = true
        };

        Process.Start(psi);
    }

    private void Exit()
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }
}