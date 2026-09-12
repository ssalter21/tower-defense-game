---
name: sync-head
description: Fetch the branch below this one in the PR stack and rebase the current branch onto it.
disable-model-invocation: true
---

# Sync with the stack

The same job as [sync-main](../sync-main/SKILL.md) — bring **this** worktree's branch up to date and leave every
other worktree's branch where it is — but the target is the PR stack, not `main`. PRs here are stacked: each
task's branch is cut from the newest open PR's head, and review merges bottom-up, so "up to date" means "on top
of the branch below me".

## Steps

1. **Find the base.** Three cases, checked in order:
   - This branch has an open PR: `gh pr view --json baseRefName --jq .baseRefName`. GitHub retargets a PR when
     its base merges, so this stays right as the stack shrinks.
   - No PR on this branch: the newest open PR's head,
     `gh pr list --state open --limit 1 --json headRefName --jq '.[0].headRefName'`.
   - No open PR anywhere: there is no stack — run `sync-main` instead and stop here.

   Then `git fetch origin <base>`. From here on "the base" means `origin/<base>`.

2. **Clear the tree.** sync-main step 2, with the stash tagged `sync-head`.

3. **Rebase.** Record `git rev-parse HEAD`. A stacked base is rewritten every time its own PR syncs, so a plain
   `git rebase origin/<base>` would replay the base's *old* commits as if they were this branch's. Replay only
   this branch's own commits:
   - With a PR: two separate commands — `gh pr view --json commits --jq '.commits | length'` to get N, then
     `git rebase --onto origin/<base> HEAD~<N>` with the number written in. The worktree guard refuses a git
     command that carries a `$(...)` substitution.
   - Without a PR: `git log --oneline origin/<base>..HEAD` first. If every line is a commit made in this
     worktree, `git rebase origin/<base>`. If not, the branch was cut from a since-rewritten tip: count the
     commits that are yours and use `--onto` as above.

4. **Conflicts and restore.** sync-main steps 4 and 5.

5. **Report.** sync-main step 6, plus the base branch and the PR it was read from.

## Rules

- sync-main's rules hold: only this worktree's branch moves, and the `--force-with-lease` push is Sam's call.
- **Sync propagates upward.** Once this branch is force-pushed, the PR whose base is this branch is stale in
  exactly the same way; its worktree runs `sync-head` next. Say so in the report when such a PR exists
  (`gh pr list --state open --base <this-branch>`).
