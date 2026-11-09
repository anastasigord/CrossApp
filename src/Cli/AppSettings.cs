using System;
using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cli;

public sealed record AppSettings(
    string EnvironmentName,
    AppOptions Options,
    StorageOptions Storage,
    string DataFile,
    string? ApiKey)
{
    public static AppSettings Load(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT");
        if (string.IsNullOrWhiteSpace(envName))
            envName = "Development";

        // Джерела за зростанням пріоритету: пізніше джерело перекриває раніше
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .AddCommandLine(args)
            .Build();

        var options = config.Get<AppOptions>() ?? new AppOptions();

        // Проста змінна CROSSAPP_DATA_PATH перекриває розділ Storage
        var legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        var storage = new StorageOptions
        {
            DataPath = string.IsNullOrWhiteSpace(legacy) ? options.Storage.DataPath : legacy,
            FileName = options.Storage.FileName
        };

        return new AppSettings(
            envName,
            options,
            storage,
            storage.ResolveFullPath(AppContext.BaseDirectory),
            config["Integration:ApiKey"]);
    }
}