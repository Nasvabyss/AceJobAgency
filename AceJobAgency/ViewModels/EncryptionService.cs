using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

public class EncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("NRICProtector");
    }

    public string Encrypt(string input)
    {
        return _protector.Protect(input);
    }

    public string Decrypt(string encryptedData)
    {
        try
        {
            return _protector.Unprotect(encryptedData);
        }
        catch (CryptographicException ex)
        {
            // Log the exception details
            // Log.Error(ex, "Error decrypting data");

            // Decide on a return value for failed decryption
            return null; // or return string.Empty;
        }
    }

}
