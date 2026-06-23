# Knowledge: Unity Anti-Patterns (Correctness & Lifecycle)

Common Unity traps that cause bugs, leaks, or crashes — distinct from `performance.md` (speed). Use this for `unity-review`, `unity-investigate`, and `unity-advisory`.

## Events & Lifecycle
- **Event leaks**: every `+=` needs a matching `-=`. Subscribe in `OnEnable`, unsubscribe in `OnDisable` (or `Awake`/`OnDestroy`). Leaked handlers keep objects alive and fire on dead targets.
- **OnEnable/OnDisable symmetry**: anything started in `OnEnable` must be stopped in `OnDisable`.
- **Execution order**: don't assume another object's `Awake`/`Start` ran first. Use `Awake` for self refs, `Start` for cross refs, or explicit init.
- **Coroutines**: stop when the GameObject/MonoBehaviour is disabled or destroyed — don't rely on one finishing cleanup. Stop via the saved `Coroutine` handle, not a string name.
- **async/await**: Unity does not auto-cancel. Pass a `CancellationToken` (or `destroyCancellationToken`) and re-check `this == null` after every `await` — the object may have been destroyed.

## Unity null semantics
- `UnityEngine.Object` overloads `==` (a destroyed object compares `== null`). The `?.` and `??` operators **bypass** this overload → use explicit `if (obj == null)` / `if (obj != null)`, never `obj?.Foo()` or `obj ?? fallback`, on Unity objects.
- A destroyed object is `== null` but not reference-null; don't track "alive" with `is not null`/`is null`.

## Instantiation & destruction
- `Destroy` is deferred to end of frame; the object still exists this frame. `DestroyImmediate` is editor-tooling only.
- Reparent safely: `Instantiate(prefab, parent, false)` to keep local transform; plain `transform.SetParent(p)` defaults to keeping world position.
- Frequent spawns without pooling cause GC stutter → pool (see `performance.md`).

## Serialization & Inspector
- Renaming a `[SerializeField]` field drops its saved Inspector value — add `[FormerlySerializedAs("old")]`.
- The Inspector value overrides field initializers; don't expect `= default` in code to win.
- Prefer `[SerializeField] private` over `public` for serialized data (encapsulation; public is serialized too).
