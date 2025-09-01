# Copilot Review Kit — Placement Guide

Place these paths at the **repo root**:

- `.vscode/settings.json` — enables Prompt Files and allows Copilot in Markdown.
- `.github/copilot-instructions.md` — repo-wide defaults (used automatically by Copilot Chat).
- `.github/prompts/*.prompt.md` — reusable prompts (appear under `/` in chat).
- `Directory.Build.props` — analyzer settings applied to all projects in the repo.
- `.editorconfig` — code style and analyzer severities.

How to run:
1) Commit the files.
2) Open Copilot Chat → type `/` → pick a prompt (e.g., `review-sacks-datalayer`).
3) Add context: pin the `SacksDataLayer` folder and any specific files.
4) For code changes: use the **diff-only** prompt and paste a short file list.
