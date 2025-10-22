namespace ParsingEngine;

using System.IO;

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

    public override bool Execute(CellContext ctx)
    {
        if (base.Execute(ctx) == false) return false;
        
        if (ctx.PropertyBag.Variables.TryGetValue(input, out var value))
        {
            var parts = value.Split(new[] { _delimiter }, StringSplitOptions.None)
                             .Select(p => p.Trim())
                             .ToArray();

            ActionHelpers.WriteListOutput(ctx.PropertyBag, output, value, parts, false, false);
            return true;
        }
        else
        {
            if (string.IsNullOrEmpty(input))
            {
                ActionHelpers.WriteListOutput(ctx.PropertyBag, input, input, null, false, false);
                return false;
            }
        }
        return false;
    }
}
