using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Platform.Application.Features.Notifications;

namespace Platform.Infrastructure.Notifications;

public sealed class AwsSnsSignatureVerifier(HttpClient httpClient) : IAwsSnsSignatureVerifier
{
    private const int MaxCertificateBytes = 64 * 1024;

    public async Task<bool> VerifyAsync(
        AwsSnsSignatureVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.SignatureVersion, "2", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(request.Signature) ||
            !Uri.TryCreate(request.SigningCertUrl, UriKind.Absolute, out var certificateUri))
        {
            return false;
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(request.Signature);
        }
        catch (FormatException)
        {
            return false;
        }

        using var certificateResponse = await httpClient.GetAsync(
            certificateUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!certificateResponse.IsSuccessStatusCode ||
            certificateResponse.Content.Headers.ContentLength is > MaxCertificateBytes)
        {
            return false;
        }

        await using var certificateStream = await certificateResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var certificateBytes = new MemoryStream();
        var buffer = new byte[4096];
        while (true)
        {
            var bytesRead = await certificateStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            if (certificateBytes.Length + bytesRead > MaxCertificateBytes)
            {
                return false;
            }

            certificateBytes.Write(buffer.AsSpan(0, bytesRead));
        }

        X509Certificate2 certificate;
        try
        {
            certificate = X509CertificateLoader.LoadCertificate(certificateBytes.ToArray());
        }
        catch (CryptographicException)
        {
            return false;
        }

        using (certificate)
        using (var chain = new X509Chain())
        {
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            if (!chain.Build(certificate))
            {
                return false;
            }
        }

        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is null)
        {
            return false;
        }

        var stringToSign = BuildStringToSign(request);
        if (stringToSign is null)
        {
            return false;
        }

        return rsa.VerifyData(
            Encoding.UTF8.GetBytes(stringToSign),
            signature,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }

    private static string? BuildStringToSign(AwsSnsSignatureVerificationRequest request)
    {
        return request.Type switch
        {
            "Notification" => BuildNotificationStringToSign(request),
            "SubscriptionConfirmation" => BuildSubscriptionConfirmationStringToSign(request),
            _ => null
        };
    }

    private static string BuildNotificationStringToSign(AwsSnsSignatureVerificationRequest request)
    {
        var builder = new StringBuilder()
            .Append("Message\n").Append(request.Message).Append('\n')
            .Append("MessageId\n").Append(request.MessageId).Append('\n');

        if (!string.IsNullOrEmpty(request.Subject))
        {
            builder
                .Append("Subject\n").Append(request.Subject).Append('\n');
        }

        return builder
            .Append("Timestamp\n").Append(request.Timestamp).Append('\n')
            .Append("TopicArn\n").Append(request.TopicArn).Append('\n')
            .Append("Type\n").Append(request.Type)
            .ToString();
    }

    private static string? BuildSubscriptionConfirmationStringToSign(AwsSnsSignatureVerificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SubscribeUrl) ||
            string.IsNullOrWhiteSpace(request.Token))
        {
            return null;
        }

        return new StringBuilder()
            .Append("Message\n").Append(request.Message).Append('\n')
            .Append("MessageId\n").Append(request.MessageId).Append('\n')
            .Append("SubscribeURL\n").Append(request.SubscribeUrl).Append('\n')
            .Append("Timestamp\n").Append(request.Timestamp).Append('\n')
            .Append("Token\n").Append(request.Token).Append('\n')
            .Append("TopicArn\n").Append(request.TopicArn).Append('\n')
            .Append("Type\n").Append(request.Type)
            .ToString();
    }
}
