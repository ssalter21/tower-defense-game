---
name: sync-main
description: Fetch origin/main and rebase the current branch onto it.
disable-model-invocation: true
---

# Sync with main

Bring the branch in **this** worktree up to date with what is on GitHub. Every other worktree's branch is left
exactly where it is.

## Steps

1. **Fetch.** `git fetch origin main`. From here on, "main" means `origin/main`; the local `main` ref may lag it
   and is not the source of truth.

2. **Clear the tree.** `git status --porcelain`. Two cases:
   - The only entry is `client/Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` — that is Unity's
     first-import side effect in a fresh worktree, not work. Discard it:
     `git checkout -- client/Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`.
   - Anything else — `git stash push -u -m sync-main`, and say in your reply that you stashed.

3. **Rebase.** Record `git rev-parse HEAD` first, then branch on where you are:
   - On `main`: `git merge --ff-only origin/main`. If it refuses, `main` has local commits that were never
     pushed — stop, report them (`git log --oneline origin/main..main`), and let Sam decide.
   - On any other branch: `git rebase origin/main`.

4. **Conflicts.** Resolve with the `resolving-merge-conflicts` skill, then `git rebase --continue`. Resolve every
   conflict on its merits — `git rebase --skip` drops the commit's changes wholesale and reads as success.
   Regenerated files (`.meta`, `packages-lock.json`) follow AGENTS.md rule 4: they belong in the commit that
   caused them, so keep the side that matches the source change rather than picking one at random.

5. **Restore.** If step 2 stashed, `git stash pop` and check for conflicts again.

6. **Report.** The old SHA, the new SHA, and `git log --oneline <old-sha>..HEAD` — the commits that arrived. If
   the branch had commits of its own, say they were replayed and are now different SHAs.

## Rules

- **Only this worktree's branch gets moved.** This repo runs several live worktrees at once, and several hold
  shared branches. Catching another branch up with `git branch -f` or `git update-ref` moves HEAD out from under
  the checkout that holds it, whose index still reflects the old commit — it lights up with phantom staged
  deletions and nothing says why. Each worktree syncs itself by running this skill there.
- **Stop at the rebase and hand back.** A rebased branch needs `git push --force-with-lease` to land, which
  rewrites what collaborators already have — that call is Sam's.
