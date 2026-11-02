# Contributing

This is an experimental mod. Small, focused bug reports and changes are easiest to review.

- Keep player saves, logs with personal paths, binaries and generated game geometry out of commits.
- Run the core checks for balance, purchase or migration changes. Preserve exact refunds and migration idempotence.
- Do not reuse saved IDs for unrelated effects without an explicit migration.
- Distinguish build/check results from real in-game validation in reports and pull requests.
- For physics changes, describe the scene, object counts, relevant upgrades, reproduction steps and observed frame times.
- Source changes should use portable local-reference paths and keep the original game save separate.

An open-source license has not yet been selected; discuss contribution and redistribution terms with the maintainer before submitting substantial changes.
