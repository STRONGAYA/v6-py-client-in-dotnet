public interface IVantage6Client
{
    Task<bool> ConnectAsync();
    Task SetupEncryptionAsync(string? organizationKey = null);
    Task<dynamic> GetCollaborationsAsync();
    Task<dynamic> CreateTaskAsync(int collaborationId, string image, dynamic input);
    Task<dynamic> GetTaskResultAsync(dynamic taskId);
    Task<dynamic> CreateAndWaitForTaskAsync(
        string name,
        string image,
        dynamic input,
        string description = "",
        int? collaborationId = null,
        int[]? organizationIds = null,
        string[]? databaseLabels = null
    );
    Task<dynamic> WaitForResultsAsync(dynamic taskId);
}