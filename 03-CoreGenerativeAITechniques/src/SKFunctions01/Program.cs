#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0070 

using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System.Text;
using SKFunctions01;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama; // for local model option

// -----------------------------------------------------------------------------
// Dual Mode Model Selection (Local Ollama vs GitHub Models)
// Simplified for testing: no environment variables or user-secrets.
// Toggle the boolean 'useOllama' (true = local Ollama, false = GitHub Models).
// NOTE: Do NOT commit real tokens. Placeholder kept for demonstration only.
// -----------------------------------------------------------------------------

var useOllama = false; // set to false to test GitHub Models path

// Values for local Ollama (Docker containerized)
// const string ollamaModelId = "llama3.2:3b"; // or try "llama3.2:3b" for better function calling
//const string ollamaModelId = "deepseek-r1"; // or try "llama3.2:3b" for better function calling
const string ollamaModelId = "phi4-mini"; // or try "llama3.2:3b" for better function calling
const string ollamaUri = "http://localhost:11434/";

// Values for GitHub Models (OpenAI-compatible endpoint)
const string githubModelId = "gpt-4o-mini"; // example GitHub Models model id

/*
 
const string githubModelId = "Phi-3.5-mini-instruct";        // Microsoft
const string githubModelId = "Meta-Llama-3.1-8B-Instruct";  // Meta
const string githubModelId = "Mistral-7B-Instruct";         // Mistral
const string githubModelId = "Claude-3-Haiku";              // Anthropic
const string githubModelId = "gpt-4o";         
 */

const string githubUri = "https://models.github.ai/inference"; // fixed endpoint
var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN"); // const string githubToken = "<put-github-token-here-if-testing>"; 

//
if (string.IsNullOrEmpty(githubToken))
{
    var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
    githubToken = config["GITHUB_TOKEN"];
}


OpenAIClient? openAiClient = null;
string activeModelId;
string providerLabel;

if (useOllama)
{
    activeModelId = ollamaModelId;
    providerLabel = "Ollama Local";
}
else
{
    if (githubToken.StartsWith("<"))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("WARNING: Placeholder githubToken not replaced. Set 'githubToken' variable or switch back to Ollama.");
        Console.ResetColor();
    }
    openAiClient = new OpenAIClient(new ApiKeyCredential(githubToken), new OpenAIClientOptions { Endpoint = new Uri(githubUri) });
    activeModelId = githubModelId;
    providerLabel = "GitHub Models";
}

// Create a chat completion service builder
var builder = Kernel.CreateBuilder();

// Register city-temperature plugin which method is exposed via KernelFunction + Description
// Purpose: Provide capabilities (data lookup, computation, side effects) beyond the model’s internal knowledge,
// with a typed contract (method + params + descriptions) guiding safe, structured invocation.
builder.Plugins.AddFromType<CityTemperaturePlugIn>();

// Register chosen provider
if (useOllama)
{
    builder.AddOllamaChatCompletion(ollamaModelId, new Uri(ollamaUri));
}
else
{
    if (openAiClient is not null)
    {
        builder.AddOpenAIChatCompletion(githubModelId, openAiClient);
    }
}

// Get the chat completion service
Kernel kernel = builder.Build();
var chat = kernel.GetRequiredService<IChatCompletionService>();

var history = new ChatHistory();
history.AddSystemMessage("You are a useful chatbot. For ANY temperature or weather questions, you MUST use the available tools to get current data - never rely on your training data for weather-temperature information. If you don't know an answer about non-weather topics, say 'I don't know!'. Use emojis if possible.");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Provider: {providerLabel} | Model: {activeModelId}");
    Console.ResetColor();
    Console.Write("Q: ");
    var userQ = Console.ReadLine();
    if (string.IsNullOrEmpty(userQ))
    {
        break;
    }
    history.AddUserMessage(userQ);

    // Get the chat completions
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        // Set the ToolCallBehavior to AutoInvokeKernelFunctions. 
        // * This means the model will automatically call any registered kernel functions as needed.
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        Temperature = 0.1f, // controls randomness in responses: 0.0 = deterministic, 1.0 = very random
        // TopP = 0.8f // controls diversity: 0.0 = no diversity, 1.0 = full diversity. Lower values make the model more focused.
    };

    var sb = new StringBuilder();
    var result = chat.GetStreamingChatMessageContentsAsync(history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);
    Console.Write($"AI [{activeModelId}]: ");
    await foreach (var item in result)
    {   
        sb.Append(item);
        Console.Write(item.Content);
    }
    Console.WriteLine();

    history.AddAssistantMessage(sb.ToString());
}