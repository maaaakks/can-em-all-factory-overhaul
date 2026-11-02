# First public binary release checklist

The source repository is public preparation, not a player-ready release.

- [ ] Validate installation and first launch on a second Windows machine.
- [ ] Test fresh vanilla import, existing mod saves, sale/restart, respec, quitting/reloading and removal.
- [ ] Exercise a crowded late-game day, nighttime and all conveyor junctions; review performance samples.
- [ ] Confirm locked covers, pit edges and player damage at every grinder-width tier.
- [ ] Provide geometry generation/loading through the local installation so the public package contains only mod-owned code/data, rather than a snapshot extracted from a development copy of the game.
- [ ] Add user-facing toggles for optional conveyor physics, nighttime lighting and diagnostics.
- [ ] Document exact supported game/loader versions and behavior after a game update.
- [ ] Prepare an installer or a clean manual-install ZIP and verify removal on a clean setup.
- [ ] Choose the project license and document any distributed dependency notices.
- [ ] Review English UI localization and capture current screenshots.
- [ ] Attach the tested binary/installer and release notes to a GitHub prerelease.

Player saves, test-point grants, local logs, development paths, game DLLs, IL2CPP reference assemblies and locally generated geometry must stay out of the repository/release package.
