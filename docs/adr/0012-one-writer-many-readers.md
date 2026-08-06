# One writer, many readers: version history lives in the reader

The writer emits the current format version and nothing else, ever. Every historical version is handled in the reader, as one branch per version.

## Consequences

The reader's history is a list that grows by one each time the format moves. If the writer carried history too, the writer/reader pairs would multiply instead.

A version branch never goes away — an old branch sits beside the current one permanently, because records written under it still exist.
