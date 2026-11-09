using System;
using System.IO;
using Core.Configuration;
using Xunit;

namespace Core.Tests;

public class StorageOptionsTests
{
    private static string NewTempDir() =>
        Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");

    [Fact] // відносний шлях відраховується від базового каталогу, каталог створюється
    public void ResolveFullPath_RelativeDataPath_IsCombinedWithBaseDir_AndDirectoryCreated()
    {
        string baseDir = NewTempDir();
        try
        {
            var options = new StorageOptions { DataPath = "data", FileName = "orders.json" };

            string full = options.ResolveFullPath(baseDir);

            Assert.Equal(Path.GetFullPath(Path.Combine(baseDir, "data", "orders.json")), full);
            Assert.True(Directory.Exists(Path.Combine(baseDir, "data")));
        }
        finally
        {
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact] // абсолютний шлях не залежить від базового каталогу
    public void ResolveFullPath_AbsoluteDataPath_IgnoresBaseDir()
    {
        string root = NewTempDir();
        try
        {
            var options = new StorageOptions { DataPath = root, FileName = "o.json" };

            string full = options.ResolveFullPath("ignored-base-dir");

            Assert.Equal(Path.GetFullPath(Path.Combine(root, "o.json")), full);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}