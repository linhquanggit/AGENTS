# Skill: test

Write and run tests, following RED-GREEN-REFACTOR when implementing new behavior.

## Procedure
1. **Pick Level**: Unit for pure logic (fast, isolated); integration for cross-module/IO behavior.
2. **RED**: Write a failing test naming the expected behavior. Run it; confirm it fails for the RIGHT reason.
3. **GREEN**: Write the minimal code to make it pass. Nothing more.
4. **REFACTOR**: Clean up while keeping the test green.
5. **Placement**: Follow the project's test layout and naming. Add setup/teardown for shared state.

## Anti-Hallucination Guardrails
- **DO NOT** write implementation before a failing test (when doing TDD).
- **DO NOT** test framework/library internals — test your logic.
- **DO NOT** leave tests that depend on shared state without setup/teardown.
- One behavior per test; assert the behavior, not the implementation.

## Output
- **Test files** (clear names); **Run result** (pass/fail + reason if RED); **Coverage note** (behavior guarded).
