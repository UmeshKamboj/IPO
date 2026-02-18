// dotnet-script to test KFinTech connectivity
using System.Net.Http;
using System.Text.RegularExpressions;

var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
};

var client = new HttpClient(handler);
client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
client.Timeout = TimeSpan.FromSeconds(30);

var urls = new[] {
    "https://ipostatus.kfintech.com/",
    "https://kprism.kfintech.com/ipostatus/",
    "https://kosmic.kfintech.com/ipostatus/",
    "https://kcas.kfintech.com/ipostatus/",
    "https://evault.kfintech.com/ipostatus/"
};

foreach (var url in urls)
{
    try
    {
        Console.WriteLine($"\n=== {url} ===");
        var response = await client.GetAsync(url);
        Console.WriteLine($"Status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Length: {content.Length}");
        Console.WriteLine(content.Substring(0, Math.Min(1500, content.Length)));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
}
