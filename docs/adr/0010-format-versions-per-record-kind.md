# Format versions are counted per record kind

Each of the three record kinds — defense, wave, replay — carries its own format version counter, rather than all three sharing one global number.

## Considered options

A single global counter was the obvious arrangement. It is wrong: editing the wave layout would bump every stored defense's version too, so every defense would look newer than it is, and readers would end up branching on versions that never changed anything about a defense.

## Consequences

Three counters, three histories, and each one only moves when its own bytes move.
