using OpenJobEngine.Application.Abstractions.Services;
using UglyToad.PdfPig;

namespace OpenJobEngine.Infrastructure.Resume;

public sealed class PdfPigResumeTextExtractor : IResumeTextExtractor
{
    public Task<string> ExtractTextAsync(byte[] content, string fileName, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var document = PdfDocument.Open(stream);

        var pages = document.GetPages()
            .Select(x => x.Text)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return Task.FromResult(string.Join(Environment.NewLine, pages));
    }
}
