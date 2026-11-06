# Changelog

## 0.14.4 - Flush grinder collision lips

- Remove the approximately 5.6 cm collision step at each grinder rim so horizontally pulled bodies can enter without climbing a ledge.
- Lower only raised rim vertices to the floor level. Preserve lower pit walls, triangle topology, horizontal opening dimensions, collision materials and purchased width scaling.
- Extend the local geometry exporter with the shared native rim collision snapshot; the game mesh is not CPU-readable. Generated geometry remains ignored and must be regenerated when upgrading.
- Validate five native rims across all seven widths (5,215 vertex/width probes), plus geometry-rule regression checks. Runtime body passage remains a manual gameplay check.

## 0.14.3 - Guarantee the first daily delivery

- Always allow a fresh daily TruckUpdate to start, even when the delivery toggle remains paused from the previous day.
- Pause only at the completed return boundary, so the first daily delivery and its permanent bonuses remain available while subsequent visits stay blocked.
- Label the red state as one delivery per day and clarify the resume prompt.
- Add a hundred-day regression sequence without clearing the pause setting. No save or balance changes.

## 0.14.2 - Delivery pause control

- Add a physical pause/resume button inside the native truck entrance, operated by aiming and pressing E. Green permits deliveries; red suspends future visits.
- Let an active delivery finish unloading and returning, then hold its existing coroutine at the visit boundary. Resume without restarting the coroutine or changing first-delivery bookkeeping.
- Keep the toggle for the current session, across days; reset it on scene reload or explicit New Game. No save schema or balance changes.
- Add dispatch-boundary checks and retain the ending-prestige regression coverage. Compilation and pure checks pass; button input/rendering and native coroutine behavior require manual gameplay validation.
- Delivery-button labels are currently French; the existing upgrade interface remains English.

## 0.14.1 - Preserve prestige after the ending

- Fix allocated permanent ranks being erased when the native game replaces SaveData after the ending scene.
- Rebind the new native identity without clearing ranks. Explicit New Game still clears them; Continue restores the loaded save's own ranks.
- Add repeated native-identity replacement checks. Save schema 6 and prices remain unchanged.

## 0.14.0 - Save-scoped permanent progress

- Fix New Game retaining the previous save's permanent ranks in memory.
- Bind restored ranks to the native SaveData object and reset ranks on an unannounced object replacement.
- Back up the previous mod save before New Game, remove old applied bonuses, restore base grinder width and disable suction for a fresh save.
- Preserve permanent ranks when loading/continuing a save and when selling the factory.
- Add save ownership and New Game regression checks; keep save schema 6.

## 0.13.0 - Shop-first starts and screenshot gallery

- Open the run shop before starting the native day loop; NEXT DAY or Space begins the day after preparation.
- Return to the run shop after selling the factory, preserving the native reset and starting-cash grant.
- Document industrial suction as a permanent grinder upgrade.
- Add five original-resolution gameplay screenshots and replace em dashes with simple hyphens.

## 0.12.0 - Scene reload fixes and English interface

- Reset injected skill/turret registries per tree, fixing duplicate identity checks after a scene reload.
- Recreate blue lighting for each lighting controller and guard destroyed lights.
- Release grinder geometry, suction, conveyor, UI and applied-bonus caches when their factory scene changes.
- Translate all custom permanent/run names, descriptions, tooltips, status labels and shop buttons into English.
- Preserve balance, prices, saved IDs and schema 6. Add English number/status regression checks.

## 0.11.0 - First delivery reinforcements

- Add a Special Bonuses permanent: +5 monsters per rank on the first delivery of each day, up to +50.
- Ten ranks cost 25 to 250 points (1,375 total). Later deliveries retain their normal size.
- Save schema 6 preserves existing purchases and appends the new rank at zero.
- Add rank, daily eligibility and migration regression checks.

## 0.10.2 - Current experimental source snapshot

- Raise permanent can value to ×10 at rank 20, keeping the 2,100-point total and existing ranks. Full run can-value upgrades bring the final native multiplier to ×19.
- Include the earlier performance cache improvements and 30-second performance/economy samples.
- Include vacuum cadence and player ammunition prestige branches, with exact refund of retired permanent stamina purchases.

## Earlier development milestones

- **0.10.0–0.10.1:** 19-row permanent UI, vacuum cadence, player ammunition, save schema 5, hot-path cache improvements and runtime diagnostics.
- **0.9.0:** synchronized locked grinder covers/supports, safer player capture bounds, conveyor guidance, reduced hit knockback and blue night lighting.
- **0.8.0–0.8.1:** shop batch purchases, then full-budget cheapest-accessible purchasing and shop visibility fixes.
- **0.7.0:** one-screen grouped prestige progression; move grinder width and suction to permanent upgrades; rebalance and refund old purchases.
- **0.6.0–0.6.1:** retire sweepers and introduce dead-body suction with congestion-aware routing; reconcile native and added turrets.
- **0.4.0–0.5.0:** extend turret/ammunition branches and implement wider grinder geometry/floor collision changes.
- **0.1.0–0.3.0:** initial permanent-upgrade overhaul, separate mod save, native-style UI, special bonuses, refunds and removal of Cold Storage.

Earlier versions were local development iterations, not public releases. This repository does not include the development saves or test-point changes.
