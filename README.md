# Kothf.Logging.File

A library contains a lightweight implementation of the `Microsoft.Extensions.Logging.ILoggerProvider` interface for file logging.

The code is heavily cribbed from the `Azure App Service Logging Provider` from the ASP.NET team.

The configuration reloading feature was eliminated.

## Usage

### appsettings.json

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Warning"
        },
        "FileLog": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft": "Warning",
                "Microsoft.Hosting.Lifetime": "Warning"
            }
        }
    },
    "FileLoggerOptions": {
        "LogDirectory": "C:\\logs",
        "FileName": "log-"
    }
}
```

### Program.cs

```csharp
internal class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var builder = Host.CreateEmptyApplicationBuilder(new() { Args = args });

            builder.Configuration
                .AddJsonFile("appsettings.json");

            var fileLoggerOptions = builder.Configuration.GetSection("FileLoggerOptions");
            builder.Services.Configure<FileLoggerOptions>(fileLoggerOptions);
            
            builder.Logging
                .AddFileLog();

            var host = builder.Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }
}
```
