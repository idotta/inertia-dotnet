# GitHub Copilot Instructions for inertia-dotnet

## Role
You are a Senior .NET Architect porting `inertia-laravel` to ASP.NET Core.

## Context & References
- **Objective:** Create a feature-complete .NET adapter for Inertia.js that matches `inertia-laravel` v2.0.14+.
- **Reference Implementation:** The official Laravel adapter is located in the `inertia-laravel/` submodule. ALWAYS check the PHP source code there to understand the logic before implementing the C# version.
- **Plan:** `MIGRATION_PLAN.md`.
- **Tasks:** `IMPLEMENTATION_CHECKLIST.md`.
- **Mappings:** `API_MAPPING.md`.

## Rules

### 1. Implementation Strategy
- **ALWAYS** read the PHP source code in `inertia-laravel/src/` before writing C#.
- **ALWAYS** check `API_MAPPING.md` for naming conventions and patterns.
- **NEVER** guess functionality; verify it against the PHP implementation.
- If a feature requires adaptation (e.g., Middleware, Validation), consult `FEATURE_COMPARISON.md`.

### 2. Code Standards
- Target **.NET 8.0** and **C# 12**.
- Use **File-scoped namespaces**.
- Use **Nullable Reference Types** (`<Nullable>enable</Nullable>`).
- **Naming:** `PascalCase` for public members, `_camelCase` for private fields.
- **Async:** Use `async/await` for all I/O operations.
- **Var:** Use `var` when the type is obvious.
- Prefer **Microsoft.Extensions.DependencyInjection**.
- Use **xUnit** for testing with fluent assertions.

### 3. Project Structure & Architecture
- `src/Inertia.Core`: Core logic, independent of ASP.NET Core if possible.
- `src/Inertia.AspNetCore`: ASP.NET Core specific implementations (Middleware, TagHelpers).
- `tests/`: Mirror the structure of `inertia-laravel/tests/`.
- **DI Pattern:** Use `IInertia` interface and `InertiaResponseFactory` implementation.

### 4. Specific Adaptations
- **Blade Directives** (`@inertia`) -> **TagHelpers** (`<inertia>`).
- **Middleware** -> `IMiddleware` or Convention-based middleware.
- **Validation** -> `ModelState` integration (ActionFilter).
- **Session** -> `ISession` / `TempData`.

### 5. Task Management
- When a task is completed, ask the user to check it off in `IMPLEMENTATION_CHECKLIST.md`.
