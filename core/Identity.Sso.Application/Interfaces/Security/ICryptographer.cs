namespace Identity.Sso.Application.Interfaces.Security;

public interface ICryptographer
{
    public string ErrorMessage { get; set; }
    string Encrypt(string text);
    string Encrypt(string text, string key);
    string Decrypt(string text);
    string Decrypt(string text, string key);
    string GenerateAdministratorHash(string password);
    string GenerateUserHash(string userName, string password);
    bool GetEncryptedInteger(string phrase, out int code);

}
