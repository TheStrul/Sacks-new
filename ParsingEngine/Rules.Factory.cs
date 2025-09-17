namespace ParsingEngine;

public sealed class RuleFactory
{
    private readonly ParserConfig _config;
    public RuleFactory(ParserConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    public IRule Create(RuleConfig rc)
    {
        ArgumentNullException.ThrowIfNull(rc);
        return rc.Type switch
        {
            "MultiCaptureRegex" => new MultiCaptureRegexRule(rc),
            "SplitByDelimiter"  => new SplitByDelimiterRule(rc),
            "Pipeline"          => new PipelineRule(rc, _config),
            "MapValue"          => new MapValueRule(rc),
            _ => throw new NotSupportedException($"Unknown rule type: {rc.Type}")
        };
    }
}
