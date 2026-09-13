# 0064 — A committed frame is documentation, not an oracle

**Decided.** The rendered pictures under `docs/frames/` and `docs/chrome/` are for a person to look at a named
tick without opening the editor. Nothing compares them to anything and nothing fails if they change. What
catches a broken view is the assertions in `Tests.PlayMode/MatchViewTests` and the sit-down landmark table.

- **Drawn through the real thing.** The real `MatchRoot`, hex floor, `OrbitCameraRig` and `MatchView` stepping
  the real simulation, out of `content/match.replay` — the same bytes the shell replays and the player plays. A
  tick in a filename is only worth anything because it is a tick of the run `content/landmarks.txt` was made
  from.
- **The capture draws every tick it steps through**, not only the ones it keeps: where a tower points and where
  its shot leaves from are read off the pose it was last drawn in, so a sparse tick list photographs a match
  whose towers never moved and puts a muzzle flash on a bind pose.
- **A frame is a function of its tick and of the tick list it was asked for**, measured twice: `-Ticks
  "342,344"` and `-Ticks "311,342,344"` give different bytes for 0342 and 0344. So a fixture is re-captured
  with its whole recipe, and a frame is expected to move if it is not.
- **A capture writes `rendered-from.txt` beside its pictures** — a digest over the four authored content files
  as they stood — and never by hand. A re-capture that comes out pixel-identical leaves git nothing to date, so
  `check-docs.ps1` asks the record where the date says a picture is older than the content
  (`tools/_rendered-from.ps1` is the one list both read).
- **A sheet drawn from a set file is exempt from that dating**, by name, one file at a time: it reads models,
  props, atlases and one pose and none of the authored files, so moving a price renders the same pixels. What
  would make it stale is a look in the roster moving, which is a person's job to notice.
- **A candidate look is a file and never an edit to `MatchTuning`.** `-Effects` plays the recorded match with
  one candidate on for the length of the run, and nothing the game ships from moves.
- **What the capture writes and nobody keeps is ignored by construction** (`docs/frames/.gitignore`, one
  negation per committed picture), per rule 4 of `AGENTS.md`.

**Cost.** A stale picture goes on looking entirely reasonable — the overtake tick has moved twice — and only a
date and a record notice, never the pixels. Screen captures of a played run have no capture to re-run and are
exempt outright; nothing notices when those go stale, and their README says so.

**Rejected.** A screenshot comparison: two frames whose bones were definitively swapped rendered
pixel-identical, reproducibly, so it would be a check that cannot fail — the species this project keeps
deleting. A capture path that builds its own approximation of the scene: a picture of something this project
does not ship. Drawing only the kept ticks: see above. Exempting sheets by a filename pattern: it would exempt
the next sheet dropped into the directory as well.

The first capture found the hitscan towers lying on their side on the road with every test green — the view was
forcing each model's root rotation to identity, and the characters' roots happened to be identity. That is the
argument for the pictures existing, and why the fix shipped with `EveryModelStandsTheWayItWasImported` rather
than only with a picture.
