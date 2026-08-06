# One match surface, every scenario — no modes, flags or branches

`Match` is constructed from `(map, layout, wave, seed)`, advanced as many ticks as the caller likes, and asked for a snapshot and a result. Every scenario is that same surface used differently: normal playback advances one tick and pulls a snapshot each time, fast-forward advances more ticks per call, a seek is a fresh match advanced to the tick asked for, instant-resolve is one call with a large number and nobody pulling anything, and the headless command line and parity run are the same again with a hash trace read off.

None of those is a mode, a flag, or a branch. If any of them needed its own code path this surface would be wrong, which is a claim the tests check.
