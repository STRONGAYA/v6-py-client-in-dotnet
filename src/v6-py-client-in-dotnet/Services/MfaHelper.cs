using OtpNet;

namespace V6DotNet.Services;

public class MfaHelper
{
    private readonly string _mfaKey;

    public MfaHelper(string mfaKey)
    {
        _mfaKey = mfaKey;
    }

    public string? GenerateMfaCode()
    {
        if (string.IsNullOrEmpty(_mfaKey))
        {
            return null;
        }

        try
        {
            var bytes = Base32Encoding.ToBytes(_mfaKey.Trim());  // Added Trim() for safety
            var totp = new Totp(bytes);
            return totp.ComputeTotp();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate MFA code: {ex.Message}", ex);
        }
    }
}