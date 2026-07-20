using System.IO.Compression;

namespace KhayratAlhaj.Services
{
    public class QuranPackageInstallerService
    {
        private const string QuranPackageUrlKey = "quran_package_url";
        private const string DefaultQuranPackageUrl = "https://drive.google.com/file/d/1eYso3a1rnSJyFn_2lmx2IPBaRoJL-nf2/view?usp=sharing";
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        private readonly QuranPageAssetService pageAssetService = new();

        public string GetSavedPackageUrl()
        {
            var stored = Preferences.Get(QuranPackageUrlKey, string.Empty);
            var effective = string.IsNullOrWhiteSpace(stored) ? DefaultQuranPackageUrl : stored;
            return NormalizeDownloadUrl(effective);
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
            var tempZipPath = Path.Combine(FileSystem.CacheDirectory, $"quran-pages-{Guid.NewGuid():N}.zip");

            try
            {
                status?.Report("جاري تنزيل حزمة المصحف...");
                using var response = await HttpClient.GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return QuranPackageInstallResult.Failed($"فشل التنزيل: {(int)response.StatusCode}");
                }

                var totalBytes = response.Content.Headers.ContentLength;
                await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var output = File.Create(tempZipPath))
                {
                    var buffer = new byte[81920];
                    long downloaded = 0;
                    int read;
                    while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                    {
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        downloaded += read;

                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            progress?.Report(Math.Min(1.0, downloaded / (double)totalBytes.Value));
                        }
                    }
                }

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
                TryDeleteFile(tempZipPath);
            }
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
            if (!host.Contains("drive.google.com"))
            {
                return url;
            }

            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var fileSegmentIndex = Array.FindIndex(pathSegments, segment => segment.Equals("d", StringComparison.OrdinalIgnoreCase));
            if (fileSegmentIndex >= 0 && fileSegmentIndex + 1 < pathSegments.Length)
            {
                var fileId = pathSegments[fileSegmentIndex + 1];
                return $"https://drive.google.com/uc?export=download&id={fileId}";
            }

            var query = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

            if (query.TryGetValue("id", out var queryId) && !string.IsNullOrWhiteSpace(queryId))
            {
                return $"https://drive.google.com/uc?export=download&id={queryId}";
            }

            return url;
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
