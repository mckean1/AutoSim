# AGENTS.md

## Language & Framework
- **Target Framework:** .NET Framework 10

## Architecture Guardrails
- Prioritize readability, clear ownership, testability, and maintainability over the smallest possible diff.
- Keep UI/entry-point code thin. It should handle input, dispatch work, and format output; it should not own business rules or workflow orchestration.
- Keep domain/core logic independent from UI, persistence, network, filesystem, and framework-specific concerns.
- Do not add new behavior to god classes or oversized files. Extract a focused service, handler, model, or helper first.
- Prefer small classes/modules with one clear responsibility and one reason to change.
- Avoid mixing input handling, orchestration, business rules, rendering, and persistence in the same class.
- Use explicit names that describe responsibility. Avoid vague catch-all names like `Manager`, `Helper`, `Processor`, or generic `Result` types when a more specific name would be clearer.
- Prefer immutable records/value objects for results, snapshots, configuration, and domain values where practical.
- Avoid long positional argument lists for complex data. Prefer named parameters, options objects, builders, or object initializers.
- Add or preserve tests for meaningful behavior, especially before refactoring risky code.
- After changes, review the diff for responsibility drift and report any remaining architecture concerns.

## Naming Conventions
- **PascalCase** for public members, types, namespaces, methods, properties, events, and constants.
- **camelCase** for local variables, parameters, and private fields.
- Prefix private fields with `_` (e.g., `_fieldName`).
- Prefix interfaces with `I` (e.g., `IRepository`).
- Use meaningful, descriptive names. Avoid single-letter names except in short lambdas or loops.

## Code Layout & Formatting
- Use **4 spaces** for indentation (no tabs).
- One class per file. File name must match the class name.
- One enum per file. File name must match the enum name.
- Constants should be logically grouped in separate Constants files. e.g., `DatabaseConstants.cs`.
- Place `using` directives at the top of the file, outside the namespace.
- Remove unused `using` directives.
- Sort `using` directives alphabetically.
- Use braces `{}` for all control flow statements, even single-line bodies.
- Keep lines under **120 characters** where practical.
- Don't add fluff comments. Code should be self-explanatory.
- Use XML documentation comments for public members and types.
- Properties should be at the top of the class, followed by constructors, then methods.
- Avoid using conditional blocks in switch statements; If needed place them in a separate method.

## Type & Member Design
- Always specify access modifiers explicitly (e.g., `public`, `private`, `internal`).
- Prefer `readonly` for fields that are assigned only in the constructor.
- Use `const` for compile-time constants; use `static readonly` for runtime constants.
- Use expression-bodied members for simple single-expression methods or properties.
- Mark classes `sealed` unless explicitly designed for inheritance.
- Prefer composition over inheritance.

## Null Handling
- Validate public method parameters with `ArgumentNullException` guard clauses.
- Use null-conditional (`?.`) and null-coalescing (`??`) operators where appropriate.
- Avoid returning `null` from methods that return collections; return empty collections instead.

## Exception Handling
- Throw specific exception types (e.g., `ArgumentNullException`, `InvalidOperationException`).
- Do **not** catch `System.Exception` unless re-throwing or at a top-level handler.
- Include meaningful messages in exceptions.
- Log errors before throwing when a logger is available.

## Collections & LINQ
- Use `IList<T>`, `IEnumerable<T>`, or `IReadOnlyList<T>` for return types and parameters over concrete types.
- Prefer LINQ methods over manual loops for querying and filtering.
- Avoid multiple enumeration of `IEnumerable<T>`; materialize with `.ToList()` or `.ToArray()` when needed.

## String Handling
- Use string interpolation (`$"..."`) over `string.Format` or concatenation.
- Use `string.IsNullOrWhiteSpace` over `string.IsNullOrEmpty` unless whitespace is valid.
- Use `StringComparison.Ordinal` or `StringComparison.OrdinalIgnoreCase` for non-linguistic comparisons.

## XML & Data Parsing
- Use `XDocument` / `XElement` (LINQ to XML) for XML manipulation.
- Prefer `TryParse` over `Parse` for user-supplied or untrusted data to avoid `FormatException`.
- Use `CultureInfo.InvariantCulture` when parsing or formatting numeric and date values from XML or external sources.

## Logging
- Use structured logging with contextual information (e.g., IDs, entity names).
- Use appropriate log levels: `Debug` for diagnostics, `Info` for flow, `Warn` for potential problems, `Error` for failures.
- Do **not** log sensitive or personally identifiable information.

## Testing
- Name test methods: `MethodName_Scenario_ExpectedResult`.
- One assertion concept per test.
- Use mocks/stubs for external dependencies (database, network).
- Keep tests independent and idempotent.
- Use NUnit and Moq for unit testing and mocking.
- Tests should be organized in a `ProjectName.Tests` project with a parallel namespace structure to the main codebase.

## Project Structure
- Organize code by feature or domain area within namespaces (e.g., `ProjectName.Constants`, `ProjectName.Enums`).
- Place interfaces in an `Interfaces` folder/namespace.
- Place constants in a `Constants` folder/namespace.
- Place enums in an `Enums` folder/namespace.
- Place database entity types in a `DatabaseEntities` folder/namespace.
