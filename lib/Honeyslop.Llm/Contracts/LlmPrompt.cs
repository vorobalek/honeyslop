namespace Honeyslop.Llm.Contracts;

public sealed record LlmPrompt(string System,
    string User);