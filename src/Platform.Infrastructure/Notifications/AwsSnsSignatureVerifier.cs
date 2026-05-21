using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Platform.Application.Features.Notifications;

namespace Platform.Infrastructure.Notifications;

public sealed class AwsSnsSignatureVerifier(
    HttpClient httpClient,
    ILogger<AwsSnsSignatureVerifier> logger) : IAwsSnsSignatureVerifier
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
            logger.LogWarning("AWS SNS signature verifier rejected invalid input.");
            return false;
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(request.Signature);
        }
        catch (FormatException)
        {
            logger.LogWarning("AWS SNS signature verifier rejected invalid base64 signature.");
            return false;
        }

        HttpResponseMessage certificateResponse;
        try
        {
            certificateResponse = await httpClient.GetAsync(
                certificateUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "AWS SNS signing certificate download failed. CertHost={CertHost}.",
                certificateUri.Host);
            return false;
        }

        using (certificateResponse)
        {
        if (!certificateResponse.IsSuccessStatusCode ||
            certificateResponse.Content.Headers.ContentLength is > MaxCertificateBytes)
        {
            logger.LogWarning(
                "AWS SNS signing certificate response rejected. StatusCode={StatusCode}; ContentLength={ContentLength}.",
                (int)certificateResponse.StatusCode,
                certificateResponse.Content.Headers.ContentLength);
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
                logger.LogWarning(
                    "AWS SNS signing certificate payload rejected while reading. MaxBytes={MaxBytes}.",
                    MaxCertificateBytes);
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
            logger.LogWarning("AWS SNS signing certificate could not be loaded.");
            return false;
        }

        try
        {
            using (certificate)
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                if (!chain.Build(certificate))
                {
                    logger.LogWarning(
                        "AWS SNS signing certificate chain validation failed. Statuses={ChainStatuses}.",
                        string.Join(",", chain.ChainStatus.Select(status => status.Status.ToString())));
                    return false;
                }

                using var rsa = certificate.GetRSAPublicKey();
                if (rsa is null)
                {
                    logger.LogWarning("AWS SNS signing certificate has no RSA public key.");
                    return false;
                }

                var stringToSign = BuildStringToSign(request);
                if (stringToSign is null)
                {
                    logger.LogWarning(
                        "AWS SNS signature verifier could not build primary string to sign. Type={MessageType}.",
                        request.Type);
                    return false;
                }

                if (VerifySignature(rsa, stringToSign, signature))
                {
                    logger.LogInformation("AWS SNS signature verified with primary string.");
                    return true;
                }

                var compatibilityStringToSign = BuildSubscriptionConfirmationMessageFormatStringToSign(request);
                if (compatibilityStringToSign is not null &&
                    VerifySignature(rsa, compatibilityStringToSign, signature))
                {
                    logger.LogInformation("AWS SNS signature verified with subscription compatibility string.");
                    return true;
                }

                logger.LogWarning(
                    "AWS SNS signature mismatch after all string variants. Type={MessageType}.",
                    request.Type);
                return false;
            }
        }
        catch (CryptographicException)
        {
            logger.LogWarning("AWS SNS signature verifier hit a cryptographic error.");
            return false;
        }
        }
    }

    private static bool VerifySignature(RSA rsa, string stringToSign, byte[] signature)
    {
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

    private static string? BuildSubscriptionConfirmationMessageFormatStringToSign(
        AwsSnsSignatureVerificationRequest request)
    {
        if (!string.Equals(request.Type, "SubscriptionConfirmation", StringComparison.Ordinal))
        {
            return null;
        }

        return new StringBuilder()
            .Append("Message\n").Append(request.Message).Append('\n')
            .Append("MessageId\n").Append(request.MessageId).Append('\n')
            .Append("Timestamp\n").Append(request.Timestamp).Append('\n')
            .Append("TopicArn\n").Append(request.TopicArn).Append('\n')
            .Append("Type\n").Append(request.Type)
            .ToString();
    }
}
