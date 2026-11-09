using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class JsonOrderFileTests
{
    private static string NewTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Fact] // повний цикл: збереження і читання, кирилиця читабельна
    public void SaveThenLoad_RoundTripsOrders_AndKeepsCyrillicReadable()
    {
        string dir = NewTempDir();
        try
        {
            string path = Path.Combine(dir, "orders.json");

            JsonOrderFile.Save(path, TestData.Items());

            string text = File.ReadAllText(path);
            Assert.Contains("Кабель", text);
            Assert.DoesNotContain("\\u", text);

            var loaded = JsonOrderFile.Load(path);
            Assert.Equal(4, loaded.Count);
            Assert.Equal(TestData.Items().Sum(o => o.Total), loaded.Sum(o => o.Total));
        }
        finally { Cleanup(dir); }
    }

    [Fact] // атомарність: файл перезаписано, тимчасового файлу не лишилось
    public void Save_OverwritesFile_AndLeavesNoTmpFile()
    {
        string dir = NewTempDir();
        try
        {
            string path = Path.Combine(dir, "orders.json");

            JsonOrderFile.Save(path, TestData.Items());
            JsonOrderFile.Save(path, TestData.Items().Take(1));

            Assert.Single(JsonOrderFile.Load(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally { Cleanup(dir); }
    }

    [Fact] // немає файлу: порожня колекція, а не виняток
    public void Load_WhenFileMissing_ReturnsEmpty()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

        Assert.Empty(JsonOrderFile.Load(path));
    }

    [Fact] // зіпсований JSON: зрозуміла помилка, початкова причина збережена
    public void Load_WithBrokenJson_ThrowsStorageException_WithInnerJsonException()
    {
        string dir = NewTempDir();
        try
        {
            string path = Path.Combine(dir, "orders.json");
            File.WriteAllText(path, "{ це не json");

            var ex = Assert.Throws<StorageException>(() => JsonOrderFile.Load(path));

            Assert.IsAssignableFrom<JsonException>(ex.InnerException);
        }
        finally { Cleanup(dir); }
    }

    [Fact] // сховище зберігає дані між екземплярами; каталог створюється автоматично
    public void FileOrderStore_PersistsAcrossInstances()
    {
        string dir = NewTempDir();
        try
        {
            string path = Path.Combine(dir, "nested", "orders.json");
            var order = TestData.Items()[0];

            new FileOrderStore(path).Add(order);
            var second = new FileOrderStore(path);

            Assert.NotNull(second.GetById(order.Id));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally { Cleanup(dir); }
    }
}