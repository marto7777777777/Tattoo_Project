using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tattoo_Project.AI.Planning;

public sealed class AiTattooPlanner(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment environment,
    ILogger<AiTattooPlanner> logger) : IAiTattooPlanner
{
    private string InstructionsPath => Path.Combine(
        environment.ContentRootPath,
        "AI",
        "Planning",
        "TattooPlannerInstructions.txt");

    public Task<string> CreateGenerationPromptAsync(
        string planningContext,
        CancellationToken cancellationToken = default)
    {
        return CreatePromptAsync(
            "Create the final concise prompt for a NEW tattoo image.",
            planningContext,
            cancellationToken);
    }

    public Task<string> CreateEditPromptAsync(
        string planningContext,
        CancellationToken cancellationToken = default)
    {
        return CreatePromptAsync(
            "Create the final concise prompt for EDITING the supplied current tattoo image. Preserve approved content and describe only the controlled changes required.",
            planningContext,
            cancellationToken);
    }

    private async Task<string> CreatePromptAsync(
    string task,
    string planningContext,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(planningContext))
            throw new ArgumentException(
                "Planning context is required.",
                nameof(planningContext));

        var apiKey = configuration["OpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) ||
            apiKey.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "OpenAI is not configured for the tattoo planner.");
        }

        if (!File.Exists(InstructionsPath))
        {
            throw new FileNotFoundException(
                "Tattoo planner instructions were not found.",
                InstructionsPath);
        }

        var instructions = await File.ReadAllTextAsync(
            InstructionsPath,
            cancellationToken);

        var model = configuration["OpenAI:TextModel"];

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                "OpenAI:TextModel is not configured.");
        }

        var payload = new
        {
            model,
            instructions,
            input =
                $"{task}\n\n" +
                $"COMPLETE TATTOO PLANNING CONTEXT:\n{planningContext}",
            max_output_tokens =
                configuration.GetValue<int?>(
                    "OpenAI:PlannerMaxOutputTokens") ?? 1400
        };

        // LOG 1: показва кой модел се използва и колко е дълъг контекстът.
        // Не логваме API ключа.
        logger.LogInformation(
            "Tattoo planner request started. " +
            "Model: {Model}. " +
            "Task: {Task}. " +
            "Context length: {ContextLength} characters.",
            model,
            task,
            planningContext.Length);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/responses");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                apiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var client = httpClientFactory.CreateClient();

        using var response = await client.SendAsync(
            request,
            cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Tattoo planner request failed. " +
                "Model: {Model}. " +
                "Status: {StatusCode}. " +
                "Response: {ResponseBody}",
                model,
                response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"Tattoo planner request failed: {responseBody}");
        }

        var prompt = ExtractText(responseBody);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException(
                "The tattoo planner returned an empty image prompt.");
        }

        prompt = prompt.Trim();

        // LOG 2: показва крайния prompt, който ще отиде към image модела.
        logger.LogInformation(
            "Tattoo planner completed. " +
            "Model: {Model}. " +
            "Final prompt length: {PromptLength} characters." +
            "{NewLine}========== FINAL IMAGE PROMPT ==========" +
            "{NewLine}{Prompt}" +
            "{NewLine}========================================",
            model,
            prompt.Length,
            Environment.NewLine,
            prompt);

        return prompt;
    }

    private static string ExtractText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString() ?? string.Empty;
        }

        if (!root.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) &&
                    text.ValueKind == JsonValueKind.String)
                {
                    var value = text.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        parts.Add(value);
                }
            }
        }

        return string.Join(Environment.NewLine, parts);
    }
}
