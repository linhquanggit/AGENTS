# Skill: unity-test

Write and run Unity tests, following RED-GREEN-REFACTOR when implementing new behavior.

## Procedure
1. **Pick Mode**:
   - **EditMode** for pure logic (fast, no scene). **PlayMode** for MonoBehaviour lifecycle, coroutines, physics.
2. **RED**:
   - Write a failing test naming the expected behavior. Run it; confirm it fails for the RIGHT reason.
3. **GREEN**:
   - Write the minimal code to make it pass. Nothing more.
4. **REFACTOR**:
   - Clean up while keeping the test green.
5. **Placement**:
   - Tests live in a test asmdef. Name tests `Method_Scenario_ExpectedResult`. Add setup/teardown for PlayMode scene state.

## Anti-Hallucination Guardrails
- **DO NOT** write implementation before a failing test (when doing TDD).
- **DO NOT** test Unity engine internals — test your logic.
- **DO NOT** leave PlayMode tests that depend on scene state without setup/teardown.
- One behavior per test; assert the behavior, not the implementation.

## Output
- **Test files**: with clear names.
- **Run result**: pass/fail with the failing reason if RED.
- **Coverage note**: which behavior is now guarded.
