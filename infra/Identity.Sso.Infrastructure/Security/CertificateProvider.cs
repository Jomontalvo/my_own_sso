using System.Security.Cryptography.X509Certificates;
using Identity.Sso.Infrastructure.Options;

namespace Identity.Sso.Infrastructure.Security;

public interface ICertificateProvider
{
    X509Certificate2 Load(CertificateOptions options, string purpose);
}

public sealed class CertificateProvider : ICertificateProvider
{
    public X509Certificate2 Load(CertificateOptions options, string purpose)
    {
        if (!string.IsNullOrWhiteSpace(options.Path))
        {
            if (!File.Exists(options.Path))
                throw new InvalidOperationException($"The {purpose} certificate file '{options.Path}' was not found.");

            return X509CertificateLoader.LoadPkcs12FromFile(
                options.Path,
                options.Password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
        }

        if (!string.IsNullOrWhiteSpace(options.Thumbprint))
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint, options.Thumbprint, validOnly: false);

            return certificates.Count > 0
                ? certificates[0]
                : throw new InvalidOperationException(
                    $"No {purpose} certificate with thumbprint '{options.Thumbprint}' was found in the LocalMachine store.");
        }

        throw new InvalidOperationException(
            $"The {purpose} certificate must define either a 'Path' or a 'Thumbprint'.");
    }
}
