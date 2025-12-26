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
                var result = new List<FolderSizeInfo>();
                try
                {
                    if (!Directory.Exists(path)) return result;

                    var di = new DirectoryInfo(path);
                    long totalSize = 0;

                    foreach (var dir in di.GetDirectories())
                    {
                        try
                        {
                            long size = GetDirectorySize(dir);
                            totalSize += size;
                            result.Add(new FolderSizeInfo
                            {
                                Path = dir.Name,
                                SizeBytes = size,
                                FormattedSize = FormatSize(size)
                            });
                        }
                        catch { /* Skip folders with no access */ }
                    }

                    // Add files in root too
                    foreach (var file in di.GetFiles())
                    {
                        totalSize += file.Length;
                        result.Add(new FolderSizeInfo
                        {
                            Path = file.Name,
                            SizeBytes = file.Length,
                            FormattedSize = FormatSize(file.Length)
                        });
                    }

                    if (totalSize > 0)
                    {
                        foreach (var item in result)
                        {
                            item.Percentage = (double)item.SizeBytes / totalSize * 100;
                        }
                    }

                    return result.OrderByDescending(x => x.SizeBytes).Take(20);
                }
                catch (Exception ex)
                {
                    _log.Error($"Error analizando disco en {path}", ex);
                    return result;
                }
            });
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
                    // Usar recursividad para buscar archivos grandes en todas las subcarpetas
                    var files = di.EnumerateFiles("*", SearchOption.AllDirectories)
                                  .Where(f => f.Length >= minSizeInBytes)
                                  .OrderByDescending(f => f.Length)
                                  .Take(50); // Limitar a los 50 más grandes para rendimiento

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
                    _log.Error($"Error buscando archivos grandes en {path}", ex);
                }
                return result;
            });
        }

        private long GetDirectorySize(DirectoryInfo di)

        {
            long size = 0;
            try
            {
                foreach (var file in di.GetFiles("*", SearchOption.AllDirectories))
                {
                    size += file.Length;
                }
            }
            catch { }
            return size;
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
