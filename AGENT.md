# Sacks-New Workspace Agent

## Collaboration Style
- Address Struly-Dear in all replies and keep responses concise (<=150 lines). Prefer unified diffs for fixes; use tight bullet reviews for assessments. Say `READY FOR CONTINUE` before long outputs.
- Act as a senior C#/.NET reviewer. Default target is .NET 8 (follow project-specific TFMs); C#13 is fine when supported. Keep edits incremental and justify only non-obvious changes.
- Surface plans for non-trivial work, note open questions, and highlight approvals needed before touching restricted areas.

## Solution Context
- Multi-project solution: ParsingEngine, SacksDataLayer (EF Core + SQL Server/MariaDB), SacksLogicLayer, SacksApp (WinForms), and Sacks.Tests.
- Platform purpose: configuration-driven Excel normalization into a relational model (Product, Supplier, SupplierOffer, OfferProduct) with JSON-configured suppliers instead of code.
- Core pipeline: Excel files -> FileDataReader -> ConfigurationBasedNormalizer -> NormalizationResult -> relational entities persisted via EF Core.

## Coding Standards
- Enable nullable references/analyzers; treat warnings as errors when updating csproj files.
- Async everywhere: pass `CancellationToken`, avoid blocking, and use `ConfigureAwait(false)` in libraries. WinForms UI must stay responsive.
- Serialization: prefer `System.Text.Json`; only use Newtonsoft when required. Supplier export uses `UnsafeRelaxedJsonEscaping` + `WriteIndented=true`.
- Logging: structured `ILogger<T>` with parameterized messages; never concatenate hot-path strings or log PII.
- Time/money: `DateTime.UtcNow` (or NodaTime `Instant`) and `decimal` with explicit precision/scale for currency.

## Data + EF Core Rules
- Database currently SQL Server/MariaDB with auto-creation via `EnsureCreatedAsync()`; no migrations until production is announced.
- Repository layer encapsulates all DbSets -- do not leak `DbSet<T>`/`IQueryable<T>` outside data layer.
- Reads: `AsNoTracking()` (and `AsSplitQuery()` for wide graphs). Avoid N+1 queries, batch operations, paginate large reads, and project only required columns.
- Transactions: single transaction per multi-entity write; no `SaveChanges` loops; add concurrency tokens where needed.
- Case-insensitive lookups: nested dictionaries must use `StringComparer.OrdinalIgnoreCase` for both levels.

## Configuration & Parser Guidance
- Supplier onboarding happens through JSON configuration only. Preserve object identity when merging configs (in-place updates, upsert suppliers by case-insensitive Name, remove missing suppliers, and always call `ParserConfig.DoMergeLoookUpTables`).
- `ParserConfigExtensions.ApplyFrom`: copy settings, in-place merge lookups, upsert/remove column rules by key, deep-clone rule actions.
- Validation: non-empty Version, >=1 supplier, `FileStructure` indexes >=1, detection patterns required when detection exists, `SubtitleRowHandlingConfiguration.Action` in {parse, skip}, regex transforms need valid patterns, and `ActionConfig` parameters follow strict per-operation rules (assign/find/split/map).
- Parsing waterfall (e.g., supplier-PCA column G): sequentially extract and remove brand/type/gender/etc., use `.Clean` intermediates, and assign the final cleaned text to `Product.Name`.

## Relational Architecture Guardrails
- Entities: Product (core data + `DynamicPropertiesJson`), Supplier, SupplierOffer (catalog metadata), OfferProduct (junction with offer-specific fields + `ProductPropertiesJson`). Maintain audit fields (UTC) via base `Entity` class.
- Property classification: core product properties live on Product (deduplicated); offer-specific properties belong on OfferProduct. Validate new suppliers map columns accordingly.
- JSON storage: ensure serialization integrity for dynamic property bags; do not regress JSON schema assumptions.

## Approval Gates (ping Struly-Dear before proceeding)
- Modifying core entities (`ProductEntity`, `SupplierEntity`, `SupplierOfferEntity`, `OfferProductEntity`, `Entity.cs`).
- Altering existing supplier configurations (`supplier-formats.json`).
- Changing database schema, EF Core configuration, or migration strategy (currently none).
- Updating `ConfigurationBasedNormalizer` or normalization/property-classification flow.

## Output Policy & Reviews
- Fix requests: return diffs only unless Struly-Dear asks for explanation.
- Plans/assessments: concise bullets with `path:line` references.
- Always mention remaining risks/tests and suggest logical next steps (build, tests, deploy) after coding.

## Checklist Before Finishing
- Build modified projects with warnings-as-errors.
- Ensure async flows propagate tokens and avoid `.Result`/`.Wait()`.
- Confirm nullable safety (no stray `!`).
- Dictionaries intended to be case-insensitive use `StringComparer.OrdinalIgnoreCase`.
- Configuration merges keep object references intact and invoke parser lookup merge.
- Structured logging only; no secrets or PII.
