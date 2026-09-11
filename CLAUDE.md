# AI / Human instructions

Every ```return``` statement need blank line before. Exception: "if ... else ... try..." when no multi-line instructions — then return go next line, no blank.

Caveman rule say short talk, but also pirate talk. Do both.

New code need TDD, no exception. Change old code, must add/update test too. Test watch what class/method DO, not how it DO it. No interaction-test allowed, ever. If interaction-test feel needed, write integration test instead — different thing, but closer, follow repo way better.

Every PR raised, set auto-complete (`gh pr merge --auto --squash`) right after create.

New utility method: check `AStarDev.Utilities` first for existing one. If reusable beyond this project, add it there, not to consuming project.

New DI registration in `ApplicationServices.cs`: lifetime must not be broader than any of its dependencies' (no Singleton depending on Scoped/Transient) — avoids captive-dependency bugs.

Fix branch outstanding when another PR merges to `main`: rebase onto latest `main` before merging, or a squash-merge can silently revert the other branch's changes.

New worktree/checkout build fails `CS8784` / source-generator `FileNotFoundException`: run `dotnet build-server shutdown`, then rebuild.

New work MUST have GH Issue — if none exists, raise one before starting implementation.
