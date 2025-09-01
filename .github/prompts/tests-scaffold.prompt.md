---
mode: 'ask'
description: 'Generate minimal tests for SacksDataLayer'
---

Generate xUnit tests for SacksDataLayer using SQLite in-memory:
- One happy path + one failure path per repository.
- One transactional multi-entity test.
- Provide diffs creating files under tests/SacksDataLayer.Tests/*

Output:
- File list + unified diffs only. ≤ 150 lines.
