namespace V6DotNet.Configuration;

public class Vantage6Options
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MfaKey { get; set; } = string.Empty;
    public string ApiPath { get; set; } = string.Empty;
    public string PythonHome { get; set; } = string.Empty;
    public string OrganizationKey { get; set; } = string.Empty;
    public int DefaultCollaborationId { get; set; }
    public int[] DefaultOrganizationIds { get; set; } = Array.Empty<int>();

    public string Vantage6Version { get; set; } = string.Empty;
}