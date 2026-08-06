# Draw order is by construction, not by sorting

Everything on the playfield is opaque geometry in a depth buffer, and each entity's view object is bound to its id for as long as that id keeps appearing.

## Consequences

Two creeps overtaking swap places in the world and never swap objects. Nothing re-sorts per frame, so there is nothing to flicker.
