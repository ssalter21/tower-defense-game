# Issue tracker: GitHub Issues

Issues and PRDs for this repo live as GitHub Issues in `ssalter21/tower-defense-game`, managed via the
`gh` CLI. Skills that say "the tracker doc" mean this file.

## Conventions

- One label per feature/effort: `effort:<slug>` (e.g. `effort:deepening`), applied to every issue belonging to
  that effort.
- The PRD/tracking issue for an effort carries the `map` label in addition to its `effort:<slug>` label.
- Ticket type is recorded as a `type:<kind>` label (`type:grilling` / `type:research` / `type:prototype` /
  `type:task`).
- Triage state uses `ready-for-agent` (fully specified, an agent can start) and `wontfix` (will not be
  actioned). This repo carries no `needs-triage` / `needs-info` / `ready-for-human` labels — a ticket that is
  not `ready-for-agent` is simply unlabelled for triage. Create the label before using it rather than assuming
  it exists.
- `regenerated-deliberately` goes on a pull request whose branch moves a golden artefact — the trace, the
  landmark table, `content/golden/`, the sweep, the run outcome, the replay or the command list. The build
  gate's `tools/check-golden-label.ps1` step is red without it, so the label is a person saying they read
  the regenerated diff.
- Native issue state is the source of truth for open/closed — there is no separate `Status:` line. "Resolved"
  means the issue is closed with an `## Answer` comment; "claimed" means it is open and assigned.
- Comments and conversation history are ordinary GitHub issue comments.

## Review boundary

Where human review happens. **This repo uses `effort`**: one PR per effort, not per ticket.

At `/to-tickets` publish time, cut `effort/<slug>` from the default branch. Each implement ticket carries a
`## Target branch` section naming it and lands there as one unsquashed commit, so a fresh `/implement` session
needs no other context; per-ticket `/code-review` is the gate at that granularity. A final **integrate
ticket** — blocked by every other ticket in the effort — brings the branch up to date with the default branch,
runs the full suite and `/code-review` against the merge-base, then opens the single PR `effort/<slug>` →
default, its body assembled from the spec and, where the effort has a map, the map's Decisions-so-far section.

The reason is review granularity. Fourteen separate PRs for one architectural effort is fourteen
context-switches for a human who needs to see the whole shape to judge any part of it.

## When a skill says "publish to the issue tracker"

Create a new issue: `gh issue create --title "<title>" --label "effort:<slug>,type:<kind>" --body "<body>"`.
To attach it to a tracking/map issue, wire it as a **native sub-issue** — pass `--parent <map-number>` at
creation (or `gh issue edit <map-number> --add-sub-issue <n>` after) rather than only mentioning the parent in
the body. See "Wayfinding operations" for the full child-ticket recipe.

## When a skill says "fetch the relevant ticket"

`gh issue view <number>`. The user will normally pass the issue number or URL directly.

## Session-start overview

Ask for the narrow fields. `gh issue view --json ...comments...` across an effort pulls every comment body and
burns a lot of tokens for a status glance.

- Open issues, one line each:
  `gh issue list --state open --limit 50 --json number,title,labels,assignees --jq '.[] | "\(.number)\t\([.labels[].name] | join(","))\t\(.title)"'`
- Frontier candidates for one effort:
  `gh issue list --label effort:<slug> --state open --json number,title,assignees --jq '.[] | select(.assignees == []) | "\(.number)\t\(.title)"'`
  — then drop any with an open blocker (see **Frontier** below).
- The map: `gh issue list --label map --state open --json number,title,url,body`
- One issue in full, comments excluded: `gh issue view <number>`. Add `--comments` only when the conversation
  history is actually needed.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a tracking issue, and each ticket is a **native GitHub sub-issue** of it.
(Labels stay repo-local: `map` / `effort:<slug>` / `type:<kind>`, not the skill's `wayfinder:*` vocabulary.)

- **Map**: a GitHub issue labeled `map` + `effort:<slug>`, body holding the Destination / Notes /
  Decisions-so-far / Not-yet-specified / Out-of-scope sections (see the map body template below). The map is an
  **index** — resolved decisions are appended as one-line gists to the body's `## Decisions so far` section,
  each linking its child ticket where the detail lives. Do **not** log decisions as comments; the body is the
  canonical decision index.
- **Child ticket**: a GitHub issue labeled `type:<kind>` + `effort:<slug>`, wired to the map as a **native
  sub-issue** so the tracker UI renders the hierarchy and progress rollup. Create it already parented:
  `gh issue create --parent <map-number> --label "effort:<slug>,type:<kind>,ready-for-agent" --title "..." --body "..."`.
  Retro-wire an existing child with `gh issue edit <map-number> --add-sub-issue <child-number>`. A
  human-readable `Part of the <effort> effort: #<map-number>` line in the body is optional prose, not the
  link — the sub-issue relationship is.
- **Blocking**: GitHub's **native issue dependencies** are the canonical, UI-visible representation, and this
  repo's tickets *also* carry a `## Blocked by` section in the body. Both are in use; the native edge wins when
  they disagree, and a ticket that gains an edge should have its body section updated to match. Add an edge
  with
  `gh api --method POST repos/ssalter21/tower-defense-game/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`,
  where `<blocker-db-id>` is the blocker's numeric **database id**
  (`gh api repos/ssalter21/tower-defense-game/issues/<n> --jq .id`, _not_ the `#number` or `node_id`). Read the
  edges back with
  `gh api repos/ssalter21/tower-defense-game/issues/<n> --jq .issue_dependencies_summary` — `blocked_by` is the
  count of **open** blockers. To list which ones:
  `gh api repos/ssalter21/tower-defense-game/issues/<n>/dependencies/blocked_by --jq '.[] | "\(.number)\t\(.state)\t\(.title)"'`.
  A ticket is unblocked when every blocker is closed.
- **Frontier**: the map's open sub-issues with no assignee and no open blocker; first by number wins.
  `gh issue list --label effort:<slug> --state open` lists candidates — drop any with an assignee or an open
  blocker (`issue_dependencies_summary.blocked_by > 0`, or an open issue in the `## Blocked by` section).
- **Claim**: `gh issue edit <number> --add-assignee @me` before starting work — the session's first write.
- **Resolve**: `gh issue comment <number> --body "## Answer\n\n..."`, then
  `gh issue close <number> --reason completed`, then **append a one-line gist + link to the map body's
  `## Decisions so far` section** (edit the body, not a comment).

Closing a blocker is what unblocks its dependents — GitHub recomputes `blocked_by` from issue state, so there
is nothing else to update. Under the `effort` review boundary a ticket closes when its commit is on the effort
branch and pushed, not when the effort's PR merges; that is deliberate, since waiting for the merge would keep
every dependent blocked for the whole run.

### Map body template

```markdown
## Destination

<what reaching the end of this map looks like — the spec, decision, or change this effort is finding its way to. One or two lines.>

## Notes

<domain; skills every session should consult; standing preferences for this effort>

## Decisions so far

<!-- the index — one line per closed ticket, linking the child where the detail lives -->

- [<closed ticket title>](link) — <one-line gist of the answer>

## Not yet specified

<!-- in-scope fog you can't ticket yet; graduates as the frontier advances -->

## Out of scope

<!-- work ruled beyond the destination; closed, never graduates -->
```

Open tickets are **not** listed in the body — they are the map's open sub-issues, found by query. The tracker
UI renders the sub-issue hierarchy, so no manual `## Children` list is needed.
