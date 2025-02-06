using V6DotNet.Configuration;
using Microsoft.Extensions.Options;
using Python.Runtime;

namespace V6DotNet.Services;

public class Vantage6Client : IVantage6Client
{
    private readonly Vantage6Options _options;
    private readonly IPythonEnvironmentManager _pythonEnv;
    private dynamic? _client;

    public Vantage6Client(
        IOptions<Vantage6Options> options,
        IPythonEnvironmentManager pythonEnv)
    {
        _options = options.Value;
        _pythonEnv = pythonEnv;
    }

    public Task<bool> ConnectAsync()
    {
        try
        {
            using (Py.GIL())
            {
                if (_client == null)
                {
                    dynamic vantage6 = Py.Import("vantage6.client");
                    _client = vantage6.Client(_options.Host, _options.Port, _options.ApiPath);
                }

                _client.authenticate(_options.Username, _options.Password);
                return Task.FromResult(true);
            }
        }
        catch (PythonException ex)
        {
            Console.WriteLine($"Failed to connect to Vantage6: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task SetupEncryptionAsync(string? organizationKey = null)
    {
        EnsureConnected();
        using (Py.GIL())
        {
            string key = organizationKey ?? _options.OrganizationKey;
            if (!string.IsNullOrEmpty(key))
            {
                _client!.setup_encryption(key);
            }

            return Task.CompletedTask;
        }
    }

    public Task<dynamic> GetCollaborationsAsync()
    {
        EnsureConnected();
        using (Py.GIL())
        {
            dynamic result = _client!.collaboration.list();
            return Task.FromResult<dynamic>(result);
        }
    }

    public Task<dynamic> CreateTaskAsync(int collaborationId, string image, dynamic input)
    {
        EnsureConnected();
        using (Py.GIL())
        {
            dynamic result = _client!.post_task(
                collaboration_id: collaborationId,
                image: image,
                input: input
            );
            return Task.FromResult<dynamic>(result);
        }
    }

    public async Task<dynamic> CreateAndWaitForTaskAsync(
        string name,
        string image,
        dynamic input,
        string description = "",
        int? collaborationId = null,
        int[]? organizationIds = null,
        string[]? databaseLabels = null)
    {
        EnsureConnected();
        using (Py.GIL())
        {
            collaborationId ??= _options.DefaultCollaborationId;
            organizationIds ??= _options.DefaultOrganizationIds;

            dynamic databases = new PyList();
            if (databaseLabels != null)
            {
                foreach (var label in databaseLabels)
                {
                    dynamic dbDict = new PyDict();
                    using var labelKey = new PyString("label");
                    using var labelValue = new PyString(label);
                    dbDict[labelKey] = labelValue;
                    databases.Append(dbDict);
                }
            }

            // Convert all string parameters to PyString
            using var pyName = new PyString(name);
            using var pyImage = new PyString(image);
            using var pyDescription = new PyString(description);
            using var pyCollab = new PyInt(collaborationId.Value);

            // Convert organizationIds array to Python list
            dynamic pyOrgIds = new PyList();
            foreach (var orgId in organizationIds)
            {
                using var pyOrgId = new PyInt(orgId);
                pyOrgIds.Append(pyOrgId);
            }

            dynamic task = _client!.task.create(
                collaboration: pyCollab,
                organizations: pyOrgIds,
                name: pyName,
                image: pyImage,
                description: pyDescription,
                databases: databases,
                input_: input
            );

            // Extract task ID using dictionary access
            dynamic taskId;
            if (task is PyObject pyTask)
            {
                using var idKey = new PyString("id");
                taskId = pyTask[idKey];
            }
            else
            {
                taskId = task["id"];
            }

            Console.WriteLine($"Waiting for results of task {taskId}...");
            dynamic result = await WaitForResultsAsync(taskId);

            dynamic resultInfo = _client!.result.from_task(task_id: taskId);
            return resultInfo;
        }
    }

    public Task<dynamic> WaitForResultsAsync(dynamic taskId)
    {
        EnsureConnected();
        using (Py.GIL())
        {
            dynamic result = _client!.wait_for_results(taskId);
            return Task.FromResult<dynamic>(result);
        }
    }

    public Task<dynamic> GetTaskResultAsync(dynamic taskId)
    {
        EnsureConnected();
        using (Py.GIL())
        {
            dynamic result = _client!.get_task_result(taskId);
            return Task.FromResult<dynamic>(result);
        }
    }

    private void EnsureConnected()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Not connected to Vantage6. Call ConnectAsync first.");
        }
    }
}