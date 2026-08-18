using System.IO.Compression;
using System.Net;

namespace KhayratAlhaj.Services
{
    public class QuranPackageInstallerService
    {
        private const string QuranPackageUrlKey = "quran_package_url";
        // Hosted as a GitHub Release asset (the zip is too large to keep in git history).
        private const string DefaultQuranPackageUrl = "https://github.com/ajalil051983/Dalil-Alhaj/releases/download/quran-pages-v1/warsh-pages-604.zip";
        // GitHub release URLs 302-redirect to the CDN. Some platform handlers do not
        // auto-follow redirects for streaming (ResponseHeadersRead) requests, which
        // surfaces the 302 as the final response and fails the download at the end.
        // This client explicitly follows redirects so that never happens.
        private static readonly HttpClient HttpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                AutomaticDecompression = DecompressionMethods.None
            };
            return new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromMinutes(15)
            };
        }

        private readonly QuranPageAssetService pageAssetService = new();

        public string GetSavedPackageUrl()
        {
            var stored = Preferences.Get(QuranPackageUrlKey, string.Empty);
            var effective = string.IsNullOrWhiteSpace(stored)
                ? DefaultQuranPackageUrl
                : stored;

            var normalized = NormalizeDownloadUrl(effective);
            if (!UrlMatches(stored, normalized))
            {
                Preferences.Set(QuranPackageUrlKey, normalized);
            }

            return normalized;
        }

        public void SavePackageUrl(string packageUrl)
        {
            if (!string.IsNullOrWhiteSpace(packageUrl))
            {
                Preferences.Set(QuranPackageUrlKey, NormalizeDownloadUrl(packageUrl.Trim()));
            }
        }

        public bool HasInstalledPages()
        {
            return pageAssetService.HasInstalledPages();
        }

        public Task<QuranPackageInstallResult> DownloadAndInstallAsync(
            string packageUrl,
            IProgress<double>? progress,
            IProgress<string>? status,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(packageUrl))
            {
                return Task.FromResult(QuranPackageInstallResult.Failed("أدخل رابط ملف المصحف أولاً."));
            }

            return DownloadAndInstallInternalAsync(NormalizeDownloadUrl(packageUrl.Trim()), progress, status, cancellationToken);
        }

        public Task<QuranPackageInstallResult> InstallFromZipAsync(
            string zipPath,
            IProgress<string>? status,
            CancellationToken cancellationToken)
        {
            return InstallFromZipInternalAsync(zipPath, status, cancellationToken);
        }

        private async Task<QuranPackageInstallResult> DownloadAndInstallInternalAsync(
            string packageUrl,
            IProgress<double>? progress,
            IProgress<string>? status,
            CancellationToken cancellationToken)
        {
            // Stable path + .part suffix so a killed/backgrounded download resumes
            // instead of restarting from zero on the next attempt.
            var tempZipPath = Path.Combine(FileSystem.CacheDirectory, "quran-pages-download.zip");
            var partPath = tempZipPath + ".part";

            BeginBackgroundDownloadKeepAlive();
            try
            {
                status?.Report("جاري تنزيل حزمة المصحف...");

                // Resolve the redirect chain up front so a platform handler that does not
                // auto-follow (returning the 302 as the final response) cannot fail the
                // download. This also gives us the real CDN URL and Content-Length.
                var effectiveUrl = await ResolveFinalUrlAsync(packageUrl, cancellationToken);

                const int maxAttempts = 4;
                long totalBytes = 0;
                long downloaded = 0;

                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        // Resume from the existing partial file if the server supports Range.
                        downloaded = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
                        using var request = new HttpRequestMessage(HttpMethod.Get, effectiveUrl);
                        if (downloaded > 0)
                        {
                            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(downloaded, null);
                        }

                        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                        // 416 = requested range not satisfiable (partial file stale) -> restart clean.
                        if (response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
                        {
                            TryDeleteFile(partPath);
                            continue;
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            return QuranPackageInstallResult.Failed($"فشل التنزيل: {(int)response.StatusCode}");
                        }

                        // If we asked to resume but the server ignored the Range header (200
                        // instead of 206), restart from scratch to avoid a corrupt file.
                        var resumed = downloaded > 0 && response.StatusCode == System.Net.HttpStatusCode.PartialContent;
                        if (downloaded > 0 && !resumed)
                        {
                            downloaded = 0;
                        }

                        var contentLength = response.Content.Headers.ContentLength ?? 0;
                        totalBytes = resumed ? downloaded + contentLength : contentLength;

                        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
                        await using (var output = new FileStream(partPath, resumed ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[81920];
                            int read;
                            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                            {
                                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                                downloaded += read;

                                if (totalBytes > 0)
                                {
                                    progress?.Report(Math.Min(1.0, downloaded / (double)totalBytes));
                                }
                            }
                        }

                        // Completed the stream without error.
                        break;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw; // Genuine user/app cancellation.
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    {
                        // Transient network drop mid-stream (common when the OS throttles a
                        // backgrounded app): wait briefly and resume from the partial file.
                        System.Diagnostics.Debug.WriteLine($"[QuranPackage] Download attempt {attempt} interrupted, resuming: {ex.Message}");
                        await Task.Delay(1200 * attempt, cancellationToken);
                    }
                }

                // Finalize the completed file.
                TryDeleteFile(tempZipPath);
                File.Move(partPath, tempZipPath, true);

                progress?.Report(1.0);
                return await InstallFromZipInternalAsync(tempZipPath, status, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return QuranPackageInstallResult.Failed("تم إلغاء عملية التنزيل.");
            }
            catch (Exception ex)
            {
                return QuranPackageInstallResult.Failed($"تعذر تنزيل الحزمة: {ex.Message}");
            }
            finally
            {
                EndBackgroundDownloadKeepAlive();
                TryDeleteFile(tempZipPath);
            }
        }

        // Keeps the process/network alive while backgrounded (Android foreground service).
        // No-op on other platforms or if the service cannot start.
        private static void BeginBackgroundDownloadKeepAlive()
        {
#if ANDROID
            try
            {
                var context = global::Android.App.Application.Context;
                Platforms.Android.Services.DownloadForegroundService.Start(context, "جاري تنزيل حزمة المصحف...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QuranPackage] Could not start download service: {ex.Message}");
            }
#endif
        }

        private static void EndBackgroundDownloadKeepAlive()
        {
#if ANDROID
            try
            {
                var context = global::Android.App.Application.Context;
                Platforms.Android.Services.DownloadForegroundService.Stop(context);
            }
            catch { /* ignore */ }
#endif
        }

        private async Task<QuranPackageInstallResult> InstallFromZipInternalAsync(
            string zipPath,
            IProgress<string>? status,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            {
                return QuranPackageInstallResult.Failed("ملف الحزمة غير موجود.");
            }

            var extractionFolder = Path.Combine(FileSystem.CacheDirectory, $"quran-pages-extract-{Guid.NewGuid():N}");

            try
            {
                status?.Report("جاري فك ضغط الحزمة...");
                Directory.CreateDirectory(extractionFolder);
                ZipFile.ExtractToDirectory(zipPath, extractionFolder, true);
                cancellationToken.ThrowIfCancellationRequested();

                var sourcePagesFolder = FindPagesDirectory(extractionFolder);
                if (string.IsNullOrWhiteSpace(sourcePagesFolder) || !Directory.Exists(sourcePagesFolder))
                {
                    return QuranPackageInstallResult.Failed("صيغة ملف الحزمة غير صحيحة (مجلد pages غير موجود).");
                }

                status?.Report("جاري تثبيت صفحات المصحف...");
                var destinationFolder = pageAssetService.GetPagesDirectoryPath();
                var destinationRoot = Directory.GetParent(destinationFolder)?.FullName;
                if (!string.IsNullOrWhiteSpace(destinationRoot) && Directory.Exists(destinationRoot))
                {
                    Directory.Delete(destinationRoot, true);
                }

                Directory.CreateDirectory(destinationFolder);
                var copiedCount = 0;
                foreach (var file in Directory.EnumerateFiles(sourcePagesFolder, "*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension != ".webp" && extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                    {
                        continue;
                    }

                    var targetPath = Path.Combine(destinationFolder, Path.GetFileName(file));
                    File.Copy(file, targetPath, true);
                    copiedCount++;
                }

                pageAssetService.InvalidateCache();

                if (!pageAssetService.HasInstalledPages())
                {
                    return QuranPackageInstallResult.Failed("تم نسخ الملفات لكن عدد صفحات المصحف غير كافٍ.");
                }

                return QuranPackageInstallResult.Success(copiedCount);
            }
            catch (OperationCanceledException)
            {
                return QuranPackageInstallResult.Failed("تم إلغاء تثبيت الحزمة.");
            }
            catch (Exception ex)
            {
                return QuranPackageInstallResult.Failed($"تعذر تثبيت الحزمة: {ex.Message}");
            }
            finally
            {
                await Task.Run(() => TryDeleteDirectory(extractionFolder), CancellationToken.None);
            }
        }

        // Follows 3xx redirects with a no-redirect HEAD-style probe and returns the final
        // URL. Uses a separate handler with AllowAutoRedirect=false so we can read the
        // Location header ourselves; if the URL does not redirect, it is returned as-is.
        private static async Task<string> ResolveFinalUrlAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using var probe = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(30) };

                var current = url;
                for (var hop = 0; hop < 10; hop++)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, current);
                    using var response = await probe.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    var code = (int)response.StatusCode;
                    if (code is >= 300 and < 400 && response.Headers.Location is { } location)
                    {
                        current = location.IsAbsoluteUri
                            ? location.ToString()
                            : new Uri(new Uri(current), location).ToString();
                        continue;
                    }

                    return current;
                }

                return current;
            }
            catch
            {
                // If probing fails (offline, etc.), fall back to the original URL and let
                // the main download surface the real error.
                return url;
            }
        }

        private static string NormalizeDownloadUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return url;
            }

            var host = uri.Host.ToLowerInvariant();
            if (host.Contains("github.com"))
            {
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 5 && segments[2].Equals("blob", StringComparison.OrdinalIgnoreCase))
                {
                    var owner = segments[0];
                    var repo = segments[1];
                    var branch = segments[3];
                    var relativePath = string.Join('/', segments.Skip(4));
                    return $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{relativePath}";
                }

                return url;
            }

            return url;
        }

        private static bool UrlMatches(string? left, string? right)
        {
            return string.Equals(NormalizeDownloadUrl(left ?? string.Empty), NormalizeDownloadUrl(right ?? string.Empty), StringComparison.OrdinalIgnoreCase);
        }

        private static string FindPagesDirectory(string extractedRoot)
        {
            var directPages = Path.Combine(extractedRoot, "pages");
            if (Directory.Exists(directPages))
            {
                return directPages;
            }

            var warshPages = Path.Combine(extractedRoot, "warsh", "pages");
            if (Directory.Exists(warshPages))
            {
                return warshPages;
            }

            foreach (var dir in Directory.EnumerateDirectories(extractedRoot, "*", SearchOption.AllDirectories))
            {
                var hasPageLikeFiles = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
                    .Any(path =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        var ext = Path.GetExtension(path).ToLowerInvariant();
                        return (ext == ".webp" || ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                               && int.TryParse(fileName, out _);
                    });

                if (hasPageLikeFiles)
                {
                    return dir;
                }
            }

            return string.Empty;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }
    }

    public sealed class QuranPackageInstallResult
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; } = string.Empty;
        public int InstalledPagesCount { get; init; }

        public static QuranPackageInstallResult Success(int installedPagesCount)
        {
            return new QuranPackageInstallResult
            {
                IsSuccess = true,
                InstalledPagesCount = installedPagesCount,
                Message = $"تم تثبيت {installedPagesCount} صفحة بنجاح."
            };
        }

        public static QuranPackageInstallResult Failed(string message)
        {
            return new QuranPackageInstallResult
            {
                IsSuccess = false,
                Message = message
            };
        }
    }
}
