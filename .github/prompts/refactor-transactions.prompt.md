---
mode: 'ask'
description: 'Audit transactions & unit-of-work'
---

Audit transaction usage across SacksDataLayer:
- Are multi-entity writes wrapped in a single transaction?
- Is SaveChanges called inside loops?
- Are concurrency tokens/rowversion used where appropriate?

Output:
1) Tight issues table (Severity | File:Line | Problem | Fix) ≤ 60 lines.
2) Unified diffs for SacksDbContext + affected repos ≤ 120 lines. Chunk and stop with READY FOR CONTINUE if needed.
