using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class DiskAnalyzerService : IDiskAnalyzerService
    {
        private readonly ILogService _log;

        public DiskAnalyzerService(ILogService log)
        {
            _log = log;
        }

        public async Task<IEnumerable<FolderSizeInfo>> AnalyzeDirectoryAsync(string path)
        {
            return await Task.Run(() =>
            {
                var folderSizes = new Dictionary<string, long>();
                long totalSize = 0;
                var rootFilesSize = 0L;

                try
                {
                    var root = new DirectoryInfo(path);
                    if (!root.Exists) return new List<FolderSizeInfo>();

                    // Initialize dictionary with top-level directories
                    foreach (var dir in root.GetDirectories())
                    {
                        folderSizes[dir.FullName] = 0;
                    }

                    // Perform a single recursive scan
                    foreach (var file in root.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var topLevelFolder = GetTopLevelFolder(root.FullName, file.DirectoryName);
                            if (topLevelFolder != null)
                            {
                                folderSizes[topLevelFolder] += file.Length;
                            }
                            else
                            {
                                // File is in the root directory
                                rootFilesSize += file.Length;
                            }
                            totalSize += file.Length;
                        }
                        catch (UnauthorizedAccessException) { /* Skip files we can't access */ }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error analyzing disk at {path}", ex);
                    return new List<FolderSizeInfo>();
                }
                
                var result = folderSizes.Select(kvp => new FolderSizeInfo
                {
                    Path = new DirectoryInfo(kvp.Key).Name,
                    SizeBytes = kvp.Value,
                    FormattedSize = FormatSize(kvp.Value)
                }).ToList();

                // Add files in the root directory as a separate item if they exist
                if (rootFilesSize > 0)
                {
                    result.Add(new FolderSizeInfo
                    {
                        Path = "[Archivos en la raíz]",
                        SizeBytes = rootFilesSize,
                        FormattedSize = FormatSize(rootFilesSize)
                    });
                }
                
                // Calculate percentages
                if (totalSize > 0)
                {
                    foreach (var item in result)
                    {
                        item.Percentage = (double)item.SizeBytes / totalSize * 100;
                    }
                }

                return result.OrderByDescending(x => x.SizeBytes).Take(20);
            });
        }

        private string? GetTopLevelFolder(string rootPath, string? filePath)
        {
            if (filePath == null || !filePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                return null;
            
            // Normalize paths to ensure consistent comparison
            var normalizedFilePath = filePath.TrimEnd(Path.DirectorySeparatorChar);
            var normalizedRootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar);

            if (string.Equals(normalizedFilePath, normalizedRootPath, StringComparison.OrdinalIgnoreCase))
                return null; // It's in the root itself

            var subPath = filePath.Substring(normalizedRootPath.Length + 1);
            var firstSeparatorIndex = subPath.IndexOf(Path.DirectorySeparatorChar);

            var topLevelDirName = firstSeparatorIndex == -1 ? subPath : subPath.Substring(0, firstSeparatorIndex);

            return Path.Combine(rootPath, topLevelDirName);
        }

        public async Task<IEnumerable<FolderSizeInfo>> FindLargeFilesAsync(string path, long minSizeInBytes)
        {
            return await Task.Run(() =>
            {
                var result = new List<FolderSizeInfo>();
                try
                {
                    if (!Directory.Exists(path)) return result;

                    var di = new DirectoryInfo(path);
                    var files = di.EnumerateFiles("*", SearchOption.AllDirectories)
                                  .Where(f => f.Length >= minSizeInBytes)
                                  .OrderByDescending(f => f.Length)
                                  .Take(50);

                    foreach (var file in files)
                    {
                        result.Add(new FolderSizeInfo
                        {
                            Path = file.FullName,
                            SizeBytes = file.Length,
                            FormattedSize = FormatSize(file.Length)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error finding large files at {path}", ex);
                }
                return result;
            });
        }

        private string FormatSize(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }
            return $"{dblSByte:0.##} {Suffix[i]}";
        }
    }
}
