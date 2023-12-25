using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace Pdf.Processor.Services;

public class PdfConvertor : IPdfConvertor
{
    private readonly string? _browserPath;

    public PdfConvertor(IConfiguration configuration)
    {
        _browserPath = configuration.GetSection("BrowserPath").Value;
    }


    public async Task<byte[]> GetPdf(string content)
    {
        var launchOptions = new LaunchOptions
        {
            Headless = true,
        };
        if (_browserPath != null)
        {
            launchOptions.ExecutablePath = _browserPath;
        }
        else
        {
            await new BrowserFetcher().DownloadAsync();
        }

        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(content);
        return await page.PdfDataAsync();
    }
}