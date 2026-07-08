using System.Security.Cryptography;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Features.Reports;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

/// <summary>
/// Validates and stores a tenant logo, returning the object key the branding
/// write (<see cref="UpdateTenantAppBrandingCommand"/>) references. Storage and
/// persistence are deliberately separate: this vets the bytes and puts them in
/// object storage under a tenant-scoped key; the PUT then adopts that key. The
/// tenant is always the authenticated caller's — never taken from the client.
/// </summary>
public sealed record UploadTenantAppBrandingLogoCommand(string? ContentType, byte[] Content)
    : IRequest<Result<TenantAppBrandingLogoUploadResponse>>;

public sealed class UploadTenantAppBrandingLogoHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IExportArtifactObjectStore objectStore)
    : IRequestHandler<UploadTenantAppBrandingLogoCommand, Result<TenantAppBrandingLogoUploadResponse>>
{
    public async Task<Result<TenantAppBrandingLogoUploadResponse>> Handle(
        UploadTenantAppBrandingLogoCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Result.Failure<TenantAppBrandingLogoUploadResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required."));
        }

        var validated = TenantAppBrandingLogo.Validate(command.ContentType, command.Content);
        if (validated.IsFailure)
        {
            return Result.Failure<TenantAppBrandingLogoUploadResponse>(validated.Error);
        }

        var metadata = validated.Value;
        var storageKey = BuildStorageKey(currentTenant.TenantId, metadata, command.Content);

        var stored = await objectStore.StoreAsync(storageKey, command.Content, cancellationToken);
        if (stored.IsFailure)
        {
            return Result.Failure<TenantAppBrandingLogoUploadResponse>(stored.Error);
        }

        return Result.Success(new TenantAppBrandingLogoUploadResponse(
            storageKey,
            metadata.ContentType,
            metadata.Width,
            metadata.Height,
            metadata.ByteSize));
    }

    private static string BuildStorageKey(Guid tenantId, TenantAppBrandingLogoMetadata metadata, byte[] content)
    {
        // Content-addressed name under a tenant-scoped prefix: re-uploading
        // identical bytes reuses the key, and the key only ever travels back to
        // the tenant that uploaded it. The "N" GUID format and the hex digest
        // stay inside the object store's safe-key charset ([A-Za-z0-9-_.]).
        var digest = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant()[..16];
        var extension = Extension(metadata.ContentType);

        return $"tenant-branding/{tenantId:N}/logo-{digest}.{extension}";
    }

    private static string Extension(string contentType)
    {
        return contentType switch
        {
            TenantAppBrandingLogo.ContentTypePng => "png",
            TenantAppBrandingLogo.ContentTypeJpeg => "jpg",
            TenantAppBrandingLogo.ContentTypeWebp => "webp",
            _ => "bin"
        };
    }
}
