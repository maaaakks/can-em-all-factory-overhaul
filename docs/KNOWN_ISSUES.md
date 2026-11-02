# Known issues and current limits

- Experimental alpha, primarily exercised on one Windows Steam installation. Fresh-machine, long-run and cross-mod testing are still needed.
- Only Steam build 26330345 / Unity 6000.2.15f1 and MelonLoader 0.7.3 are currently documented as tested.
- Widened grinders, collision seams, conveyor junctions and large body piles need more manual gameplay validation. Physics changes can interact with other mods.
- Night lighting is implemented but has not been validated across multiple displays, graphics settings or platforms.
- Core timings do not account for all Unity physics or GPU work. Continuous collision, stronger solver settings and extra lights can still have a cost at high object counts.
- UI strings are currently in French. English documentation does not imply English in-game localization.
- Several progression changes intentionally differ from vanilla: this is an overhaul, not a bug-fix-only package.
- The mod uses a separate save. Removing it resumes vanilla progress rather than transferring mod progress back.
- No end-user installer or public binary artifact is provided yet. Local builds embed a locally generated geometry snapshot.
- No compatibility guarantee is made for future game patches. Floor geometry generation intentionally checks the expected meshes and fails if the local layout is not recognized.

For a bug report, include the mod/game/loader versions, whether a save was imported, reproduction steps and a short sanitized loader log excerpt. Do not attach saves containing personal information to a public issue unless you intend to share them.
