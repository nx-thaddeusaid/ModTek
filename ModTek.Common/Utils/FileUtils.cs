using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ModTek.Common.Globals;

namespace ModTek.Common.Utils;

internal static class FileUtils
{
    // this is more an informative path that works for diagnosing issues
    internal static string GetRelativePath(string path)
    {
        try
        {
            return new Uri(Paths.BaseDirectory).MakeRelativeUri(new Uri(path)).ToString();
        }
        catch
        {
            return path;
        }
    }

    // this is the correct relative path with proper directory separators for internal use
    internal static string GetRealRelativePath(string absolutePath, string basePath)
    {
        if (!Path.IsPathRooted(absolutePath))
        {
            return absolutePath;
        }

        basePath = Path.GetFullPath(basePath);
        if (basePath.Last() != Path.DirectorySeparatorChar)
        {
            basePath += Path.DirectorySeparatorChar;
        }

        var pathUri = new Uri(Path.GetFullPath(absolutePath), UriKind.Absolute);
        var rootUri = new Uri(basePath, UriKind.Absolute);

        if (pathUri.Scheme != rootUri.Scheme)
        {
            return absolutePath;
        }

        var relativeUri = rootUri.MakeRelativeUri(pathUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (pathUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        return relativePath;
    }

    private static readonly string[] IGNORE_LIST =
    {
        ".DS_STORE",
        "~",
        ".nomedia"
    };

    internal static bool FileIsOnDenyList(string filePath)
    {
        return IGNORE_LIST.Any(x => filePath.EndsWith(x, StringComparison.OrdinalIgnoreCase));
    }

    internal static List<string> FindFiles(string basePath, params string[] suffixes)
    {
        // EnumerateFiles is lazy — avoids materialising the full directory listing
        // before filtering. When only one suffix is requested pass it to the OS so
        // the kernel can filter instead of pulling every file across the syscall boundary.
        IEnumerable<string> raw;
        if (suffixes == null || suffixes.Length == 0)
        {
            raw = Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories);
        }
        else if (suffixes.Length == 1)
        {
            raw = Directory.EnumerateFiles(basePath, "*" + suffixes[0], SearchOption.AllDirectories);
            return raw.Where(p => !FileIsOnDenyList(p)).ToList();
        }
        else
        {
            var suffixSet = new HashSet<string>(suffixes, StringComparer.OrdinalIgnoreCase);
            raw = Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories)
                .Where(p => suffixSet.Contains(Path.GetExtension(p)));
        }
        return raw.Where(p => !FileIsOnDenyList(p)).ToList();
    }

    internal const string JSON_TYPE = ".json";
    private const string CSV_TYPE = ".csv";
    private const string TXT_TYPE = ".txt";

    internal static bool IsJson(string name)
    {
        return name.HasExtension(JSON_TYPE);
    }

    internal static bool IsCsv(string name)
    {
        return name.HasExtension(CSV_TYPE);
    }

    internal static bool IsTxt(string name)
    {
        return name.HasExtension(TXT_TYPE);
    }

    internal static string GetExtension(this string str)
    {
        var index = str.LastIndexOf('.');
        return index >= 0 ? str.Substring(index) : default;
    }

    internal static bool HasExtension(this string str, string ext)
    {
        var index = str.LastIndexOf('.');
        return index >= 0 && str.Substring(index).Equals(ext);
    }

    internal static bool HasStringExtension(this string str)
    {
        var ext = str.GetExtension();
        return ext != null && StringExtensions.Contains(ext);
    }

    internal static readonly string[] StringExtensions = {
        JSON_TYPE,
        CSV_TYPE,
        TXT_TYPE,
    };

    internal static void CleanDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Directory.CreateDirectory(path);
    }

    public static void CreateParentOfPath(string path)
    {
        var fi = new FileInfo(path);
        var dir = fi.Directory;
        if (dir != null && !dir.Exists)
        {
            dir.Create();
        }
    }

    internal static StreamReader StreamReaderFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return new StreamReader(stream);
    }

    internal static void RotatePath(string path, int backups)
    {
        for (var i = backups - 1; i >= 0; i--)
        {
            var pathCurrent = path + (i == 0 ? "" : "." + i);
            var pathNext = path + "." + (i + 1);
            File.Delete(pathNext);
            if (File.Exists(pathCurrent))
            {
                File.Move(pathCurrent, pathNext);
            }
        }
    }

    internal static void CreateDirectoryForFile(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new Exception($"Could not find directory for {filePath}"));
    }

    internal static void SetupCleanDirectory(string path, bool recursive = false)
    {
        var di = new DirectoryInfo(path);
        if (di.Exists)
        {
            foreach (var file in di.GetFiles())
            {
                file.Delete();
            }
            if (recursive)
            {
                foreach (var dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
        }
        else
        {
            di.Create();
        }
    }

    public static StreamWriter LogStream(string path)
    {
        return new StreamWriter(
            File.Open(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete),
            Encoding.UTF8,
            32 * 1024
        )
        {
            AutoFlush = true
        };
    }
}