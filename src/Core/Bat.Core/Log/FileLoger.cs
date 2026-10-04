namespace Bat.Core;

// Paths use Path.Combine so logs work on Linux/Kubernetes too (the old "\\" separators created
// files literally named "Log\\Info-....txt" in the working directory on Linux).
// File names use the date only: PersianDateTime.ToString() includes the time (HH:mm:ss), which created
// one file per second and is an invalid file name on Windows (':').
public class FileLoger
{
    private static readonly object _infoLock = new();
    private static readonly object _errorLock = new();
    private static readonly object _messageLock = new();

    public static void Info(string log, string path = "")
    {
        if (string.IsNullOrEmpty(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var now = PersianDateTime.Now;
        Monitor.Enter(_infoLock);
        try
        {
            using var stream = File.AppendText(Path.Combine(path, $"Info-{now.ToString(PersianDateTimeFormat.Date).Replace("/", "-")}.txt"));
            stream.WriteLine($"{now.ToString(PersianDateTimeFormat.DateTime)} :: {log}");
            stream.Close();
        }
        finally
        {
            Monitor.Exit(_infoLock);
        }
    }

    public static void CriticalInfo(string log, string path = "")
    {
        if (string.IsNullOrEmpty(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var now = PersianDateTime.Now;
        Monitor.Enter(_infoLock);
        try
        {
            using var stream = File.AppendText(Path.Combine(path, $"CriticalInfo-{now.ToString(PersianDateTimeFormat.Date).Replace("/", "-")}.txt"));
            stream.WriteLine($"{now.ToString(PersianDateTimeFormat.DateTime)} :: {log}");
            stream.Close();
        }
        finally
        {
            Monitor.Exit(_infoLock);
        }
    }

    public static void Message(string log, string path = "")
    {
        if (string.IsNullOrEmpty(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var now = PersianDateTime.Now;
        Monitor.Enter(_messageLock);
        try
        {
            using var stream = File.AppendText(Path.Combine(path, $"Message-{now.ToString(PersianDateTimeFormat.Date).Replace("/", "-")}.txt"));
            stream.WriteLine($"{now.ToString(PersianDateTimeFormat.DateTime)} :: {log}");
            stream.Close();
        }
        finally
        {
            Monitor.Exit(_messageLock);
        }
    }

    public static void Error(Exception e, string path = "")
    {
        var exceptionDetails = ExceptionBusiness.GetCallerMethodName(e);
        if (string.IsNullOrEmpty(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var now = PersianDateTime.Now;
        Monitor.Enter(_errorLock);
        try
        {
            using var stream = File.AppendText(Path.Combine(path, $"Error-{now.ToString(PersianDateTimeFormat.Date).Replace("/", "-")}.txt"));
            stream.WriteLine(
                $" DateTime : {now.ToString(PersianDateTimeFormat.DateTime)}" + Environment.NewLine +
                $" MethodName : {exceptionDetails.MethodName}" + Environment.NewLine +
                $" Parameters : {exceptionDetails.Parameters}" + Environment.NewLine +
                $" ExceptionLineNumber : {exceptionDetails.ExceptionLineNumber}" + Environment.NewLine +
                $" Message : {e.Message}" + Environment.NewLine +
                $" InnerException : {e.InnerException}" + Environment.NewLine + Environment.NewLine);
            stream.Close();
        }
        finally
        {
            Monitor.Exit(_errorLock);
        }
    }

    public static void CriticalError(Exception e, string path = "")
    {
        var exceptionDetails = ExceptionBusiness.GetCallerMethodName(e);
        if (string.IsNullOrEmpty(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var now = PersianDateTime.Now;
        Monitor.Enter(_errorLock);
        try
        {
            using var stream = File.AppendText(Path.Combine(path, $"CriticalError-{now.ToString(PersianDateTimeFormat.Date).Replace("/", "-")}.txt"));
            stream.WriteLine(
                $" DateTime : {now.ToString(PersianDateTimeFormat.DateTime)}" + Environment.NewLine +
                $" MethodName : {exceptionDetails.MethodName}" + Environment.NewLine +
                $" Parameters : {exceptionDetails.Parameters}" + Environment.NewLine +
                $" ExceptionLineNumber : {exceptionDetails.ExceptionLineNumber}" + Environment.NewLine +
                $" Message : {e.Message}" + Environment.NewLine +
                $" InnerException : {e.InnerException}" + Environment.NewLine + Environment.NewLine);
            stream.Close();
        }
        finally
        {
            Monitor.Exit(_errorLock);
        }
    }
}