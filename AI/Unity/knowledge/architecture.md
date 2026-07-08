# Knowledge: Unity Architecture & Patterns

Standard architectural patterns to ensure scalability and maintainability.

## Core Principles
- **Composition over Inheritance**: Prefer building complex behaviors by combining small, reusable components rather than deep class hierarchies.
- **Decoupling**: Use Events (C# Events or ScriptableObject Events) to communicate between unrelated systems.
- **Data-Driven Design**: Keep logic in MonoBehaviors and data in ScriptableObjects or JSON/ScriptableObject configs.

## Common Patterns
- **Manager/Service Locator**: Use a centralized place to access global systems (e.g., `SoundManager`, `UIManager`).
- **Singleton (Careful Use)**: Use Singletons only for truly global, persistent systems. Prefer Dependency Injection or Service Locators if possible.
- **State Machine**: Use Finite State Machines (FSM) for complex AI or UI flows to keep logic organized.
- **Factory Pattern**: Use for dynamic object creation, especially when combined with Object Pooling.

## ScriptableObject Architecture
- **Variables**: Use SOs for shared variables (FloatVariable, IntVariable) to allow different systems to react to changes without direct references.
- **Events**: Use SOs as channels (GameEvent) to broadcast signals across the project.
- **Sets**: Use SOs to maintain lists of active objects (RuntimeSets), like a list of all active enemies.
