using System.Text;
using Platform.Domain.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record TenantAppBrandingLogoMetadata(
    string ContentType,
    int Width,
    int Height,
    int ByteSize);

/// <summary>
/// Validates a tenant-uploaded logo without any image library: it sniffs the
/// magic bytes to identify the real format (so a client cannot smuggle HTML or
/// script behind an "image/png" label), cross-checks that against the declared
/// content type, enforces byte-size and pixel-dimension caps, and parses the
/// dimensions straight from the header. Pure and deterministic — the object
/// store never sees bytes this has not vetted.
/// </summary>
public static class TenantAppBrandingLogo
{
    /// <summary>Upload cap. Kept small so it can be embedded in the token-scoped respondent response.</summary>
    public const int MaxBytes = 256 * 1024;

    public const int MaxDimension = 1024;
    public const int MinDimension = 16;

    public const string ContentTypePng = "image/png";
    public const string ContentTypeJpeg = "image/jpeg";
    public const string ContentTypeWebp = "image/webp";

    public static Result<TenantAppBrandingLogoMetadata> Validate(string? declaredContentType, byte[]? content)
    {
        if (content is null || content.Length == 0)
        {
            return Result.Failure<TenantAppBrandingLogoMetadata>(
                Error.Validation("app_branding_logo.empty", "No logo file was uploaded."));
        }

        if (content.Length > MaxBytes)
        {
            return Result.Failure<TenantAppBrandingLogoMetadata>(
                Error.Validation(
                    "app_branding_logo.too_large",
                    $"Logo is {content.Length / 1024} KB — the limit is {MaxBytes / 1024} KB."));
        }

        var declared = NormalizeContentType(declaredContentType);
        if (declared is null || !Tenant.IsAppBrandingLogoContentType(declared))
        {
            return Result.Failure<TenantAppBrandingLogoMetadata>(
                Error.Validation(
                    "app_branding_logo.unsupported_type",
                    "Logo must be a PNG, JPEG, or WebP image."));
        }

        var sniffed = SniffContentType(content);
        if (sniffed is null)
        {
            return Result.Failure<TenantAppBrandingLogoMetadata>(
                Error.Validation(
                    "app_branding_logo.unsupported_type",
                    "Logo must be a PNG, JPEG, or WebP image."));
        }

        if (!string.Equals(sniffed, declared, StringComparison.Ordinal))
        {
            return Result.Failure<TenantAppBrandingLogoMetadata>(
                Error.Validation(
                    "app_branding_logo.content_mismatch",
                    "The uploaded file is not the image type it claims to be."));
        }

        var dimensions = ReadDimensions(sniffed, content);
        if (dimensions is not { } parsed || parsed.Width <= 0 || parsed.Height <= 0)
        {
            return Result.Failure<TenantAppBrandingLogoMetadata>(
                Error.Validation(
                    "app_branding_logo.unreadable",
                    "The image could not be read. Try re-exporting the logo."));
        }

        var (width, height) = parsed;

        if (width > MaxDimension || height > MaxDimension)
        {
            return Result.Failure<TenantAppBrandingLogoMetadata>(
                Error.Validation(
                    "app_branding_logo.dimensions_too_large",
                    $"Logo is {width}×{height}px — the limit is {MaxDimension}px per side."));
        }

        if (width < MinDimension || height < MinDimension)
        {
            return Result.Failure<TenantAppBrandingLogoMetadata>(
                Error.Validation(
                    "app_branding_logo.dimensions_too_small",
                    $"Logo is {width}×{height}px — it must be at least {MinDimension}px per side."));
        }

        return Result.Success(new TenantAppBrandingLogoMetadata(sniffed, width, height, content.Length));
    }

    private static string? NormalizeContentType(string? contentType)
    {
        var normalized = contentType?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        // Strip any "; charset=..." suffix a client might tack on.
        var separator = normalized.IndexOf(';');
        if (separator >= 0)
        {
            normalized = normalized[..separator].Trim();
        }

        return normalized switch
        {
            "image/jpg" => ContentTypeJpeg,
            _ => normalized
        };
    }

    private static string? SniffContentType(byte[] content)
    {
        if (IsPng(content))
        {
            return ContentTypePng;
        }

        if (IsJpeg(content))
        {
            return ContentTypeJpeg;
        }

        if (IsWebp(content))
        {
            return ContentTypeWebp;
        }

        return null;
    }

    private static bool IsPng(byte[] content)
    {
        return content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A;
    }

    private static bool IsJpeg(byte[] content)
    {
        return content.Length >= 3 &&
            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;
    }

    private static bool IsWebp(byte[] content)
    {
        return content.Length >= 12 &&
            content[0] == (byte)'R' && content[1] == (byte)'I' && content[2] == (byte)'F' && content[3] == (byte)'F' &&
            content[8] == (byte)'W' && content[9] == (byte)'E' && content[10] == (byte)'B' && content[11] == (byte)'P';
    }

    private static (int Width, int Height)? ReadDimensions(string contentType, byte[] content)
    {
        return contentType switch
        {
            ContentTypePng => ReadPngDimensions(content),
            ContentTypeJpeg => ReadJpegDimensions(content),
            ContentTypeWebp => ReadWebpDimensions(content),
            _ => null
        };
    }

    private static (int Width, int Height)? ReadPngDimensions(byte[] content)
    {
        // IHDR is the first chunk: width and height are big-endian uint32 at 16 and 20.
        if (content.Length < 24)
        {
            return null;
        }

        var width = ReadUInt32BigEndian(content, 16);
        var height = ReadUInt32BigEndian(content, 20);

        return (width, height);
    }

    private static (int Width, int Height)? ReadJpegDimensions(byte[] content)
    {
        var offset = 2; // skip SOI (FF D8)
        while (offset + 9 < content.Length)
        {
            if (content[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            var marker = content[offset + 1];
            // Standalone markers with no length payload.
            if (marker is 0xD8 or 0xD9 || (marker >= 0xD0 && marker <= 0xD7) || marker == 0x01)
            {
                offset += 2;
                continue;
            }

            var segmentLength = ReadUInt16BigEndian(content, offset + 2);
            if (segmentLength < 2)
            {
                return null;
            }

            // Start-Of-Frame markers carry the dimensions (skip the differential/arithmetic gaps).
            var isSof = marker is >= 0xC0 and <= 0xCF &&
                marker is not (0xC4 or 0xC8 or 0xCC);
            if (isSof)
            {
                if (offset + 9 >= content.Length)
                {
                    return null;
                }

                var height = ReadUInt16BigEndian(content, offset + 5);
                var width = ReadUInt16BigEndian(content, offset + 7);

                return (width, height);
            }

            offset += 2 + segmentLength;
        }

        return null;
    }

    private static (int Width, int Height)? ReadWebpDimensions(byte[] content)
    {
        if (content.Length < 16)
        {
            return null;
        }

        var format = Encoding.ASCII.GetString(content, 12, 4);
        switch (format)
        {
            case "VP8 ":
                // Lossy: 16-bit width/height (14 bits used) at offset 26/28.
                if (content.Length < 30)
                {
                    return null;
                }

                var lossyWidth = (content[26] | (content[27] << 8)) & 0x3FFF;
                var lossyHeight = (content[28] | (content[29] << 8)) & 0x3FFF;
                return (lossyWidth, lossyHeight);

            case "VP8L":
                // Lossless: 14-bit width-1 and height-1 packed after the 0x2F signature.
                if (content.Length < 25 || content[20] != 0x2F)
                {
                    return null;
                }

                var bits = content[21] | (content[22] << 8) | (content[23] << 16) | (content[24] << 24);
                var losslessWidth = (bits & 0x3FFF) + 1;
                var losslessHeight = ((bits >> 14) & 0x3FFF) + 1;
                return (losslessWidth, losslessHeight);

            case "VP8X":
                // Extended: 24-bit canvas width-1 / height-1 (little-endian) at offset 24/27.
                if (content.Length < 30)
                {
                    return null;
                }

                var extWidth = (content[24] | (content[25] << 8) | (content[26] << 16)) + 1;
                var extHeight = (content[27] | (content[28] << 8) | (content[29] << 16)) + 1;
                return (extWidth, extHeight);

            default:
                return null;
        }
    }

    private static int ReadUInt32BigEndian(byte[] content, int offset)
    {
        return (content[offset] << 24) |
            (content[offset + 1] << 16) |
            (content[offset + 2] << 8) |
            content[offset + 3];
    }

    private static int ReadUInt16BigEndian(byte[] content, int offset)
    {
        return (content[offset] << 8) | content[offset + 1];
    }
}
