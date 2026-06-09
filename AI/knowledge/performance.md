# Knowledge: Unity Performance Best Practices

Hard rules and guidelines for high-performance Unity development. Use this for `unity-optimize` and `unity-advisory` tasks.

## Memory & Garbage Collection (GC)
- **Zero Alloc in Update**: Avoid `new`, `Linq`, or string concatenation in `Update()`, `LateUpdate()`, or `FixedUpdate()`.
- **String Optimization**: Use `StringBuilder` for complex string building. Cache frequently used strings.
- **Collections**: Use `ListPool<T>` or `ArrayPool<T>` to avoid frequent heap allocations for temporary collections.
- **Boxing**: Avoid boxing by using generic types. Be careful with `Enum.HasFlag` and string formatting with value types.

## CPU & Logic
- **Component Caching**: Never call `GetComponent()` in a loop or `Update`. Cache it in `Awake` or `Start`.
- **Finding Objects**: Avoid `GameObject.Find()` and `GameObject.FindWithTag()`. Use direct references or a Registry/Manager pattern.
- **Physics**: Use `NonAlloc` versions of physics queries (e.g., `Physics.RaycastNonAlloc`, `Physics.OverlapSphereNonAlloc`).
- **Loops**: Use `for` instead of `foreach` for large arrays/lists to avoid iterator allocations (in older Unity versions) and for slightly better performance.

## GPU & Rendering
- **Batching**: Minimize Draw Calls by using Static/Dynamic Batching and SRP Batcher.
- **Shaders**: Use `PropertyToID` for shader property access instead of string names.
- **Materials**: Avoid accessing `.material` (creates a copy); use `.sharedMaterial` if possible.

## Data Structures
- **ScriptableObjects**: Use SOs for data-heavy configurations to reduce memory footprint per instance.
- **Structs vs Classes**: Use `structs` for small, short-lived data containers to stay on the stack and reduce GC pressure.
