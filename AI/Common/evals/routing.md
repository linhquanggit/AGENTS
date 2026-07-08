# Evals: Intent Routing

Verifies natural-language intent routes to the correct skill per [Workflows.md](../context/Workflows.md).

### EVAL-ROUTE-01: Bug
- Intent: "This function returns the wrong value sometimes."
- Expected: Routes to `investigate`.
- Pass: [ ] Selects `investigate` [ ] Reproduces/bounds before searching.

### EVAL-ROUTE-02: Brainstorm
- Intent: "I want a caching layer but I'm not sure how."
- Expected: Routes to `brainstorm`.
- Pass: [ ] Selects `brainstorm` [ ] Asks blocking-only questions before any code.

### EVAL-ROUTE-03: Feature
- Intent: "Add CSV export to the report page."
- Expected: Routes to `feature`.
- Pass: [ ] Selects `feature` [ ] Finds an existing pattern to extend.

### EVAL-ROUTE-04: Refactor
- Intent: "Rename this class and split the god-module."
- Expected: Routes to `refactor`.
- Pass: [ ] Selects `refactor` [ ] Maps callers before renaming.

### EVAL-ROUTE-05: Migrate
- Intent: "Move everything off the deprecated HTTP client."
- Expected: Routes to `migrate`.
- Pass: [ ] Selects `migrate` [ ] Enumerates all sites first.

### EVAL-ROUTE-06: Review
- Intent: "Review my diff."
- Expected: Routes to `review`.
- Pass: [ ] Selects `review` [ ] Reviews only the changed scope.

### EVAL-ROUTE-07: Test
- Intent: "Write tests for the parser."
- Expected: Routes to `test`.
- Pass: [ ] Selects `test` [ ] RED before GREEN.

### EVAL-ROUTE-08: Explain
- Intent: "Explain how auth middleware runs."
- Expected: Routes to `explain`.
- Pass: [ ] Selects `explain` [ ] Bounds scope.

### EVAL-ROUTE-09: Optimize
- Intent: "This endpoint is slow under load."
- Expected: Routes to `optimize`.
- Pass: [ ] Selects `optimize` [ ] Requires bottleneck evidence.

### EVAL-ROUTE-10: Learn
- Intent: "Remember this for next time."
- Expected: Routes to `learn`.
- Pass: [ ] Selects `learn` [ ] Asks approval before writing.
