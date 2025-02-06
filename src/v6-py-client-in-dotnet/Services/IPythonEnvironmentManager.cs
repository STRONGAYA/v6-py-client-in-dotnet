namespace V6DotNet.Services;

public interface IPythonEnvironmentManager : IDisposable
{
    void Initialize();
    bool IsInitialized { get; }
}