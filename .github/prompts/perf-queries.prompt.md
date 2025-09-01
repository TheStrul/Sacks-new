---
mode: 'ask'
description: 'Query performance review (N+1, tracking, split queries)'
---

Scan repository query methods for:
- N+1 via naive Includes
- Missing AsNoTracking for read-only
- Missing AsSplitQuery on wide graphs
- ToListAsync on large sets without pagination

Output:
1) The 5 worst offenders (File:Line | Why | 1-line Fix).
2) Unified diffs to apply the fixes. ≤ 150 lines.
