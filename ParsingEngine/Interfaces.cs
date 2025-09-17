namespace ParsingEngine;

public interface IRule
{
    string Id { get; }
    int Priority { get; }
    RuleExecutionResult Execute(CellContext ctx);
}

public interface ITransform
{
    TransformResult Apply(TransformResult input);
}

public record TransformResult(string Text, IDictionary<string, string> Captures);
