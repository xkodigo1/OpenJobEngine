namespace OpenJobEngine.Infrastructure.Providers;

public interface IPageContentFetcher
{
    Task<string> GetHtmlAsync(string url, CancellationToken cancellationToken);
}
