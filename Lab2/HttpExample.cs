using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Globalization;
using System.IO;

namespace My.Functions;

public class HttpExample
{
    private readonly ILogger<HttpExample> _logger;

    public HttpExample(ILogger<HttpExample> logger)
    {
        _logger = logger;
    }

    [Function("HttpExample")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        // Try to get from query string
        string name = req.Query["name"].ToString();
        string email = req.Query["email"].ToString();
        string ageStr = req.Query["age"].ToString();

        // If any are missing, try to get from JSON body
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(ageStr))
        {
            try
            {
                if (req.Body != null)
                {
                    if (req.Body.CanSeek)
                        req.Body.Position = 0;
                    using var reader = new StreamReader(req.Body);
                    var body = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        var json = JsonDocument.Parse(body).RootElement;
                        if (string.IsNullOrEmpty(name) && json.TryGetProperty("name", out var nameProp))
                            name = nameProp.GetString() ?? string.Empty;
                        if (string.IsNullOrEmpty(email) && json.TryGetProperty("email", out var emailProp))
                            email = emailProp.GetString() ?? string.Empty;
                        if (string.IsNullOrEmpty(ageStr) && json.TryGetProperty("age", out var ageProp))
                            ageStr = ageProp.ToString();
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error reading JSON body");
                // Ignore JSON errors, use defaults
            }
        }

        // Apply rules and defaults
        name = string.IsNullOrWhiteSpace(name) ? "Guest" : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Trim());
        email = string.IsNullOrWhiteSpace(email) ? "unknown@example.com" : email.Trim().ToLowerInvariant();

        object ageResult;
        if (int.TryParse(ageStr, out int ageVal))
            ageResult = ageVal;
        else
            ageResult = "not provided";

        var result = new
        {
            name,
            email,
            age = ageResult
        };

        return new OkObjectResult(result);
    }
}