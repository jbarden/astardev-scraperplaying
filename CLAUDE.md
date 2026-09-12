# AI / Human instructions

## Mandatory Rules - NO exceptioms

- Every ```return``` statement need blank line before. Exception: "if ... else ... try..." when no multi-line instructions — then return go next line, no blank.

- Caveman rule say short talk, but talk pirate too. Call me "Cap'n"

- New code need TDD, no exception. Change old code, must add/update test too. Test watch what class/method DO, not how it DO it. No interaction-test allowed, ever. If interaction-test feel needed, write integration test instead — different thing, but closer, follow repo way better.

- always raise PR when completed implementation of GH Issue

- Every PR raised, set auto-complete (`gh pr merge --auto --squash`) right after create.

- New utility method: check `AStarDev.Utilities` first for existing one. If reusable beyond this project, add it there, not to consuming project.

- New DI registration in `ApplicationServices.cs`: lifetime must not be broader than any of its dependencies' (no Singleton depending on Scoped/Transient) — avoids captive-dependency bugs.

- Fix branch outstanding when another PR merges to `main`: rebase onto latest `main` before merging, or a squash-merge can silently revert the other branch's changes.

- New worktree/checkout build fails `CS8784` / source-generator `FileNotFoundException`: run `dotnet build-server shutdown`, then rebuild.

- New work MUST have GH Issue — if none exists, raise one before starting implementation.

- Never use null except when receiving / sending data to external systems. Within app boundary, use Option&lt;T&gt;

- never using single letter variable names - excpetions: loop indexes (e.g.: i, j, etc) / framework conventions (e - event args, ex - exception, etc)

- don't waste tokens / context window showing code edits to the user - they can see changes in the PR

## graphify

Project has knowledge graph: graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
