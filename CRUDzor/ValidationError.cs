namespace CRUDzor;

public readonly struct ValidationError(string property, IEnumerable<string> messages)
{
    public string Property { get; init; } = property;

    public IEnumerable<string> Messages { get; init; } = messages;
}
