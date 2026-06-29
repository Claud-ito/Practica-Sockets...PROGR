using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PracticaSockets_Core.Helpers
{
    public static class CertificateHelper
    {
        public static X509Certificate2 GenerateSelfSignedCertificate(
            string commonName = "PracticaSockets")
        {
            using (var rsa = RSA.Create())
            {
                rsa.KeySize = 2048;
                var req = new CertificateRequest(
                    $"CN={commonName}, O=Universidad, C=BO",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // No es una CA (no puede firmar otros certificados)
                req.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, false));

                // Firma digital + encriptado de la clave de sesión TLS
                req.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                        false));

                // OID 1.3.6.1.5.5.7.3.1 = autenticación de servidor TLS
                req.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") },
                        false));

                // SAN: el cliente aceptará conexiones a "localhost" y "127.0.0.1"
                var san = new SubjectAlternativeNameBuilder();
                san.AddDnsName("localhost");
                san.AddIpAddress(IPAddress.Loopback);
                req.CertificateExtensions.Add(san.Build());

                var cert = req.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddYears(2));

                // Re-exportar como PFX (incluye la clave privada) para que SslStream lo acepte
                return new X509Certificate2(
                    cert.Export(X509ContentType.Pfx),
                    (string)null,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.Exportable);
            }
        }
    }
}
