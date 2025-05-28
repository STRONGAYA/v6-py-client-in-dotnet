using V6DotNet.Configuration;
using V6DotNet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Python.Runtime;
using System.Text.Json;

namespace V6DotNet;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide the path to algorithm_input.json");
            return;
        }

        var algorithmConfigPath = args[0];
        if (!File.Exists(algorithmConfigPath))
        {
            Console.WriteLine($"File not found: {algorithmConfigPath}");
            return;
        }

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

            // Read and parse the algorithm input JSON
            var algorithmConfig = JsonSerializer.Deserialize<AlgorithmConfiguration>(
                File.ReadAllText(algorithmConfigPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

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
                    // Create input dictionary for the algorithm
                    dynamic input = new PyDict();
                    using var methodKey = new PyString("method");
                    using var methodValue = new PyString(algorithmConfig.Input.Method);
                    using var kwargsKey = new PyString("kwargs");

                    input[methodKey] = methodValue;
                    input[kwargsKey] = ConvertToPyDict(algorithmConfig.Input.Kwargs);

                    Console.WriteLine("Creating and executing task...");
                    var result = await client.CreateAndWaitForTaskAsync(
                        name: algorithmConfig.Name,
                        image: algorithmConfig.Image,
                        input: input,
                        description: algorithmConfig.Description,
                        databaseLabels: algorithmConfig.DatabaseLabels
                    );

                    Console.WriteLine("Task completed!");
                    var data = result.GetItem("data");  // Get the 'data' list
                    var firstResult = data.GetItem(0);  // Get first item from the list
                    Console.WriteLine($"Result: {firstResult.GetItem("result")}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private static PyDict ConvertToPyDict(Dictionary<string, object> dict)
    {
        var pyDict = new PyDict();
        foreach (var kvp in dict)
        {
            using var key = new PyString(kvp.Key);
            dynamic value = ConvertToPyObject(kvp.Value);
            pyDict[key] = value;
        }
        return pyDict;
    }

    private static dynamic ConvertToPyObject(object value)
    {
        return value switch
        {
            string s => new PyString(s),
            Dictionary<string, object> dict => ConvertToPyDict(dict),
            JsonElement element => ConvertJsonElementToPyObject(element),
            _ => throw new ArgumentException($"Unsupported type: {value.GetType()}")
        };
    }

    private static dynamic ConvertJsonElementToPyObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new PyDict();
                foreach (var property in element.EnumerateObject())
                {
                    using var key = new PyString(property.Name);
                    dict[key] = ConvertJsonElementToPyObject(property.Value);
                }
                return dict;
            case JsonValueKind.Array:
                var list = new PyList();
                foreach (var item in element.EnumerateArray())
                {
                    list.Append(ConvertJsonElementToPyObject(item));
                }
                return list;
            case JsonValueKind.String:
                return new PyString(element.GetString());
            case JsonValueKind.Number:
                return new PyFloat(element.GetDouble());
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean().ToPython();
            case JsonValueKind.Null:
                return Runtime.None;
            default:
                throw new ArgumentException($"Unsupported JSON value kind: {element.ValueKind}");
        }
    }
}