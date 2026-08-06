# Record serialisation is hand-rolled, one field at a time

The record reader and writer are written by hand: a growing byte array, six little-endian primitives, and fields emitted in an order somebody chose.

## Considered options

A reflection serializer's output is a function of the type definitions it was pointed at, so renaming a field or reordering an enum silently changes what stored records mean years later. Written by hand, changing the byte order is a visible edit to this assembly that has to be paid for with a format version bump (ADR-0010).

## Consequences

`System.IO` is banned in the simulation assembly and the IL scan enforces it, so there is no `BinaryWriter` to reach for. That ban is why the writer is fifty lines rather than five, and why byte order is stated explicitly rather than inherited from the machine.

Width checks throw `SimulationException` rather than `RecordException`: a caller handing a seventy-thousandth tower to a `u16` count is a fault in this program, not in somebody's stored bytes. Widening a field is a format version bump, not a cast.
