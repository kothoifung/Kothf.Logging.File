using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Kothf.Logging.File.Tests;

internal class Program
{
    internal static async Task Main(string[] args)
    {
        try
        {
            var builder = Host.CreateEmptyApplicationBuilder(new() { Args = args });

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            builder.Logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
            });

            // 1. Configure file logger options from configuration file
            var fileLoggerOptions = builder.Configuration.GetRequiredSection("FileLoggerOptions");
            // 2. Ensure log directory exists
            string? logDirectory = fileLoggerOptions.GetValue<string>("LogDirectory");
            ArgumentException.ThrowIfNullOrEmpty(logDirectory);
            Directory.CreateDirectory(logDirectory!);
            // 3. Register file logger options and add file logger
            builder.Services.Configure<FileLoggerOptions>(fileLoggerOptions);
            builder.Logging.AddFile();

            builder.Services
                .AddHostedService<TestsWorker>();

            var host = builder.Build();

            await host.RunAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }
}
