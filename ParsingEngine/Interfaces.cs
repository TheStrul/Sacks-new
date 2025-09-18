namespace ParsingEngine;

public interface IRule
{
    RuleExecutionResult Execute(CellContext ctx);
}

public interface ITransform
{
    TransformResult Apply(TransformResult input);
}

public record TransformResult(string Text, IDictionary<string, string> Captures);
