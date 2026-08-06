# Reading a record is all-or-nothing

There is no partial read, no best-effort read, and no skipping a field the reader does not recognise. Anything unexpected — wrong magic, an unknown format version, a truncation, an array out of canonical order, a bundle that contradicts itself — throws.

## Consequences

The record format has fixed-width fields with no length prefixes to skip by. A reader that tolerated something it did not understand would not be skipping an unknown field; it would be reading the next field at the wrong offset and returning a defense made of noise that still validates.

Every read is bounds-checked and every failure names the field it failed on, so the error says which gate failed and both values rather than just that something went wrong.
