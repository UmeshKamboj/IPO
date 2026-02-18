using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

// Step 1: Login to ipoutility.in and explore their KFinTech integration
var cookieContainer = new CookieContainer();
var handler = new HttpClientHandler
{
    CookieContainer = cookieContainer,
    AllowAutoRedirect = true,
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
};

var client = new HttpClient(handler);
client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

// Step 1: GET the login page to extract CSRF token
Console.WriteLine("=== Step 1: GET Login Page ===");
var loginPageResp = await client.GetAsync("https://ipoutility.in/login");
var loginPage = await loginPageResp.Content.ReadAsStringAsync();
Console.WriteLine($"Status: {loginPageResp.StatusCode}, Length: {loginPage.Length}");

// Extract CSRF token
var csrfMatch = Regex.Match(loginPage, @"name=""_token""\s+value=""([^""]+)""");
var csrfMatch2 = Regex.Match(loginPage, @"value=""([^""]+)""\s+name=""_token""");
var csrfMatch3 = Regex.Match(loginPage, @"csrf[_-]?token[^""]*""[^""]*""([^""]+)""", RegexOptions.IgnoreCase);
var metaCsrf = Regex.Match(loginPage, @"<meta\s+name=""csrf-token""\s+content=""([^""]+)""");

Console.WriteLine($"CSRF _token: {csrfMatch.Success} => {(csrfMatch.Success ? csrfMatch.Groups[1].Value[..Math.Min(30, csrfMatch.Groups[1].Value.Length)] : "N/A")}");
Console.WriteLine($"CSRF _token (alt): {csrfMatch2.Success}");
Console.WriteLine($"CSRF generic: {csrfMatch3.Success}");
Console.WriteLine($"CSRF meta tag: {metaCsrf.Success} => {(metaCsrf.Success ? metaCsrf.Groups[1].Value[..Math.Min(30, metaCsrf.Groups[1].Value.Length)] : "N/A")}");

// Show all hidden inputs
var hiddenInputs = Regex.Matches(loginPage, @"<input[^>]*type=""hidden""[^>]*>", RegexOptions.IgnoreCase);
Console.WriteLine($"\nHidden inputs: {hiddenInputs.Count}");
foreach (Match m in hiddenInputs)
    Console.WriteLine($"  {m.Value}");

// Show form action
var formAction = Regex.Match(loginPage, @"<form[^>]*action=""([^""]*login[^""]*|[^""]+)""[^>]*>", RegexOptions.IgnoreCase);
Console.WriteLine($"\nForm action: {(formAction.Success ? formAction.Groups[1].Value : "N/A")}");

// Show form fields
var inputFields = Regex.Matches(loginPage, @"<input[^>]*name=""([^""]+)""[^>]*>", RegexOptions.IgnoreCase);
Console.WriteLine($"\nAll input fields:");
foreach (Match m in inputFields)
    Console.WriteLine($"  name={m.Groups[1].Value} => {m.Value[..Math.Min(100, m.Value.Length)]}");

// Extract CSRF token value
string csrfToken = "";
if (csrfMatch.Success) csrfToken = csrfMatch.Groups[1].Value;
else if (csrfMatch2.Success) csrfToken = csrfMatch2.Groups[1].Value;
else if (metaCsrf.Success) csrfToken = metaCsrf.Groups[1].Value;

Console.WriteLine($"\nUsing CSRF token: {csrfToken[..Math.Min(40, csrfToken.Length)]}...");

// Show cookies received
Console.WriteLine($"\nCookies received:");
foreach (Cookie c in cookieContainer.GetCookies(new Uri("https://ipoutility.in")))
    Console.WriteLine($"  {c.Name} = {c.Value[..Math.Min(30, c.Value.Length)]}...");

// Step 2: POST login
Console.WriteLine("\n=== Step 2: POST Login ===");
var loginData = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("_token", csrfToken),
    new KeyValuePair<string, string>("email", "Panthil"),
    new KeyValuePair<string, string>("password", "Pan@1234"),
});

client.DefaultRequestHeaders.Add("Referer", "https://ipoutility.in/login");
var loginResp = await client.PostAsync("https://ipoutility.in/login", loginData);
Console.WriteLine($"Status: {(int)loginResp.StatusCode} {loginResp.StatusCode}");
Console.WriteLine($"Location: {loginResp.Headers.Location}");

var loginResult = await loginResp.Content.ReadAsStringAsync();
Console.WriteLine($"Response length: {loginResult.Length}");

// Check if redirected to dashboard
if (loginResult.Contains("dashboard", StringComparison.OrdinalIgnoreCase) ||
    loginResult.Contains("allotment", StringComparison.OrdinalIgnoreCase) ||
    loginResult.Contains("welcome", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(">>> LOGIN SUCCESS - Redirected to dashboard");
}
else if (loginResult.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
         loginResult.Contains("error", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(">>> LOGIN FAILED");
    // Show error message
    var errorMatch = Regex.Match(loginResult, @"class=""[^""]*(?:error|alert|danger)[^""]*""[^>]*>(.*?)</", RegexOptions.IgnoreCase);
    if (errorMatch.Success)
        Console.WriteLine($"Error: {errorMatch.Groups[1].Value}");
}

// Step 3: Explore the site for KFinTech-related pages
Console.WriteLine("\n=== Step 3: Exploring site ===");

// Show all navigation links
var navLinks = Regex.Matches(loginResult, @"href=""([^""]*(?:allot|ipo|kfin|registrar|status|check|company)[^""]*)""", RegexOptions.IgnoreCase);
Console.WriteLine($"Relevant navigation links: {navLinks.Count}");
foreach (Match m in navLinks.Take(20))
    Console.WriteLine($"  {m.Groups[1].Value}");

// Also check all links
var allLinks = Regex.Matches(loginResult, @"href=""(https?://ipoutility\.in/[^""]+)""", RegexOptions.IgnoreCase);
Console.WriteLine($"\nAll site links: {allLinks.Count}");
foreach (Match m in allLinks.Take(30))
    Console.WriteLine($"  {m.Groups[1].Value}");

// Try common IPO/allotment pages
var pages = new[] {
    "https://ipoutility.in/dashboard",
    "https://ipoutility.in/allotment",
    "https://ipoutility.in/ipo",
    "https://ipoutility.in/ipo/allotment",
    "https://ipoutility.in/ipo-allotment",
    "https://ipoutility.in/check-allotment",
    "https://ipoutility.in/kfintech",
};

foreach (var page in pages)
{
    try
    {
        var resp = await client.GetAsync(page);
        if (resp.StatusCode == HttpStatusCode.OK)
        {
            var content = await resp.Content.ReadAsStringAsync();
            if (content.Contains("kfin", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("allot", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("registrar", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"\n>>> FOUND: {page} (Status: {resp.StatusCode}, Length: {content.Length})");

                // Extract API endpoints from JavaScript
                var apiMatches = Regex.Matches(content, @"(?:fetch|axios|ajax|url|href)\s*[\(""':]\s*[""']([^""']*(?:allot|ipo|kfin|registrar|status|check|company)[^""']*)[""']", RegexOptions.IgnoreCase);
                Console.WriteLine($"  API endpoints found: {apiMatches.Count}");
                foreach (Match m in apiMatches.Take(20))
                    Console.WriteLine($"    {m.Groups[1].Value}");

                // Look for AJAX/fetch URLs
                var ajaxUrls = Regex.Matches(content, @"[""']((?:/|https?://)[^""']*(?:api|allot|ipo|kfin|fetch|check)[^""']*)[""']", RegexOptions.IgnoreCase);
                Console.WriteLine($"  AJAX URLs: {ajaxUrls.Count}");
                foreach (Match m in ajaxUrls.Take(20))
                    Console.WriteLine($"    {m.Groups[1].Value}");
            }
            else
            {
                Console.WriteLine($"  {page} => {resp.StatusCode} (no relevant content)");
            }
        }
        else
        {
            Console.WriteLine($"  {page} => {(int)resp.StatusCode} {resp.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {page} => Error: {ex.Message}");
    }
}

Console.WriteLine("\n=== First 3000 chars of dashboard/response ===");
Console.WriteLine(loginResult[..Math.Min(3000, loginResult.Length)]);
