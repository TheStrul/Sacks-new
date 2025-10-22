using System.Text.Json;
using System.Text.Encodings.Web;
using ParsingEngine;

namespace SacksApp.Utils
{
    public static class ActionTestRunner
    {
        private static readonly JsonSerializerOptions s_json = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public sealed class Result
        {
            public bool Success { get; init; }
            public Dictionary<string, string> Bag { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public string JsonAction { get; init; } = string.Empty;
        }

        /// <summary>
        /// Executes a single action with the provided inputs and returns the Variables dictionary and JSON action snippet.
        /// </summary>
        public static Result Run(
            string op,
            string inputText,
            string inputKey,
            string outputKey,
            bool assign,
            string? condition,
            Dictionary<string, string>? parameters,
            Dictionary<string, Dictionary<string, string>> lookups,
            string? seedKey = null,
            string? seedValue = null,
            IFormatProvider? culture = null)
        {
            op ??= string.Empty; inputText ??= string.Empty; inputKey ??= "Text"; outputKey ??= "Out";
            parameters ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            lookups ??= new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            var cfg = new ActionConfig
            {
                Op = op,
                Input = inputKey,
                Output = outputKey,
                Assign = assign,
                Condition = string.IsNullOrWhiteSpace(condition) ? null : condition,
                Parameters = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase)
            };

            var action = ActionsFactory.Create(cfg, lookups);

            var pb = new PropertyBag();
            pb.SetVariable(inputKey, inputText);

            if (!string.IsNullOrWhiteSpace(seedKey))
            {
                pb.SetVariable(seedKey!, seedValue ?? string.Empty);
            }

            var ok = action.Execute(new CellContext("A", inputText, System.Globalization.CultureInfo.InvariantCulture, pb));

            // Build JSON output matching config schema casing
            var jsonParams = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in parameters)
            {
                jsonParams[kv.Key] = kv.Value;
            }
            var actionForJson = new
            {
                Op = ToPascal(op),
                Input = inputKey,
                Output = outputKey,
                Assign = assign,
                Condition = string.IsNullOrWhiteSpace(condition) ? null : condition,
                Parameters = jsonParams.Count == 0 ? null : jsonParams
            };
            var json = JsonSerializer.Serialize(actionForJson, s_json);

            return new Result
            {
                Success = ok,
                Bag = pb.Variables,
                JsonAction = json
            };
        }

        private static string ToPascal(string op)
        {
            if (string.IsNullOrWhiteSpace(op)) return string.Empty;
            return op.Length == 1 ? op.ToUpperInvariant() : char.ToUpperInvariant(op[0]) + op[1..].ToLowerInvariant();
        }
    }
}
