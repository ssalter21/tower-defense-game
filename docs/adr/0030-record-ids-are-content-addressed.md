# A record's id is the hash of its bytes, computed rather than stored

`RecordId` is a static function over bytes. No record carries its own id as a field.

## Considered options

Storing an id inside the thing it identifies creates two values that must agree and can therefore disagree, and it makes "this wave goes with this defense" a claim in a field rather than a fact about the bytes. Computed instead, the claim cannot be faked — not by a filename, not by an envelope, not by an editor.

## Consequences

The id is a function of bytes, not of a parsed record, and that difference bites exactly once: there is one writer and it emits only the current format (ADR-0012), so re-writing a record read from an older format version legitimately produces different bytes and therefore a different id. That is correct — they are different bytes.

Canonical array order (ADR-0017) is what makes content-addressing mean anything. Two identical defenses have identical bytes and therefore identical ids. Sorting on load instead of asserting order would have left identical defenses with different bytes, turning every id into a hash of somebody's typing order.

This is also why the seed cannot live in the defense record (ADR-0015).
