# Features and balance — 0.11.0

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
