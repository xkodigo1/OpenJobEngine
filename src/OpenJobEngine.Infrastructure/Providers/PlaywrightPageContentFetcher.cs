using Microsoft.Playwright;

namespace OpenJobEngine.Infrastructure.Providers;

public sealed class PlaywrightPageContentFetcher : IPageContentFetcher
{
    public async Task<string> GetHtmlAsync(string url, CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        return await page.ContentAsync();
    }
}
