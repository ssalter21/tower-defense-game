# Canonical order is asserted at load, never restored

Stored arrays have a required order — towers ascend by row then column, wave entries ascend by tick then type. The loader asserts that order and throws when it does not hold. It never sorts the array into place.

## Consequences

Sorting on read would hide the bug that produced the out-of-order record and make two records with the same bytes in a different order load as the same thing, which breaks the property that a record's id is the hash of its bytes.

Throwing means a writer that emits out of order is found immediately, by the first reader, rather than by a desync much later.
