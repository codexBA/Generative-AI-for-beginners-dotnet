# Extended Notes — Core Generative AI Techniques (src)

This file centralizes short, practical notes for all projects under this folder. Entries are concise and focused on usage decisions. We’ll append sections per project as we go.

## BasicChat-03Ollama

### Model tag and container checks

- Use the exact Ollama model tag you pulled (example used here: `llama3.2:1b`).
- MEAI sample:
  - `IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2:1b");`
- Ensure your Ollama container exposes port `11434` to the host.
- Verify the model exists in the container:

```powershell
docker exec -it ollama-local-ai ollama list
```

### SK vs MEAI usage (same Ollama model)

- Semantic Kernel path:
  - `var chat = new OllamaApiClient(uri, modelId).AsChatCompletionService();`
  - Returns `IChatCompletionService` for SK orchestration (tools/function calling, memory, planners, pipelines). SK can manage chat history for you.

- Microsoft.Extensions.AI (MEAI) path:
  - `IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2:1b");`
  - Returns `IChatClient` for lightweight, provider-agnostic chat in plain .NET. Stateless by default, but supports multi-turn when you pass message history.

- Both target the same Ollama model/tag and both can do multi-turn; the difference is orchestration and who manages history.

### What is MEAI?

- MEAI = Microsoft.Extensions.AI — official .NET abstractions for AI.
- Key pieces: `IChatClient`, `IEmbeddingGenerator`, streaming, tool calls, safety, adapters for providers (Ollama, Azure OpenAI, OpenAI, GitHub Models).
- A “MEAI-based app” uses these interfaces directly (without Semantic Kernel) to keep integrations simple and portable.

> We’ll keep appending brief, high-signal notes here as we examine other projects in this folder.
