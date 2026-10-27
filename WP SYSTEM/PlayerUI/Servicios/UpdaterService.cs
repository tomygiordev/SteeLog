using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace WP_Inventario.Servicios
{
    public class UpdateInfo
    {
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleaseNotes { get; set; }
    }

    public static class UpdaterService
    {
        private const string Owner = "tomygiordev";
        private const string Repo = "SteeLog";
        private const string Token = "";
        private const string ApiUrl = "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases/latest";

        public static async Task<UpdateInfo> CheckForUpdateAsync(bool throwOnError = false)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    client.DefaultRequestHeaders.Add("User-Agent", "SteeLog-Updater");
                    if (!string.IsNullOrWhiteSpace(Token)) client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                    var response = await client.GetStringAsync(ApiUrl);

                    using (var json = JsonDocument.Parse(response))
                    {
                        var root = json.RootElement;

                        string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
                        string remoteVersion = tagName.TrimStart('v');
                        string body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

                        string downloadUrl = null;
                        if (root.TryGetProperty("assets", out var assets))
                        {
                            foreach (var asset in assets.EnumerateArray())
                            {
                                if (asset.TryGetProperty("name", out var nameProp))
                                {
                                    string aName = nameProp.GetString();
                                    if (aName == "SteeLog.exe" || aName == "WP_Inventario.exe")
                                    {
                                        downloadUrl = asset.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                                        break;
                                    }
                                }
                            }
                        }

                        if (downloadUrl == null || !IsNewerVersion(remoteVersion, AppVersion.Current))
                            return null;

                        return new UpdateInfo
                        {
                            Version = remoteVersion,
                            DownloadUrl = downloadUrl,
                            ReleaseNotes = body
                        };
                    }
                }
            }
            catch (Exception)
            {
                if (throwOnError)
                    throw;
                // Entorno offline / local de tesis
                return null;
            }
        }

        public static async Task DownloadAndInstallAsync(UpdateInfo update, IProgress<int> progress)
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string tempDir = Path.GetTempPath();
            string tempExe = Path.Combine(tempDir, "SteeLog_new.exe");
            string updaterScript = Path.Combine(tempDir, "steelog_updater.ps1");
            int currentPid = Process.GetCurrentProcess().Id;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "SteeLog-Updater");
                if (!string.IsNullOrWhiteSpace(Token)) client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);
                client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");

                var response = await client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempExe, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    long downloaded = 0;
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloaded += bytesRead;
                        if (totalBytes.HasValue && progress != null)
                            progress.Report((int)(downloaded * 100 / totalBytes.Value));
                    }
                }
            }

            // PS1 en %TEMP%: espera que el PID muera, reemplaza el exe y relanza
            string src = tempExe.Replace("'", "''");
            string dst = exePath.Replace("'", "''");
            string scriptContent =
                "$pidTarget = " + currentPid + "\r\n" +
                "while (Get-Process -Id $pidTarget -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 300 }\r\n" +
                "Start-Sleep -Seconds 1\r\n" +
                "$success = $false\r\n" +
                "for ($i=0; $i -lt 15; $i++) {\r\n" +
                "    try {\r\n" +
                "        Copy-Item -Path '" + src + "' -Destination '" + dst + "' -Force -ErrorAction Stop\r\n" +
                "        $success = $true\r\n" +
                "        break\r\n" +
                "    } catch {\r\n" +
                "        Start-Sleep -Milliseconds 500\r\n" +
                "    }\r\n" +
                "}\r\n" +
                "Remove-Item '" + src + "' -Force -ErrorAction SilentlyContinue\r\n" +
                "if ($success) { Start-Process -FilePath '" + dst + "' -WorkingDirectory '" + Path.GetDirectoryName(dst) + "' }\r\n" +
                "else { Start-Process -FilePath '" + dst + "' -WorkingDirectory '" + Path.GetDirectoryName(dst) + "' }\r\n" +
                "Remove-Item $PSCommandPath -Force -ErrorAction SilentlyContinue\r\n";

            File.WriteAllText(updaterScript, scriptContent, System.Text.Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-ExecutionPolicy Bypass -NonInteractive -WindowStyle Hidden -File \"" + updaterScript + "\"",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            });

            System.Windows.Forms.Application.Exit();
        }

        private static bool IsNewerVersion(string remote, string current)
        {
            try
            {
                return new Version(remote) > new Version(current);
            }
            catch
            {
                return false;
            }
        }
    }
}
