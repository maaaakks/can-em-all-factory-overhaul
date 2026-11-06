# Features and balance - 0.14.4

## Permanent upgrade table

There are 20 upgrades, with a combined maximum cost of **23,400 prestige points**. Prices and benefits are defined in `src/PermanentUpgradesRevamped/Balance.cs`.

| Group | Upgrade | Ranks | Maximum permanent effect |
| --- | --- | ---: | --- |
| Profitability | Starting Cash | 20 | $1,000,000 after a factory sale/restart |
| Profitability | Can Value | 20 | ×10 baseline contribution |
| Profitability | Shiny Probability | 20 | +10 percentage points |
| Profitability | Shiny Value | 10 | ×2 permanent shiny value |
| Grinders | Grinder Width | 6 | ×2.25 opening width |
| Grinders | Processing Cadence | 20 | ×5 processing cadence |
| Grinders | Suction Range | 10 | 26 m |
| Grinders | Suction Power | 10 | 3 m/s target speed |
| Player | Damage | 20 | ×8 baseline contribution |
| Player | Movement | 12 | +60% |
| Player | Vacuum Storage | 20 | ×6 baseline storage contribution |
| Player | Vacuum Firing Cadence | 10 | ×2 ejection cadence |
| Player | Player Ammunition | 20 | +200% of base capacity |
| Player | Critical Chance | 10 | +10 percentage points |
| Special Bonuses | Shiny Shipment | 1 | Native special shipment effect |
| Special Bonuses | Damaged Delivery | 10 | −20% maximum HP on new arrivals |
| Special Bonuses | Opening Shockwave | 3 | 50% maximum HP damage to targets reached by the first-delivery wave |
| Special Bonuses | First Blood | 1 | Native first ten hits are lethal effect |
| Special Bonuses | Death Touch | 5 | +1 percentage point instant-death chance |
| Special Bonuses | First Delivery Reinforcements | 10 | +50 monsters on the first delivery of each day |

### How bonuses combine

The native game uses additive fields for several upgrades. A listed permanent ×10 can value contributes +9 to the native value multiplier. With all run can-value upgrades (+9), the final multiplier is **×19**, not ×100. Player damage and player ammunition also add their permanent base contribution to the run bonuses.

The maximum can-value permanent costs **2,100 points**. Ranks 0–10 retain the original overhaul curve, reaching ×2 at rank 10. Ranks 11–20 progress through ×2.8, ×3.6, ×4.4, ×5.2, ×6, ×6.8, ×7.6, ×8.4, ×9.2 and ×10.

Random probabilities are capped at **40% shiny**, **50% critical** and **5% instant death**, including run bonuses. Guaranteed Shiny Shipment and First Blood effects remain separate. The shiny chance upgrade affects truck creature rolls; the run tree also contains a separate shiny-can probability.

Suction range I unlocks a 4 m field at a base target speed of 0.3 m/s. Power I raises the target speed to 0.45 m/s and requires range I. Acceleration is progressive; bodies retain gravity and physical collisions. Living creatures, held bodies and bodies already in free fall are excluded from pulling.

First Delivery Reinforcements adds **+5 monsters per rank**, from +5 to +50. Prices are 25, 50, 75, 100, 125, 150, 175, 200, 225 and 250 prestige points (1,375 total). It stacks with the normal run-tree delivery size. The truck count and ordinary cap are restored after spawning, so later deliveries remain unchanged. Existing permanent ranks are retained and the new upgrade starts unpurchased when migrating to save schema 6.

## Run tree extensions

| Upgrade | Result | Run-money price |
| --- | --- | ---: |
| Turret III | 3 turrets total | $35,000,000 |
| Turret IV | 4 turrets total | $100,000,000 |
| Reserve V | ×7 base turret reserve | $20,000,000 |
| Reserve VI | ×10 base turret reserve | $40,000,000 |
| Reserve VII | ×15 base turret reserve | $80,000,000 |
| Reserve VIII | ×25 base turret reserve | $160,000,000 |

The five processing machines remain normal run unlocks. Permanent grinder width and suction do not unlock those machines automatically. Extra turret positions complement the two native central turrets.

**Buy Maximum** is a greedy, prerequisite-aware batch purchase: buy the cheapest affordable available upgrade, unlock its descendants, and repeat. The preview is recalculated from the current run state. It is not a background auto-spender and it does not buy prestige upgrades.

## Physics and visual changes

- Reconstructed floor visuals/colliders, wider pit geometry and rectangular capture volumes.
- Solid, resized covers for locked machines; supports disappear with the native cover when unlocked.
- Player damage capture below the walking surface with a foot-position check.
- Stable can conveyor guidance with material/collision settings restored on pickup, throw, pooling or leaving the belt.
- Reduced hit knockback, including shockwaves, without lowering damage or changing manual throw methods.
- Blue nighttime lighting and restored daytime ambient settings.
- Native-style hover/click feedback, four upgrade columns, rank bars and contextual descriptions.

## Retired or replaced mechanics

- Sweeper robots: retired; applicable native and old mod purchases refunded in run money.
- Old run-tree grinder-width/suction nodes: moved to prestige progression and refunded in run money.
- Permanent stamina economy: removed because the run tree already supplies stamina progression; old purchases refunded in prestige points and replaced with vacuum cadence.
- Cold Storage: disabled because the carry-over behavior was unreliable.

## Diagnostics

Every 30 seconds of active gameplay, loader logs report average/P95 frame times, long frames, mod section timings, managed allocations, actual sales, guided/floor cans and production queues. These measurements help distinguish payout balance from transport/production bottlenecks. They do not establish a controlled vanilla/mod FPS comparison.

## Scene lifecycle and language

All custom upgrade names, descriptions, tooltips and shop buttons are in English. The new physical delivery control currently uses French status labels and prompts. Permanent numerical labels use invariant English formatting even on French Windows. Prices, progression tables, saved identifiers and schema 6 are unchanged.

The factory extension clears its injected-node and turret registries for each new skill tree. Blue lighting follows the current lighting controller and ignores destroyed lights. Grinder, suction, conveyor and UI caches are rebuilt for the new scene rather than retaining destroyed Unity objects. These fixes target the duplicate-skill and destroyed-light errors seen after restarting a game. Compilation and core checks do not replace manual new-game/end-of-run testing.

## Shop before the first day

Starting or continuing a game opens the native run shop before the first day begins. Selling the factory returns to the run shop after the native reset and starting-cash grant. Use Buy Maximum or buy individual upgrades, access Permanent Upgrades with P, then press NEXT DAY or Space to begin. Preparing purchases does not advance the saved day, run the day timer, or trigger the first delivery. Space does not start the day while the prestige panel or pause menu is open.

## Industrial suction and screenshots

Industrial suction is a **Permanent Upgrades > Grinders** feature, upgraded through Suction Range and Suction Power. Grinder Width is in the same permanent group. Maximum-width openings are x2.25 at rank 6/6.

Original-resolution screenshots are available in the [README](../README.md): the Permanent Upgrades menu, maximum-width grinders empty and processing bodies, blue night lighting, and optimized conveyors.

## Save-scoped prestige

Permanent ranks are stored in the active mod save. Continue restores that save's ranks. New Game creates a fresh save with zero permanent ranks and native starting points; it first creates a timestamped `.before-new-game-*` backup of the previous mod save. Grinder width returns to its baseline and suction remains inactive until purchased in the new save. Selling the factory is prestige within the same save and preserves permanent purchases. Schema 6 and all balance tables remain unchanged.

Returning from the ending can replace the native SaveData object without loading another save. This replacement now preserves allocated ranks instead of treating it as an implicit New Game.

## Flush grinder rim collisions

The native grinder collision rim rises approximately 5.6 cm above the walking floor. Version 0.14.4 lowers only the above-floor collision vertices to floor level on all five grinders, allowing horizontal suction to cross the lip. The original lower walls, triangle indices, horizontal opening shape and width-upgrade transforms remain intact. This changes collision geometry, not the existing rim artwork. No extra ramps, floors over pits, teleportation or upward body impulses are added. Geometry is read from the locally generated snapshot because the native mesh is not CPU-readable; regenerate old snapshots with the current exporter.

## Delivery pause button

A button inside the native truck entrance toggles additional deliveries. Aim directly at it within 2.7 m and press E. Green permits all deliveries; red allows the first delivery of each day and pauses subsequent visits. A loaded or returning truck finishes its current visit. The native iterator then waits at the completed-return boundary, preserving first-delivery bonuses and local state. Press E again to resume the same iterator. A fresh daily iterator always starts its first delivery, without resetting the red toggle.

The toggle is session-only and remains paused across days. Reloading the scene or explicitly starting a New Game enables deliveries again. It does not alter saved progress, prices, day timers or prestige.
