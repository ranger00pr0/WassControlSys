namespace WassControlSys.Models
{
    public class CleanResult
    {
        public int FilesDeleted { get; set; }
        public int FoldersDeleted { get; set; }
        public int FilesFailed { get; set; }
        public int FoldersFailed { get; set; }
        public long BytesFreed { get; set; }
        public string? Notes { get; set; }

        public override string ToString()
        {
            string freed = BytesFreed > 0 ? FormatBytes(BytesFreed) : "0 B";
            return $"Eliminados: {FilesDeleted} archivos, {FoldersDeleted} carpetas. Fallidos: {FilesFailed} archivos, {FoldersFailed} carpetas. Espacio liberado: {freed}.";
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
