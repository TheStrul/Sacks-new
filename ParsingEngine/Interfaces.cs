namespace ParsingEngine;

public interface IRule
{
    RuleExecutionResult Execute(CellContext ctx);
}

