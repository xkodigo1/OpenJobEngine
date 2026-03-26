namespace OpenJobEngine.Application.Abstractions.Services;

public interface IResumeTextExtractor
{
    Task<string> ExtractTextAsync(byte[] content, string fileName, CancellationToken cancellationToken);
}
