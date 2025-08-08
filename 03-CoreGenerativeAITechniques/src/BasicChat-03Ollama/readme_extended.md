# Extended Notes — BasicChat-03Ollama

This file aggregates short, practical notes as we review this sample. Entries are concise and focused on decisions and usage.

## SK vs MEAI usage (same Ollama model)

- Semantic Kernel path:
  - `var chat = new OllamaApiClient(uri, modelId).AsChatCompletionService();`
  - Returns `IChatCompletionService` for SK orchestration (tools/function calling, memory, planners, pipelines). SK can manage chat history for you.
- Microsoft.Extensions.AI (MEAI) path:
  - `IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2:1b");`
  - Returns `IChatClient` for lightweight, provider-agnostic chat in plain .NET. Stateless by default, but supports multi-turn when you pass message history.
- Both target the same Ollama model/tag and both can do multi-turn; the difference is orchestration and who manages history.

## What is MEAI?

- MEAI = Microsoft.Extensions.AI — official .NET abstractions for AI.
- Key pieces: `IChatClient`, `IEmbeddingGenerator`, streaming, tool calls, safety, adapters for providers (Ollama, Azure OpenAI, OpenAI, GitHub Models).
- A “MEAI-based app” uses these interfaces directly (without Semantic Kernel) to keep integrations simple and portable.

> Note: We’ll continue appending brief, high-signal notes here as we examine other projects in this folder.
