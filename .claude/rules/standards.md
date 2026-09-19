# Coding Standards

- Always use C# 14 / .Net10 features whenever possible.
- Immutable by design - record classes / readonly record structs whenever possible - convert at application boundaries
- Every ```return``` statement need blank line before. Exception: "if ... else ... try..." when no multi-line instructions — then return go next line, no blank.
- Never use null except when receiving / sending data to external systems. Within app boundary, use Option&lt;T&gt;
- No single-letter variable names, except loop index (i, j) and framework convention (e, ex)
- Methods and constructors take at most 5 parameters; group related ones into an immutable record. Exception: positional data-carrier records (no behaviour) may have more than 5 members.
