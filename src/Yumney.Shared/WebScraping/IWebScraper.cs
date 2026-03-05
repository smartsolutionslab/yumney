namespace Yumney.Shared.WebScraping;

public interface IWebScraper
{
    Task<string> FetchContentFromAsync(string url, CancellationToken cancellationToken = default);
}
