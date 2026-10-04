namespace Bat.Core;

public static partial class FileOperation
{
    private static readonly HashSet<string> _image = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".tiff", ".heic" };
    private static readonly HashSet<string> _audio = new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".wav", ".flm", ".fsm", ".ogg", ".m4a", ".m4b", ".m4p", ".m4r" };
    private static readonly HashSet<string> _video = new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".avi", ".ts", ".m4v", ".flv" };
    private static readonly HashSet<string> _archive = new(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".iso", ".tar" };
    private static readonly HashSet<string> _document = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".xln", ".txt", ".xls", ".xlm", ".josn", ".xlsx", ".pptx" };

    public static bool Save(string fileName, byte[] file, string absolatePath)
    {
        string path = Path.Combine(absolatePath, fileName);
        File.WriteAllBytes(path, file);
        if (File.Exists(path)) return true;

        return false;
    }

    public static string Save(string absolatePath, byte[] file)
    {
        var fileName = DateTime.Now.Ticks.ToString();
        string path = Path.Combine(absolatePath, fileName);
        File.WriteAllBytes(path, file);
        if (File.Exists(path)) return fileName;

        return string.Empty;
    }

    public static bool Delete(string fileName, string absolatePath)
    {
        string path = Path.Combine(absolatePath, fileName);
        File.Delete(path);
        if (File.Exists(path)) return false;

        return true;
    }

    public static bool Exist(string fileName, string absolatePath)
    {
        string path = Path.Combine(absolatePath, fileName);
        if (File.Exists(path)) return true;

        return false;
    }

    // Directory.CreateDirectory creates every missing segment. The old implementation joined segments
    // with '\\' and dropped the leading '/', so on Linux it created relative folders with backslashes in their names.
    public static bool CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var normalized = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        Directory.CreateDirectory(normalized);
        return true;
    }

    // Old (misspelled) name kept so services compiled against 10.0.0 still build.
    [Obsolete("Use CheckExtension.")]
    public static bool CheckExtention(FileType fileType, string fileName) => CheckExtension(fileType, fileName);

    public static bool CheckExtension(FileType fileType, string fileName)
    {
        switch (fileType)
        {
            case FileType.Image:
                return _image.Contains(Path.GetExtension(fileName) ?? string.Empty);
            case FileType.Audio:
                return _audio.Contains(Path.GetExtension(fileName) ?? string.Empty);
            case FileType.Video:
                return _video.Contains(Path.GetExtension(fileName) ?? string.Empty);
            case FileType.Archive:
                return _archive.Contains(Path.GetExtension(fileName) ?? string.Empty);
            case FileType.Document:
                return _document.Contains(Path.GetExtension(fileName) ?? string.Empty);
            case FileType.Unknown:
            default:
                return false;

        }
    }

    public static FileType GetFileType(string fileName)
    {
        switch (Path.GetExtension(fileName))
        {
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".gif":
            case ".tiff":
            case ".heic":
                return FileType.Image;
            case ".mp3":
            case ".wav":
            case ".flm":
            case ".fsm":
            case ".ogg":
            case ".m4a":
            case ".m4b":
            case ".m4p":
            case ".m4r":
                return FileType.Audio;
            case ".mp4":
            case ".mkv":
            case ".avi":
            case ".ts":
            case ".m4v":
            case ".flv":
                return FileType.Video;
            case ".zip":
            case ".rar":
            case ".iso":
            case ".tar":
            case ".jar":
                return FileType.Archive;
            case ".pdf":
            case ".doc":
            case ".docx":
            case ".txt":
            case ".xls":
            case ".xlsx":
            case ".josn":
            case ".pptx":
                return FileType.Document;
            default:
                return FileType.Unknown;
        }
    }

    public static FileInfo GetFileInfo(string fullPath) => new FileInfo(fullPath);
}