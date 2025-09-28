namespace ParsingEngine;

/// <summary>
/// SplitAction: splits a source value by a delimiter and writes parts into a target collection
/// as Target[0], Target[1], ... and sets Target.Length and Target.Valid flags.
/// </summary>
public sealed class SplitAction : BaseAction
{
    public override string Op => "split";
    private readonly string _delimiter;

    public SplitAction(string fromKey, string toKey, string delimiter):
        base(fromKey, toKey)
    {
        _delimiter = delimiter;
    }

    public override bool Execute(IDictionary<string, string> bag, CellContext ctx)
    {
        if (base.Execute(bag, ctx) == false) return false; // already processed
        bag.TryGetValue(base.input, out var value);
        var inputVal = value ?? string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            ActionHelpers.WriteListOutput(bag, input, input, null, false, false);
            return false;
        }

        var parts = input.Split(new[] { _delimiter }, StringSplitOptions.None)
                         .Select(p => p.Trim())
                         .ToArray();

        ActionHelpers.WriteListOutput(bag, input, input, parts, false, false);
        return true;
    }
}
