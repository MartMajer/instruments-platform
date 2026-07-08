using System.Text;
using Platform.Application.Features.ProductSurfaces;

namespace Platform.UnitTests.Application;

public sealed class TenantAppBrandingLogoTests
{
    private static byte[] Png(int width, int height, int totalLength = 24)
    {
        var bytes = new byte[Math.Max(totalLength, 24)];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Array.Copy(signature, bytes, signature.Length);
        WriteUInt32BigEndian(bytes, 16, width);
        WriteUInt32BigEndian(bytes, 20, height);
        return bytes;
    }

    private static byte[] Jpeg(int width, int height)
    {
        // SOI (FF D8) then a SOF0 (FF C0) segment carrying the dimensions.
        var bytes = new byte[20];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        bytes[3] = 0xC0;
        bytes[4] = 0x00;
        bytes[5] = 0x11; // segment length
        bytes[6] = 0x08; // precision
        bytes[7] = (byte)(height >> 8);
        bytes[8] = (byte)(height & 0xFF);
        bytes[9] = (byte)(width >> 8);
        bytes[10] = (byte)(width & 0xFF);
        return bytes;
    }

    private static byte[] WebpVp8x(int width, int height)
    {
        var bytes = new byte[30];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
        Encoding.ASCII.GetBytes("WEBP").CopyTo(bytes, 8);
        Encoding.ASCII.GetBytes("VP8X").CopyTo(bytes, 12);
        var w = width - 1;
        var h = height - 1;
        bytes[24] = (byte)(w & 0xFF);
        bytes[25] = (byte)((w >> 8) & 0xFF);
        bytes[26] = (byte)((w >> 16) & 0xFF);
        bytes[27] = (byte)(h & 0xFF);
        bytes[28] = (byte)((h >> 8) & 0xFF);
        bytes[29] = (byte)((h >> 16) & 0xFF);
        return bytes;
    }

    private static void WriteUInt32BigEndian(byte[] target, int offset, int value)
    {
        target[offset] = (byte)((value >> 24) & 0xFF);
        target[offset + 1] = (byte)((value >> 16) & 0xFF);
        target[offset + 2] = (byte)((value >> 8) & 0xFF);
        target[offset + 3] = (byte)(value & 0xFF);
    }

    [Fact]
    public void Accepts_a_valid_png_and_reports_its_dimensions()
    {
        var result = TenantAppBrandingLogo.Validate("image/png", Png(64, 48));

        Assert.True(result.IsSuccess);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Equal(64, result.Value.Width);
        Assert.Equal(48, result.Value.Height);
    }

    [Fact]
    public void Accepts_a_valid_jpeg()
    {
        var result = TenantAppBrandingLogo.Validate("image/jpeg", Jpeg(320, 200));

        Assert.True(result.IsSuccess);
        Assert.Equal("image/jpeg", result.Value.ContentType);
        Assert.Equal(320, result.Value.Width);
        Assert.Equal(200, result.Value.Height);
    }

    [Fact]
    public void Accepts_jpg_alias_content_type()
    {
        var result = TenantAppBrandingLogo.Validate("image/jpg", Jpeg(320, 200));

        Assert.True(result.IsSuccess);
        Assert.Equal("image/jpeg", result.Value.ContentType);
    }

    [Fact]
    public void Accepts_a_valid_webp()
    {
        var result = TenantAppBrandingLogo.Validate("image/webp", WebpVp8x(512, 256));

        Assert.True(result.IsSuccess);
        Assert.Equal("image/webp", result.Value.ContentType);
        Assert.Equal(512, result.Value.Width);
        Assert.Equal(256, result.Value.Height);
    }

    [Fact]
    public void Rejects_empty_upload()
    {
        var result = TenantAppBrandingLogo.Validate("image/png", []);

        Assert.True(result.IsFailure);
        Assert.Equal("app_branding_logo.empty", result.Error.Code);
    }

    [Fact]
    public void Rejects_oversized_upload()
    {
        var big = Png(64, 48, TenantAppBrandingLogo.MaxBytes + 1);

        var result = TenantAppBrandingLogo.Validate("image/png", big);

        Assert.True(result.IsFailure);
        Assert.Equal("app_branding_logo.too_large", result.Error.Code);
    }

    [Theory]
    [InlineData("image/svg+xml")]
    [InlineData("image/gif")]
    [InlineData("text/html")]
    public void Rejects_disallowed_declared_type(string contentType)
    {
        var result = TenantAppBrandingLogo.Validate(contentType, Png(64, 48));

        Assert.True(result.IsFailure);
        Assert.Equal("app_branding_logo.unsupported_type", result.Error.Code);
    }

    [Fact]
    public void Rejects_html_masquerading_as_png()
    {
        var html = Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>");

        var result = TenantAppBrandingLogo.Validate("image/png", html);

        Assert.True(result.IsFailure);
        Assert.Equal("app_branding_logo.unsupported_type", result.Error.Code);
    }

    [Fact]
    public void Rejects_a_real_image_whose_declared_type_is_wrong()
    {
        // Bytes are genuinely JPEG, but the client claims PNG.
        var result = TenantAppBrandingLogo.Validate("image/png", Jpeg(64, 48));

        Assert.True(result.IsFailure);
        Assert.Equal("app_branding_logo.content_mismatch", result.Error.Code);
    }

    [Fact]
    public void Rejects_dimensions_over_the_cap()
    {
        var result = TenantAppBrandingLogo.Validate("image/png", Png(2000, 64));

        Assert.True(result.IsFailure);
        Assert.Equal("app_branding_logo.dimensions_too_large", result.Error.Code);
    }

    [Fact]
    public void Rejects_dimensions_under_the_floor()
    {
        var result = TenantAppBrandingLogo.Validate("image/png", Png(8, 8));

        Assert.True(result.IsFailure);
        Assert.Equal("app_branding_logo.dimensions_too_small", result.Error.Code);
    }
}
