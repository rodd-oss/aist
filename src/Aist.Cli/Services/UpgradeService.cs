using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Aist.Core;
using Spectre.Console;

namespace Aist.Cli.Services;

internal sealed class UpgradeService : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string Repo = "rodd-oss/aist";

    public UpgradeService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("aist-cli", "1.0.0"));
    }

    public async Task UpgradeAsync(string? requestedVersion = null)
    {
        var currentVersion = GetCurrentVersion();
        AnsiConsole.MarkupLine($"[blue]Current version:[/] {currentVersion}");

        var release = await GetReleaseAsync(requestedVersion).ConfigureAwait(false);
        if (release == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not find the specified release.");
            return;
        }

        // Compare versions (handling 'v' prefix in TagName)
        var isLatest = release.TagName == currentVersion || release.TagName == $"v{currentVersion}";
        if (isLatest && requestedVersion == null)
        {
            AnsiConsole.MarkupLine("[green]You are already on the latest version.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[blue]Upgrading to:[/] {release.TagName}");

        var asset = GetAssetForPlatform(release.Assets);
        if (asset == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not find a compatible binary for your platform.");
            return;
        }

        await DownloadAndInstallAsync(asset.BrowserDownloadUrl, release.TagName).ConfigureAwait(false);
    }

    private static string GetCurrentVersion()
    {
        return typeof(UpgradeService).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private async Task<GitHubRelease?> GetReleaseAsync(string? version)
    {
        var url = string.IsNullOrEmpty(version)
            ? $"https://api.github.com/repos/{Repo}/releases/latest"
            : $"https://api.github.com/repos/{Repo}/releases/tags/{version}";

        try
        {
            return await _httpClient.GetFromJsonAsync(url, AistJsonContext.Default.GitHubRelease).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error fetching release:[/] {ex.Message}");
            return null;
        }
    }

    private static GitHubAsset? GetAssetForPlatform(IReadOnlyCollection<GitHubAsset> assets)
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "linux";
        
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => null
        };

        if (arch == null) return null;

        var suffix = $"{os}-{arch}";
        return assets.FirstOrDefault(a => a.Name.Contains(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private async Task DownloadAndInstallAsync(Uri downloadUrl, string version)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "aist-upgrade");
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        var fileName = Path.GetFileName(downloadUrl.LocalPath);
        var downloadPath = Path.Combine(tempDir, fileName);

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Downloading upgrade...[/]");
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var downloadStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                var totalRead = 0L;
                int read;
                while ((read = await downloadStream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                    totalRead += read;
                    if (totalBytes != -1)
                    {
                        task.Value = (double)totalRead / totalBytes * 100;
                    }
                }
            }).ConfigureAwait(false);

        AnsiConsole.MarkupLine("[green]Extracting...[/]");
        var extractPath = Path.Combine(tempDir, "extract");
        Directory.CreateDirectory(extractPath);

        if (fileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            using var fs = File.OpenRead(downloadPath);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(gz, extractPath, true).ConfigureAwait(false);
        }
        else if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            await ZipFile.ExtractToDirectoryAsync(downloadPath, extractPath, true).ConfigureAwait(false);
        }

        var binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "aist.exe" : "aist";
        var newBinaryPath = Path.Combine(extractPath, binaryName);

        if (!File.Exists(newBinaryPath))
        {
            // Try searching for it if it's nested
            var found = Directory.GetFiles(extractPath, binaryName, SearchOption.AllDirectories).FirstOrDefault();
            if (found != null) newBinaryPath = found;
        }

        if (!File.Exists(newBinaryPath))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not find binary in the downloaded archive.");
            return;
        }

        var currentBinaryPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentBinaryPath))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not determine current binary path.");
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await InstallWindowsAsync(newBinaryPath, currentBinaryPath).ConfigureAwait(false);
        }
        else
        {
            InstallUnix(newBinaryPath, currentBinaryPath);
        }

        AnsiConsole.MarkupLine($"[green]Successfully upgraded to {version}![/]");
    }

    private static void InstallUnix(string newBinaryPath, string currentBinaryPath)
    {
        // On Unix, we can just move the file even if it's running (it replaces the inode entry)
        File.Move(newBinaryPath, currentBinaryPath, true);
        
        // Ensure it's executable
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            SetExecutablePermission(currentBinaryPath);
        }
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
    private static void SetExecutablePermission(string path)
    {
        try
        {
            File.SetUnixFileMode(path, 
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        catch (IOException)
        {
            // Ignore if we can't set permissions
        }
    }

    private static async Task InstallWindowsAsync(string newBinaryPath, string currentBinaryPath)
    {
        var backupPath = currentBinaryPath + ".bak";
        var batchPath = Path.Combine(Path.GetTempPath(), "aist-upgrade.bat");

        File.Move(currentBinaryPath, backupPath, true);
        File.Move(newBinaryPath, currentBinaryPath, true);

        var batchContent = $@"
@echo off
timeout /t 1 /nobreak > nul
del ""{backupPath}""
del ""%~f0""
";
        await File.WriteAllTextAsync(batchPath, batchContent).ConfigureAwait(false);
        
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batchPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
