namespace Bat.AspNetCore;

public static class FileExtensions
{
    public static byte[] ToByteArray(this IFormFile file)
    {
        using var target = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        using var source = file.OpenReadStream();
        source.CopyTo(target);
        return target.ToArray();
    }

    public static string ToBase64(this IFormFile file)
        => Convert.ToBase64String(file.ToByteArray());

    public static string Save(this IFormFile file, string fullPath)
    {
        if (file is null || file.Length <= 0) return null;

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
        }
        if (File.Exists(fullPath)) return fullPath.Contains("wwwroot/") ? fullPath.Remove(0, 8) : fullPath;
        return null;
    }

    public static string SaveFile(this byte[] fileBytes, string fullPath)
    {
        if (fileBytes is null || fileBytes.Length <= 0) return null;

        // Fixed: the bytes were wrapped in a FormFile with a null stream, so nothing was written (it threw).
        File.WriteAllBytes(fullPath, fileBytes);
        if (File.Exists(fullPath)) return fullPath;
        return null;
    }
}