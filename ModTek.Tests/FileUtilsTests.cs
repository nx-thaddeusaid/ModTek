using System;
using System.IO;
using System.Text;
using ModTek.Common.Utils;
using Xunit;

namespace ModTek.Tests;

public class FileUtilsTests
{
    // GetRealRelativePath

    [Fact]
    public void GetRealRelativePath_SubPath_ReturnsRelative()
    {
        var result = FileUtils.GetRealRelativePath("/a/b/c", "/a/b");
        Assert.Equal("c", result);
    }

    [Fact]
    public void GetRealRelativePath_DeeperSubPath_ReturnsRelative()
    {
        var result = FileUtils.GetRealRelativePath("/a/b/c/d", "/a/b");
        Assert.Equal("c/d", result);
    }

    [Fact]
    public void GetRealRelativePath_AlreadyRelative_ReturnedUnchanged()
    {
        var result = FileUtils.GetRealRelativePath("relative/path", "/base");
        Assert.Equal("relative/path", result);
    }

    [Fact]
    public void GetRealRelativePath_SameDirectory_ReturnsFilename()
    {
        var result = FileUtils.GetRealRelativePath("/a/b/file.json", "/a/b");
        Assert.Equal("file.json", result);
    }

    // FileIsOnDenyList

    [Fact]
    public void FileIsOnDenyList_DsStore_True()
    {
        Assert.True(FileUtils.FileIsOnDenyList("some/path/.DS_STORE"));
    }

    [Fact]
    public void FileIsOnDenyList_DsStoreLower_True()
    {
        Assert.True(FileUtils.FileIsOnDenyList("some/path/.ds_store"));
    }

    [Fact]
    public void FileIsOnDenyList_TrailingTilde_True()
    {
        Assert.True(FileUtils.FileIsOnDenyList("backup~"));
    }

    [Fact]
    public void FileIsOnDenyList_NomediaFile_True()
    {
        Assert.True(FileUtils.FileIsOnDenyList("/some/dir/.nomedia"));
    }

    [Fact]
    public void FileIsOnDenyList_NormalJson_False()
    {
        Assert.False(FileUtils.FileIsOnDenyList("mechdef_atlas.json"));
    }

    [Fact]
    public void FileIsOnDenyList_LeadingTilde_False()
    {
        Assert.False(FileUtils.FileIsOnDenyList("~backup.txt"));
    }

    // HasExtension / GetExtension

    [Fact]
    public void HasExtension_Json_True()
    {
        Assert.True("file.json".HasExtension(".json"));
    }

    [Fact]
    public void HasExtension_Csv_True()
    {
        Assert.True("data.csv".HasExtension(".csv"));
    }

    [Fact]
    public void HasExtension_Txt_True()
    {
        Assert.True("notes.txt".HasExtension(".txt"));
    }

    [Fact]
    public void HasExtension_NoExtension_False()
    {
        Assert.False("file".HasExtension(".json"));
    }

    [Fact]
    public void HasExtension_WrongExtension_False()
    {
        Assert.False("file.csv".HasExtension(".json"));
    }

    [Fact]
    public void HasExtension_CaseSensitive_False()
    {
        Assert.False("file.JSON".HasExtension(".json"));
    }

    [Fact]
    public void GetExtension_ReturnsExtensionWithDot()
    {
        Assert.Equal(".json", "file.json".GetExtension());
    }

    [Fact]
    public void GetExtension_NoExtension_ReturnsNull()
    {
        Assert.Null("file".GetExtension());
    }

    // IsJson / IsCsv / IsTxt

    [Fact]
    public void IsJson_JsonFile_True()
    {
        Assert.True(FileUtils.IsJson("mechdef.json"));
    }

    [Fact]
    public void IsJson_CsvFile_False()
    {
        Assert.False(FileUtils.IsJson("data.csv"));
    }

    [Fact]
    public void IsCsv_CsvFile_True()
    {
        Assert.True(FileUtils.IsCsv("data.csv"));
    }

    [Fact]
    public void IsTxt_TxtFile_True()
    {
        Assert.True(FileUtils.IsTxt("notes.txt"));
    }

    // HasStringExtension

    [Fact]
    public void HasStringExtension_Json_True()
    {
        Assert.True("file.json".HasStringExtension());
    }

    [Fact]
    public void HasStringExtension_Png_False()
    {
        Assert.False("image.png".HasStringExtension());
    }

    // RotatePath

    [Fact]
    public void RotatePath_CreatesBackups()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "log.txt");
            File.WriteAllText(path, "current");

            FileUtils.RotatePath(path, 3);

            Assert.False(File.Exists(path));
            Assert.True(File.Exists(path + ".1"));
            Assert.Equal("current", File.ReadAllText(path + ".1"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void RotatePath_ShiftsExistingBackups()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "log.txt");
            File.WriteAllText(path, "current");
            File.WriteAllText(path + ".1", "backup1");

            FileUtils.RotatePath(path, 3);

            Assert.False(File.Exists(path));
            Assert.Equal("current", File.ReadAllText(path + ".1"));
            Assert.Equal("backup1", File.ReadAllText(path + ".2"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void RotatePath_DropsOldestWhenAtLimit()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "log.txt");
            File.WriteAllText(path, "current");
            File.WriteAllText(path + ".1", "backup1");
            File.WriteAllText(path + ".2", "backup2");

            FileUtils.RotatePath(path, 2); // keep 2 backups: .1 and (old .1→.2 drops old .2)

            Assert.False(File.Exists(path));
            Assert.Equal("current", File.ReadAllText(path + ".1"));
            Assert.Equal("backup1", File.ReadAllText(path + ".2"));
            Assert.False(File.Exists(path + ".3"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // FindFiles

    [Fact]
    public void FindFiles_WithSuffix_ReturnsMatchingFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "b.txt"), "text");
            var found = FileUtils.FindFiles(dir, ".json");
            Assert.Single(found);
            Assert.Contains("a.json", found[0]);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FindFiles_NoSuffix_ReturnsAllNonDenied()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "b.txt"), "text");
            File.WriteAllText(Path.Combine(dir, ".DS_STORE"), "");
            var found = FileUtils.FindFiles(dir);
            Assert.Equal(2, found.Count);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // CleanDirectory

    [Fact]
    public void CleanDirectory_ExistingDirWithFiles_CreatesEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "file.txt"), "data");
        try
        {
            FileUtils.CleanDirectory(dir);
            Assert.True(Directory.Exists(dir));
            Assert.Empty(Directory.GetFiles(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void CleanDirectory_NonExistentDir_Creates()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Assert.False(Directory.Exists(dir));
            FileUtils.CleanDirectory(dir);
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    // CreateParentOfPath

    [Fact]
    public void CreateParentOfPath_CreatesParentDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "sub");
        var filePath = Path.Combine(dir, "file.txt");
        try
        {
            FileUtils.CreateParentOfPath(filePath);
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(dir))) Directory.Delete(Path.GetDirectoryName(dir)!, true);
        }
    }

    [Fact]
    public void CreateParentOfPath_ExistingParent_NoOp()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "file.txt");
        try
        {
            FileUtils.CreateParentOfPath(filePath); // should not throw
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // StreamReaderFromString

    [Fact]
    public void StreamReaderFromString_RoundTrip()
    {
        using var reader = FileUtils.StreamReaderFromString("hello world");
        var result = reader.ReadToEnd();
        Assert.Equal("hello world", result);
    }

    // CreateDirectoryForFile

    [Fact]
    public void CreateDirectoryForFile_CreatesDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "nested");
        var filePath = Path.Combine(dir, "output.json");
        try
        {
            FileUtils.CreateDirectoryForFile(filePath);
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(dir))) Directory.Delete(Path.GetDirectoryName(dir)!, true);
        }
    }

    // SetupCleanDirectory

    [Fact]
    public void SetupCleanDirectory_ExistingDirWithFiles_DeletesFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "old.txt"), "data");
        try
        {
            FileUtils.SetupCleanDirectory(dir);
            Assert.True(Directory.Exists(dir));
            Assert.Empty(Directory.GetFiles(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SetupCleanDirectory_Recursive_DeletesSubdirectories()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var subDir = Path.Combine(dir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "data");
        try
        {
            FileUtils.SetupCleanDirectory(dir, recursive: true);
            Assert.True(Directory.Exists(dir));
            Assert.Empty(Directory.GetDirectories(dir));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SetupCleanDirectory_NonExistentDir_Creates()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            FileUtils.SetupCleanDirectory(dir);
            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    // LogStream

    [Fact]
    public void LogStream_CreatesFileAndWritesLine()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test.log");
        try
        {
            using (var sw = FileUtils.LogStream(path))
            {
                sw.WriteLine("test line");
            }
            var content = File.ReadAllText(path, Encoding.UTF8);
            Assert.Contains("test line", content);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
