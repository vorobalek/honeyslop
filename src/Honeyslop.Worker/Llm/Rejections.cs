namespace Honeyslop.Worker.Llm;

public static class Rejections
{
    public const string Refused = "Model refused a path it has to answer: ";
    public const string GivesAway = "Model output gives itself away: ";
    public const string Placeholder = "Model output has a placeholder: ";
    public const string NotJson = "Model output is not valid JSON: ";
    public const string NotObject = "Model output is not a JSON object.";
    public const string NoStatus = "Model output has no integer 'status'.";
    public const string NoBody = "Model output has no string 'body'.";
    public const string CutOff = "Model output was cut at the token limit.";

    private static readonly (string Reason, string Hint)[] Hints =
    [
        (Refused, "This path is served: reply with status 200 and its content."),
        (GivesAway, "Send only what the software itself sends."),
        (Placeholder, "Fill every value with something specific and realistic."),
        (NotJson, "Reply with JSON in exactly the required shape."),
        (NotObject, "Reply with JSON in exactly the required shape."),
        (NoStatus, "Reply with JSON in exactly the required shape."),
        (NoBody, "Reply with JSON in exactly the required shape."),
        (CutOff, "Keep the body well under 1500 characters.")
    ];

    public static string? HintFor(string? error)
    {
        return error is null ? null : Hints.FirstOrDefault(x => error.StartsWith(x.Reason, StringComparison.Ordinal)).Hint;
    }
}