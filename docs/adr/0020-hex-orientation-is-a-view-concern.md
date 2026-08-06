# Hex orientation is a view concern and enters in exactly one file

The simulation converts the authored character grid to axial `(q, r)` through odd-r offset and stops. It has no idea whether a hex is pointy-top, flat-top or drawn as a square, because none of that changes what happens in a match.

Pointy-top, two metres across the flats, is decided in `HexGeometry` and nowhere else, so nothing downstream is free to disagree.

## Consequences

Only the width is a typed constant. The circumradius (`AcrossFlats / sqrt(3)`) and the row pitch (`1.5 × circumradius`, which works out to `sqrt(3)`) follow from it arithmetically rather than being three independent numbers that could drift apart.

Odd-r offset to axial is the simulation's canonical conversion; rows are odd-r, so odd-numbered rows sit half a cell to the right.
