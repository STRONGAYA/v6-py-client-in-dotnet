using System.Diagnostics;
using V6DotNet.Configuration;
using Microsoft.Extensions.Options;
using Python.Runtime;

namespace V6DotNet.Services;

public class PythonEnvironmentManager : IPythonEnvironmentManager
{
    private readonly string _pythonHome;
    private readonly string _venvPath;
    private readonly string _pythonDll;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public PythonEnvironmentManager(IOptions<Vantage6Options> options)
    {
        _pythonHome = options.Value.PythonHome;
        _venvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".venv");
        _pythonDll = Path.Combine(_pythonHome, "python310.dll");
    }

    public void Initialize()
    {
        if (_isInitialized) return;

        try
        {
            SetupEnvironment();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize Python environment", ex);
        }
    }

    private void SetupEnvironment()
    {
        if (!File.Exists(_pythonDll))
        {
            throw new FileNotFoundException($"Python 3.10 DLL not found at: {_pythonDll}");
        }

        if (!Directory.Exists(_venvPath))
        {
            CreateAndSetupVirtualEnvironment();
        }

        ConfigureEnvironment();
        InitializePythonEngine();
    }

    private void CreateAndSetupVirtualEnvironment()
    {
        Console.WriteLine("Creating new virtual environment...");
        CreateVirtualEnvironment(Path.Combine(_pythonHome, "python.exe"), _venvPath);
        InstallRequirements(_venvPath);
        VerifyVantage6Installation(_venvPath);
    }

    private void CreateVirtualEnvironment(string pythonPath, string venvPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"-m venv \"{venvPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Python process");
        }
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Failed to create virtual environment. Error: {error}");
        }
        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output);
        }
    }

    private void ConfigureEnvironment()
    {
        var sitePackages = Path.Combine(_venvPath, "Lib", "site-packages");
        var venvLib = Path.Combine(_venvPath, "Lib");
        var venvScripts = Path.Combine(_venvPath, "Scripts");
        var venvDlls = Path.Combine(_venvPath, "DLLs");
        var pythonLib = Path.Combine(_pythonHome, "Lib");
        var pythonDlls = Path.Combine(_pythonHome, "DLLs");

        Environment.SetEnvironmentVariable("PYTHONHOME", _pythonHome);
        Environment.SetEnvironmentVariable("VIRTUAL_ENV", _venvPath);
        
        var pythonPath = $"{sitePackages};{venvLib};{venvScripts};{venvDlls};" +
                        $"{pythonLib};{pythonDlls};{_pythonHome}";
        Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);

        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var newPath = $"{venvScripts};{_venvPath};{_pythonHome};{pythonDlls};{currentPath}";
        Environment.SetEnvironmentVariable("PATH", newPath);
    }

    private void InitializePythonEngine()
    {
        Runtime.PythonDLL = _pythonDll;
        Environment.SetEnvironmentVariable("PYTHONNET_NO_SERIALIZE", "1");
        PythonEngine.Initialize();

        using (Py.GIL())
        {
            ModifyPythonPath();
        }
    }

    private void ModifyPythonPath()
    {
        dynamic sys = Py.Import("sys");
        sys.path.clear();

        var paths = new[]
        {
            Path.Combine(_venvPath, "Lib", "site-packages"),
            Path.Combine(_venvPath, "Lib"),
            Path.Combine(_venvPath, "Scripts"),
            _venvPath,
            Path.Combine(_pythonHome, "Lib"),
            Path.Combine(_pythonHome, "DLLs"),
            _pythonHome
        };

        foreach (var path in paths.Where(Directory.Exists))
        {
            sys.path.insert(0, path);
            Console.WriteLine($"Added to sys.path: {path}");
        }
    }

    private void InstallRequirements(string venvPath)
    {
        var pythonExe = Path.Combine(venvPath, "Scripts", "python.exe");
        
        Console.WriteLine("Upgrading pip...");
        RunProcess(pythonExe, "-m pip install --upgrade pip");

        var pipPath = Path.Combine(venvPath, "Scripts", "pip.exe");
        Console.WriteLine("\nInstalling vantage6...");
        RunProcess(pipPath, "install vantage6");
    }

    private void RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process: {fileName}");
        }
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Process failed. Error: {error}\nOutput: {output}");
        }
        Console.WriteLine(output);
    }

    private void VerifyVantage6Installation(string venvPath)
    {
        Console.WriteLine("\nVerifying installation...");
        var pipPath = Path.Combine(venvPath, "Scripts", "pip.exe");
        RunProcess(pipPath, "list");
    }

    public void Dispose()
    {
        if (_isInitialized && PythonEngine.IsInitialized)
        {
            try
            {
                PythonEngine.Shutdown();
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("Note: Ignored serialization warning during shutdown.");
            }
        }
    }
}