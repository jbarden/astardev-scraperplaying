# AI / Human instructions

## Mandatory Rules - NO exceptions

- Caveman rule say short talk, but talk pirate too. Call me "Cap'n"
- always raise PR when completed implementation of GH Issue
- Every PR raised, set auto-complete (`gh pr merge --auto --squash`) right after create.
- New utility method: check `AStarDev.Utilities` first for existing one. If reusable beyond this project, add it there, not to consuming project.
- New work MUST have GH Issue — if none exists, raise one before starting implementation.
- New DI registration in `ApplicationServices.cs`: lifetime must not be broader than any of its dependencies' (no Singleton depending on Scoped/Transient) — avoids captive-dependency bugs.
- Fix branch outstanding when another PR merges to `main`: rebase onto latest `main` before merging, or a squash-merge can silently revert the other branch's changes.
- New worktree/checkout build fails `CS8784` / source-generator `FileNotFoundException`: run `dotnet build-server shutdown`, then rebuild.
- Never show code edits to the user - they can see changes in the PR
- ALWAYS follow rules in: @.claude/rules/standards.md @.claude/rules/styling.md and @.claude/rules/testing.md
- Before using `??` / `?.` / a null check to handle a "missing value" case, confirm the API's actual sentinel for absence — many BCL/library methods return empty string, empty collection, or a default struct instead of null (e.g. `Path.GetExtension` returns `""`, not `null`, when the path has no extension, so `Path.GetExtension(path) ?? fallback` never falls back). When in doubt, write a throwaway check or read the docs rather than assume. Add a test for that specific falsy-but-not-null case, not just the null case.
- A shorter/more "idiomatic" rewrite of existing logic is not proof it's correct. If existing code has an unusual shape (e.g. an explicit `IsNullOrEmpty` check instead of `??`), treat it as intentional and re-run the tests covering that behaviour before simplifying it — don't rely on the rewrite "looking right".
- Group classes / services / types by domain usage, not by type: Customer folder with: CustomerDto, CustomerEntity, CustomerRepo not separate folders for: Domain, DTOs, Repositories

## graphify

Project has knowledge graph: graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:

- codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
