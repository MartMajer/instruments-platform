namespace Platform.Application.Features.Notifications;

public sealed record AwsSnsSignatureVerificationRequest(
    string Type,
    string MessageId,
    string TopicArn,
    string Message,
    string? Subject,
    string Timestamp,
    string SignatureVersion,
    string Signature,
    string SigningCertUrl,
    string? SubscribeUrl = null,
    string? Token = null);

public interface IAwsSnsSignatureVerifier
{
    Task<bool> VerifyAsync(
        AwsSnsSignatureVerificationRequest request,
        CancellationToken cancellationToken);
}

public interface IAwsSnsSubscriptionConfirmer
{
    Task<bool> ConfirmAsync(
        string subscribeUrl,
        CancellationToken cancellationToken);
}
