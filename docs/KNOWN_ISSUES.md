# Known issues and current limits

- Experimental alpha, primarily exercised on one Windows Steam installation. Fresh-machine, long-run and cross-mod testing are still needed.
- Only Steam build 26330345 / Unity 6000.2.15f1 and MelonLoader 0.7.3 are currently documented as tested.
- Widened grinders, collision seams, conveyor junctions and large body piles need more manual gameplay validation. Physics changes can interact with other mods.
- Night lighting is implemented but has not been validated across multiple displays, graphics settings or platforms.
- Core timings do not account for all Unity physics or GPU work. Continuous collision, stronger solver settings and extra lights can still have a cost at high object counts.
- The custom upgrade interface is in English as of 0.12.0. The new delivery-control labels are currently French. Screenshot/layout review is still needed, especially for long upgrade names at lower resolutions.
- The delivery button's position is based on the native gate geometry. Its rendering, targeting, pause/resume behavior and interaction with day transitions still need manual gameplay validation. Pure iterator-boundary checks do not validate Unity coroutines in a running game.
- Prestige retention after the ending has regression coverage for native save-identity changes; the complete ending/continuation flow still needs a new manual gameplay check.
- Several progression changes intentionally differ from vanilla: this is an overhaul, not a bug-fix-only package.
- The mod uses a separate save. Removing it resumes vanilla progress rather than transferring mod progress back.
- No end-user installer or public binary artifact is provided yet. Local builds embed a locally generated geometry snapshot.
- No compatibility guarantee is made for future game patches. Floor geometry generation intentionally checks the expected meshes and fails if the local layout is not recognized.

For a bug report, include the mod/game/loader versions, whether a save was imported, reproduction steps and a short sanitized loader log excerpt. Do not attach saves containing personal information to a public issue unless you intend to share them.
