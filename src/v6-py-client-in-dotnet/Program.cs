using V6DotNet.Configuration;
using V6DotNet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Python.Runtime;

namespace V6DotNet;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile(
                    $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                    true)
                .Build();

            services.Configure<Vantage6Options>(options =>
                configuration.GetSection("Vantage6").Bind(options)
            );

            services.AddSingleton<IPythonEnvironmentManager, PythonEnvironmentManager>();
            services.AddScoped<IVantage6Client, Vantage6Client>();

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var pythonEnv = scope.ServiceProvider.GetRequiredService<IPythonEnvironmentManager>();
                pythonEnv.Initialize();

                var client = scope.ServiceProvider.GetRequiredService<IVantage6Client>();

                bool connected = await client.ConnectAsync();
                if (!connected)
                {
                    Console.WriteLine("Failed to connect to Vantage6 server");
                    return;
                }

                using (Py.GIL())
                {
                    // Convert strings to Python strings
                    using var methodKey = new PyString("method");
                    using var methodValue = new PyString("central_average");
                    using var kwargsKey = new PyString("kwargs");
                    using var columnKey = new PyString("column_name");
                    using var columnValue = new PyString("Age");

                    // Create input dictionary for the averaging algorithm
                    dynamic input = new PyDict();
                    dynamic kwargs = new PyDict();

                    input[methodKey] = methodValue;
                    kwargs[columnKey] = columnValue;
                    input[kwargsKey] = kwargs;

                    Console.WriteLine("Creating and executing task...");
                    var result = await client.CreateAndWaitForTaskAsync(
                        name: "default-test-average-task",
                        image: "harbor2.vantage6.ai/demo/average",
                        input: input,
                        description: "Test average task",
                        databaseLabels: new[] { "default" }
                    );

                    Console.WriteLine("Task completed!");
                    Console.WriteLine($"Result info:");
                    Console.WriteLine(result);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }
}