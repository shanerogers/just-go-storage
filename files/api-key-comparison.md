# API key comparison

## Result

Both keys produced the same response shape for the club search path:

- `GET /clubs/search` returned `500 Internal Server Error`
- No club IDs were discovered
- No member samples could be collected

## Conclusion

At this point, the comparison is blocked by the upstream club search failure rather than by a visible access-level difference between the admin key and the single-club key.

## Next test

Use a known-working club ID from another path, then compare:

- `GET /clubs/{clubId}`
- `GET /members/search?ClubId={clubId}`

with each key.
