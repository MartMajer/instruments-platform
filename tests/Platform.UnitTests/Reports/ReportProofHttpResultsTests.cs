using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Features.Reports;
using Platform.SharedKernel;

namespace Platform.UnitTests.Reports;

public sealed class ReportProofHttpResultsTests
{
    [Fact]
    public async Task ToFile_writes_binary_content_bytes_without_utf8_conversion()
    {
        byte[] contentBytes = [0x00, 0x01, 0xFE, 0xFF];
        var result = ReportProofHttpResults.ToFile(Result.Success(new ExportArtifactDownloadResponse(
            Guid.NewGuid(),
            "artifact.pdf",
            "application/pdf",
            contentBytes.Length,
            "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
            Content: string.Empty,
            ContentBytes: contentBytes)));
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await result.ExecuteAsync(context);

        Assert.Equal(contentBytes, body.ToArray());
        Assert.Equal("application/pdf", context.Response.ContentType);
    }
}
