# A replay bundle is self-contained, and the seed lives in it

A replay bundle carries the seed, the map inlined, the defense and the wave — everything needed to re-run the match, with no registry and no assumption about where anything lives. The two tables it does not inline, the unit types and the ruleset, it pins by content hash ([0047](0047-a-bundle-stamps-its-ruleset.md)), so neither can be substituted without the replay gate saying so.

## Considered options

A bundle that named its map by id would only replay on a machine that already had that map, under that id, with those exact contents — three assumptions, each of which is somebody else's job to keep true. Inlining the parsed grid costs about 135 bytes and makes handing somebody a replay a matter of handing them the bytes.

This is the wrong trade at pool scale, which is why the defense and the wave keep their own ids and can be stored separately. It is the right trade for a replay.

## Consequences

The seed can live nowhere else. A record's id is the hash of its bytes, so a seed inside the defense would make rolling different dice a *different defense* — orphaning every replay that pointed at the old one, and destroying the property that makes one defense runnable under ten seeds.

The header appears three times and the loader checks all three agree. Those 36 spare bytes buy the guarantee that a wave from one ruleset cannot be stapled to a defense from another. A bundle whose copies disagree is a hard read error rather than a replay refusal, because it is not an old record — it is a record that contradicts itself.
