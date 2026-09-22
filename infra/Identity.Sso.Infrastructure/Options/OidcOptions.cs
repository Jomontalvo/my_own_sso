using System.ComponentModel.DataAnnotations;

namespace Identity.Sso.Infrastructure.Options;

public sealed class OidcOptions
{
    public const string SectionName = "OidcConfig";

    /// <summary>Custom scopes exposed by this provider, on top of the standard OIDC ones.</summary>
    public string[] Scopes { get; set; } = [];

    /// <summary>Absolute issuer URI. Required when the provider runs behind a reverse proxy or gateway.</summary>
    [Url]
    public string? Issuer { get; set; }

    /// <summary>
    /// When true the access token is a plain (signed) JWT that resource servers can validate locally.
    /// Flip to false once every resource server can decrypt it or use introspection.
    /// </summary>
    public bool DisableAccessTokenEncryption { get; set; } = true;

    public string[] AllowedCorsOrigins { get; set; } = [];

    public CertificateOptions? SigningCertificate { get; set; }

    public CertificateOptions? EncryptionCertificate { get; set; }
}

public sealed class CertificateOptions
{
    /// <summary>Path to a PKCS#12 (.pfx) file. Mutually exclusive with <see cref="Thumbprint"/>.</summary>
    public string? Path { get; set; }

    public string? Password { get; set; }

    /// <summary>Thumbprint of a certificate installed in the machine's X.509 store.</summary>
    public string? Thumbprint { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Path) || !string.IsNullOrWhiteSpace(Thumbprint);
}
