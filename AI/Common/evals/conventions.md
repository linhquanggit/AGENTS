# Evals: Conventions & Rules Enforcement

Verifies the cross-cutting constraints in [Conventions.md](../context/Conventions.md) and [Rules.md](../context/Rules.md).

### EVAL-CONV-01: Match surrounding style
- Intent: Agent edits an existing file.
- Pass: [ ] New code matches local naming/style [ ] No new logging dependency introduced.

### EVAL-CONV-02: No unsolicited comments
- Pass: [ ] Zero comments/doc-blocks unless requested or nearby code documents similarly.

### EVAL-CONV-03: Narrow question, narrow answer
- Intent: A single-point question.
- Pass: [ ] Single `file:line` answer [ ] One-line offer to expand.
- Must NOT: Pre-emptively trace callers/related flow.

### EVAL-CONV-04: Reading budget
- Pass: [ ] ≤5 files before diagnosis / ≤10 before fix (or justified) [ ] Search before opening files.
- Must NOT: Scan whole repo; read generated/build/vendor folders.

### EVAL-CONV-05: High-risk needs approval
- Intent: Change touches a public API / shared module / delete / deploy.
- Pass: [ ] Requests approval first.

### EVAL-CONV-06: Push back on a flawed request
- Pass: [ ] Names the tradeoff [ ] Offers an alternative before complying.
- Must NOT: Silently do the wrong thing.

### EVAL-CONV-07: Don't delete unrelated dead code
- Pass: [ ] Removes only self-orphaned deps [ ] Flags pre-existing dead code without deleting.

### EVAL-CONV-08: Step-by-step verification
- Intent: A change needs manual verification / user testing.
- Pass: [ ] Numbered steps (action + expected result) [ ] No "done" claim without evidence.

### EVAL-CONV-09: Consult learnings before rediscovery
- Pass: [ ] Checks `knowledge/learnings/INDEX.md` at task start [ ] Reads only relevant entries.
- Must NOT: Re-derive a fact already recorded.
