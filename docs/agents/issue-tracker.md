# Issue tracker: GitHub Issues

Issues and PRDs for this repo are GitHub Issues in `ssalter21/tower-defense-game`, driven by the `gh` CLI. A
skill that says "the tracker doc" means this file.

## Conventions

- Label every issue in an effort `effort:<slug>`; label the effort's PRD/tracking issue `map` as well.
- Label the ticket type `type:grilling` / `type:research` / `type:prototype` / `type:task`.
- Label a fully specified ticket `ready-for-agent`; label one that will not be actioned `wontfix`. There is no
  `needs-triage`, `needs-info` or `ready-for-human` — an untriaged ticket is simply unlabelled. Create a label
  before using it rather than assuming it exists.
- Label a pull request `regenerated-deliberately` when its branch moves a golden artefact — the trace, the
  landmark table, `content/golden/`, the sweep, the run outcome, the replay or the command list — because the
  gate's `tools/check-golden-label.ps1` is red without it, and the label is a person saying they read the
  regenerated diff.
- Read open/closed off native issue state; there is no `Status:` line. "Resolved" is closed with an
  `## Answer` comment; "claimed" is open and assigned.
- Converse in ordinary issue comments.

## Review boundary: `stack`

- Open one PR per ticket, branched from the head of the newest open PR — or from the default branch when
  none is open — and targeted at it, so review reads in the order the work was built and no ticket waits on
  a merge. `/implement` finds the base at implement time; tickets carry no branch section, because the base
  depends on what has merged since the tickets were written.
- Expect the stack to drain from the bottom: merging the bottom PR deletes its branch and GitHub retargets the
  one above at the default branch. Rebase a bottom PR that changed under review into the PRs above it before
  they are read.
- Take a small change that is not a ticket **off the stack** — cut from `origin/main`, opened with
  `--base main` — when all three hold: it is one small PR and not a ticket in an effort; it touches no file an
  open stack PR touches (`gh pr diff <n> --name-only` per open PR); and nothing in the stack waits on it. After
  it merges, run `/sync-main` in the bottom PR's worktree so the stack is never read against a stale main. A
  person chooses this lane, never `/implement`.

## Commands

- **Publish a ticket:** `gh issue create --title "<title>" --label "effort:<slug>,type:<kind>" --body "<body>"`.
  Parent it to its map at creation with `--parent <map-number>` (or `gh issue edit <map-number>
  --add-sub-issue <n>` after) — the native sub-issue is the link; a `Part of …` line in the body is prose.
- **Fetch a ticket:** `gh issue view <number>`. Add `--comments` only when the history is actually needed.
- **Session-start overview** — ask for the narrow fields; `--json …comments…` across an effort burns tokens
  for a glance:
  - Open issues, one line each:
    `gh issue list --state open --limit 50 --json number,title,labels,assignees --jq '.[] | "\(.number)\t\([.labels[].name] | join(","))\t\(.title)"'`
  - Frontier candidates for one effort:
    `gh issue list --label effort:<slug> --state open --json number,title,assignees --jq '.[] | select(.assignees == []) | "\(.number)\t\(.title)"'`
    — then drop any with an open blocker.
  - The map: `gh issue list --label map --state open --json number,title,url,body`

## Wayfinding operations

What `/wayfinder` needs that is specific to this repo; the map's shape, the frontier and the claim are the
skill's.

- Keep the labels repo-local — `map` / `effort:<slug>` / `type:<kind>`, not the skill's `wayfinder:*` words.
- Create a child ticket already parented and triaged:
  `gh issue create --parent <map-number> --label "effort:<slug>,type:<kind>,ready-for-agent" --title "..." --body "..."`.
- Record a blocking edge natively **and** in the ticket's `## Blocked by` body section; the native edge wins
  when they disagree. Add one with
  `gh api --method POST repos/ssalter21/tower-defense-game/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`,
  where `<blocker-db-id>` is the blocker's numeric **database id** —
  `gh api repos/ssalter21/tower-defense-game/issues/<n> --jq .id` — not the `#number` and not the `node_id`.
  Read edges back with `gh api repos/ssalter21/tower-defense-game/issues/<n> --jq .issue_dependencies_summary`
  (`blocked_by` counts **open** blockers) or list them with
  `gh api repos/ssalter21/tower-defense-game/issues/<n>/dependencies/blocked_by --jq '.[] | "\(.number)\t\(.state)\t\(.title)"'`.
- Claim: `gh issue edit <number> --add-assignee @me`, as the session's first write.
- Resolve: `gh issue comment <number> --body "## Answer\n\n..."`, then
  `gh issue close <number> --reason completed`, then append a one-line gist and link to the map body's
  `## Decisions so far` — edit the body, not a comment, because the body is the decision index.
- Close a ticket when its PR is open and pushed, not when it merges, because under `stack` waiting for the
  merge would keep every dependent blocked for the whole run. Closing a blocker is what unblocks its
  dependents; GitHub recomputes `blocked_by` from issue state.
- List no open tickets in the map body — they are its open sub-issues, found by query.
