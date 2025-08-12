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

## Foundations (Background Theory)

### What is a Large Language Model (LLM)?

- An LLM predicts the next token (fragment of a word) given previous tokens, using patterns learned from massive text corpora.
- “Intelligence” emerges from statistical associations; the model does not “know” facts— it reproduces likely continuations.
- Chat = Repeated next-token prediction over a structured message history (system, user, assistant roles). Structure steers behavior.

### Why do we need abstraction layers?

- Raw HTTP calls are verbose and provider-specific (different auth headers, JSON shapes, features).
- MEAI (Microsoft.Extensions.AI) creates stable interfaces (`IChatClient`, `IEmbeddingGenerator`) so you can swap providers (Ollama local, Azure OpenAI cloud, GitHub Models) with minimal code change.
- Semantic Kernel (SK) sits above MEAI (or native SDKs) to add orchestration: memory, function/tool calling, planning, multi-step reasoning, plugin discovery, composition.

### MEAI vs SK (Conceptual Architecture)

Layered mental model (top -> bottom):

1. Your Application (CLI, web, game)
2. Orchestration / Agents (Semantic Kernel: plugins, planning, memories)
3. AI Abstractions (MEAI interfaces: chat, embeddings, safety, streaming)
4. Provider Adapters (Ollama, Azure OpenAI, OpenAI, GitHub Models, local runners)
5. Model Runtime (LLM engine executing token generation)

Use only MEAI when you just need: “Send messages; get responses.” Add SK when you need structured tool invocation, chaining, or reusable skills across scenarios.

### Why Function / Tool Calling Exists

- LLMs hallucinate when asked for dynamic or precise data ("temperature in Paris now").
- Tool calling lets the model produce a structured request (function name + arguments) instead of guessing the answer.
- The host (SK) executes verified code (API, DB query) and returns authoritative data which the model then weaves into a final natural-language answer.

### How Descriptions Influence Invocation

- Each exposed function becomes part of a “tool schema” added to the model prompt.
- Clear, action-oriented descriptions raise precision (“Returns current temperature for a city (Celsius)” is better than “Get data”).
- Small parameter sets with explicit units/types reduce argument errors.

### Typical Tool Call Loop

1. User asks a question needing external data.
2. Model evaluates available tool descriptions.
3. Model emits a tool call (name + JSON args) rather than a final answer.
4. Orchestrator executes function, captures result.
5. (Optional) Model receives tool result as context and produces final answer.

### Designing Good Plugin Functions

- Single responsibility: one focused capability per function.
- Deterministic: avoid random output (or clearly label randomness) to help predictable answers.
- Fast: long I/O stalls user experience; consider timeouts + retries.
- Return compact, structured info (JSON) if the model must perform follow-up reasoning; return natural language if final.
- Provide error handling: return a concise error message instead of throwing (models handle text gracefully).

### Local vs Remote Models (Ollama vs Cloud)

- Local (Ollama):
  - Pros: Privacy, offline use, no per-token billing, rapid experimentation.
  - Cons: Hardware dependent, may lag behind state-of-the-art performance.
- Cloud (e.g., Azure OpenAI):
  - Pros: Latest models, scaling, integrated compliance/safety.
  - Cons: Cost, network latency, data governance considerations.
- Abstractions let you prototype locally then upscale to cloud with minimal code edits.

### Model Tags (e.g., `llama3.2:1b`)

- Format often includes family + variant/size (here 1b = ~1 billion parameters; smaller = faster, less capable).
- Choosing size is a latency vs quality trade-off; start small for dev feedback loops, move larger for production quality.

### Maintaining Conversation State

- Stateless APIs: You resend prior messages each call (you own conversation memory).
- SK: Can centrally manage or augment memory (short-term: message history; long-term: embeddings + recall).
- Prune history (drop earliest turns or summarize) to stay within token limits.

### Preventing Hallucinations (Quick Strategies)

- Retrieval Augmented Generation (RAG): Provide relevant documents in the prompt.
- Tool calling: Fetch real data instead of letting the model invent.
- Constrained outputs: Ask for JSON with required fields.
- Validation: Post-check critical fields (dates, currency) before trusting output.

### When to Move from MEAI Only to SK

Add SK if you need any of:

- Multiple coordinated tools (e.g., weather + news + translation).
- Dynamic tool selection / planning.
- Memory beyond raw message replay.
- Reusable “skills” shared across applications.

### Common Pitfalls for Beginners

- Overly vague function descriptions -> irrelevant tool calls.
- Returning verbose unstructured text -> harder follow-up reasoning.
- Forgetting to include prior messages -> model “forgets” context.
- Mixing units (C vs F) without clarifying -> inconsistent answers.

### Minimal Example: Structured Return

```csharp
[KernelFunction, Description("Returns current temperature in Celsius for a city.")]
public async Task<object> GetCityTemperature(Kernel kernel, string city)
{
    var tempC = await FetchAsync(city); // domain logic
    return new { city, tempC, unit = "C" }; // structured JSON-like object
}
```

Structured objects help if you later chain another tool (e.g., convert units, decide clothing advice).

### Mental Model Recap

- LLM = pattern engine.
- Abstractions (MEAI/SK) = glue so you spend time on capability, not plumbing.
- Plugins = safely extend grounding in real world data/actions.
- Descriptions = prompt-time API doc for the model.
- Good design = clear contracts, deterministic outputs, fast responses.


## SKFunctions01

### CityTemperaturePlugIn concept

- Defined in `CityTemperaturePlugIn.cs` with a method `GetCityTemperature` decorated by `[KernelFunction]` and `[Description]`.
- Adding via `builder.Plugins.AddFromType<CityTemperaturePlugIn>();` registers the method as an invokable tool/function inside the Semantic Kernel (SK) Kernel.
- During chat, `ToolCallBehavior.AutoInvokeKernelFunctions` lets the model decide (via function-calling semantics) to invoke registered functions; SK parses tool calls and executes them automatically, replacing them with the function result in the model’s final response stream.
- The method signature includes `Kernel kernel` (optional injection for advanced use) and a `city` parameter annotated for better model guidance.
- Current implementation simulates a temperature (random value) and logs start/end for transparency.

### Function-calling flow (simplified)

1. User asks something (e.g., "What’s the temperature in Paris?").
2. Model output contains a tool/function call JSON referencing `GetCityTemperature` with arguments.
3. SK intercepts, calls the C# method, captures its return string.
4. SK feeds the tool result back to the model (if needed) or streams it to the user as part of the assistant reply.

### Purpose of plugins

- Encapsulate capabilities (data lookup, calculations, side-effects) the LLM alone cannot perform reliably.
- Provide a typed contract (method name + parameters + descriptions) that guides the model toward deterministic invocation.
- Keep separation of concerns: orchestration (SK) vs. capability (plugin).

### Can plugins wrap external APIs?

- Yes. Replace the random logic with a real HTTP call (e.g., weather API). Plugins commonly act as API wrappers, database accessors, vector search, file IO, etc.
- Pattern: validate inputs -> call external service (with retry/timeouts) -> map response to concise, model-friendly string/JSON -> return.

### Minimal real API wrapper sketch

```csharp
[KernelFunction, Description("Returns current temperature for a city.")]
public async Task<string> GetCityTemperature(Kernel kernel, string city)
{
  using var http = new HttpClient();
  var resp = await http.GetAsync($"https://api.example.com/weather?city={Uri.EscapeDataString(city)}");
  resp.EnsureSuccessStatusCode();
  var json = await resp.Content.ReadAsStringAsync();
  // Extract temperature (pseudo-code)
  var tempC = ParseTemp(json);
  return $"The current temperature in {city} is {tempC} C";
}
```

### Quick contract summary

- Input: city (string)
- Output: natural-language sentence (could be structured JSON if preferred)
- Errors: should be caught and surfaced as a friendly message (e.g., "Could not retrieve temperature.")

### When to use a plugin vs inline code

- Use a plugin when capability might be re-used across prompts, shared among agents, or swapped out (mocked in tests).
- Inline code is fine for one-off experiments but doesn’t scale or self-describe to the model.

### How SK decides to call a plugin function

- Metadata sent to the model includes:
  - Function name (`GetCityTemperature`)
  - Method-level `[KernelFunction]` marker (exposes it)
  - Method `[Description]` (purpose text)
  - Parameter names and their `[Description]` values
- This becomes a tool/function schema in the model prompt alongside the user message.
- With `ToolCallBehavior.AutoInvokeKernelFunctions`, the model may emit a tool/function call JSON when user intent matches the described capability.
- SK parses that tool call, executes the C# method, and injects the result back into the assistant reply.
- Clear, specific descriptions increase accurate invocation; vague text reduces precision.

