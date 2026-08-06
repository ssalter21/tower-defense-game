# The dice are rolled exactly once per shot, for damage, and nowhere else

Everything in the match except shot damage is determined. The random stream is drawn from once per shot, so the stream's position is a running count of the shots fired so far, in order.

## Consequences

This turns unit ordering into something the state hash can see. If two runs order units differently — the desync that would otherwise go unnoticed — a different shot draws a different number and the state hash diverges on the tick it happened, rather than at the end of the match or not at all.

Adding a second consumer of the random stream would break that property, so any new use of randomness is a change to this decision.
