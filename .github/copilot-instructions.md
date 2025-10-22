# Copilot Instructions (Repo Defaults)


You are assisting on a multi-project .NET solution containing:
- ParsingEngine (C# 13, may target .NET 9)
- SacksDataLayer (EF Core + SQL Server; targets .NET 8/9 per csproj)
- SacksLogicLayer
- SacksApp (WinForms)
- Sacks.Tests

Act as a senior C#/.NET reviewer & implementer. Keep edits minimal and incremental.

## Global rules
- Prefer unified diffs over prose. Keep answers concise (<=150 lines). Use `READY FOR CONTINUE` for long outputs.
- Default target: .NET 8. If a project explicitly pins .NET 9, follow it. C# 13 features are allowed where the project supports them.
- Enable nullable reference types and analyzers; treat warnings as errors when proposing `.csproj` changes.
- I/O must be async, pass through `CancellationToken`, and use `ConfigureAwait(false)` inside libraries.
- Security: parameterized SQL only; no secrets in code; never compose raw SQL with string concatenation.
- Time/Money: use `DateTime.UtcNow` (or `Instant` if NodaTime), `decimal` for money with explicit precision/scale.
- Logging: use structured `ILogger<T>`; avoid string concatenation in hot paths.
- NO BACKWARD COMPATIBILITY IS EVER NEEDED. Prefer breaking-change simplifications over shims.

## EF Core rules (SacksDataLayer)
- Do not leak `DbSet<T>`/`IQueryable<T>` outside the data layer; prefer DTOs/projections.
- Use `AsNoTracking()` for read-only queries; `AsSplitQuery()` for wide graphs.
- Prevent N+1 queries; batch where possible.
- Pagination for large queries; project only required columns.
- Transactions: a single transaction for multi-entity writes; avoid `SaveChanges` in loops; add concurrency tokens if needed.

## Configuration domain rules (SuppliersConfiguration / ParserConfig)
Reflect current code semantics in `SacksDataLayer.Models.SupplierConfigurationModels` and `ParsingEngine`:
- Lookups are nested dictionaries that MUST be case-insensitive (both outer and inner). Preserve/convert to `StringComparer.OrdinalIgnoreCase` when creating or merging.
- Update configuration in-place to preserve object identity and existing references:
  - `SuppliersConfiguration.ApplyFrom`: update version/path; in-place-merge top-level `Lookups`; upsert `Suppliers` by `Name` (case-insensitive); set `ParentConfiguration`; call `ParserConfig.DoMergeLoookUpTables` for new/updated items; remove suppliers not present in source.
  - `SupplierConfiguration.UpdateFrom`: keep `Name` as identity; copy simple props; `FileStructure.ApplyFrom`; `SubtitleRowHandlingConfiguration.ApplyFrom`; `ParserConfig.ApplyFrom` then re-merge parser lookups.
  - `FileStructureConfiguration.ApplyFrom` and `DetectionConfiguration.ApplyFrom`: copy scalars and replace list contents without swapping instances.
  - `ParserConfigExtensions.ApplyFrom`: copy `Settings`; in-place-merge `Lookups`; upsert `ColumnRules` by key, removing missing; deep-clone/replace `RuleConfig.Actions`.
- Always call `ParserConfig.DoMergeLoookUpTables` after updating Lookups so parser sees merged tables.
- Validation rules (keep consistent with `ValidateConfiguration`):
  - Require non-empty `Version` and at least one supplier.
  - `FileStructure`: `DataStartRowIndex|HeaderRowIndex|ExpectedColumnCount` must be >= 1; if `Detection` exists, `FileNamePatterns` must be non-empty.
  - `SubtitleRowHandlingConfiguration`: `Action` in {`parse`,`skip`}; detection rules support `columnCount|pattern|hybrid`; transforms validate regex when `Mode == regexReplace` and require `Pattern`.
  - `ActionConfig` ops and parameters:
    - `assign`: no parameters allowed.
    - `find`: requires `Pattern` (regex or `Lookup:TableName`); only `Pattern|Options|PatternKey` allowed.
    - `split|splitByDelimiter`: only `Delimiter|ExpectedParts|Strict` allowed.
    - `map|mapping`: requires existing `Table` name in merged lookups; only `Table|CaseMode|AddIfNotFound` allowed.

## Serialization
- Prefer `System.Text.Json`. For supplier export, match current behavior: `UnsafeRelaxedJsonEscaping` and `WriteIndented=true`.
- Avoid Newtonsoft.Json unless explicitly required.

## WinForms (SacksApp)
- Keep UI thread safety. Offload I/O and CPU-bound work to background tasks with proper cancellation.
- Do not block the UI thread; use `await` with progress reporting.

## API design
- New public APIs that perform I/O must accept `CancellationToken` (defaulted where appropriate).
- Ensure null-safety input validation (CA1062), disposal correctness (CA2000), and token forwarding (CA2016).

## Output policy
- For “fix” requests: output diffs only unless asked for explanation.
- For “plan/assessment”: return a tight bullet list with file:line references.

## PR checklist (apply before finishing changes)
- Builds succeed with warnings-as-errors in modified projects.
- Nullability: avoid `!` suppression; prefer proper checks.
- Async: pass cancellation tokens end-to-end and use `ConfigureAwait(false)` in libraries.
- Dictionaries intended for case-insensitive keys use `StringComparer.OrdinalIgnoreCase`.
- Configuration merges update in-place and do not break existing references.
- Logging is structured and parameterized; no PII in logs.

- Always address user as "Struly-Dear".

## Parser Configuration Best Practices (`supplier-PCA.json` example)
When parsing a complex, unstructured field like a product description, follow the "waterfall" or "chain-of-responsibility" pattern demonstrated in `supplier-PCA.json` under column `G`. This approach creates a robust, multi-stage parsing pipeline.

The key principles are:
1.  **Sequential Extraction & Cleaning**: Start with the full text. In each step, find a specific piece of information (e.g., Brand, Size, Gender), extract it, and *remove it* from the text.
2.  **Use Intermediate Variables**: Store the progressively cleaned text in intermediate variables (e.g., `TextNoBrand`, `SizesAndUnits.Clean`). The `Find` operation's `remove` option automatically creates a `.Clean` property on the output variable.
3.  **Process of Elimination**: Handle the most specific or easily identifiable tokens first. For example, remove the brand name before trying to identify more generic types or the product name itself.
4.  **Final Assignment**: After all known attributes have been extracted, the remaining text is often the core product name. Assign this cleaned-up remainder to `Product.Name`.
5.  **Example Flow from `supplier-PCA.json`**:
    - Start with `Offer.Description`.
    - Remove `Brand` -> `TextNoBrand`.
    - From `TextNoBrand.Clean`, remove `Type 2`.
    - From `Product.Type 2.Clean`, remove `Gender`.
    - From `Product.Gender.Clean`, remove sizes/units (`SizesAndUnits`).
    - ...and so on, for `Concentration` and `Type 1`.
    - The final cleaned string is assigned to `Product.Name`.
