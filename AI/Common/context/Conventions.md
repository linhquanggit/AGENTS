# Conventions

Authoritative coding conventions. Apply on every edit. **Match surrounding code first** — the local style wins over any general preference.

## Naming
- Follow the language's idiomatic conventions; above that, match the names and casing already used in the file/module.
- Names describe intent. Avoid abbreviations the codebase doesn't already use.

## Comments & Docs
- No comments or doc-blocks unless explicitly requested, or unless nearby code documents similar things.

## Logging
- Use the project's existing logging facility, format, and levels; match how nearby code logs. Do not introduce a new logging dependency.

## Code Generation
- Prefer modifying existing patterns over introducing new ones.
- Minimize the number of files touched and lines changed.
- Reuse existing helpers, base classes, and modules before adding new ones.
