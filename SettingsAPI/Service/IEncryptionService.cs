namespace SettingsAPI.Service
{
    public interface IEncryptionService
    {
        string EncryptWithSalt(string input, string salt);
        string GenerateSaltKey(int size);
        
        string ComputeSha256Hash(string input);

        string Base64Encode(string variable1, string variable2);
        
        string Base64Decode(string base64EncodedVariable);
    }
}