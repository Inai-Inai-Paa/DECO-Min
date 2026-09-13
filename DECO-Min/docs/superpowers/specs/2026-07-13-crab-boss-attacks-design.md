# Crab Boss Attacks 2 and 3 Design

## Scope

Implement only the CrabBoss attack flow for Attack2 and Attack3. Damage, player reaction, hand weak-point behavior, and final combat integration can be completed later by teammates.

Attack2 and Attack3 should follow the existing Attack1 style: timed boss states, visible hand/boss movement, attack-area detection, and Debug.Log output for hit confirmation.

## Existing Context

CrabBoss already has CrabState.Attack2 and CrabState.Attack3 entries. Attack1 uses StateTimer and StateCountInState to split the attack into charge, active, and recovery phases.

Attack1 does not currently apply real player damage. Its collider logs when the player enters the attack area.

## Attack2 Flow

Attack2 locks the player's current position when the attack starts.

The boss moves both hands backward into a preparation pose for 0.4 seconds. Both hands use the charging material during this preparation.

After preparation, both hands thrust toward the previously locked player position. The original path from each hand's start position to its target position is the attack area.

The attack flow logs player hits along the thrust path, matching Attack1's current level of integration.

After the hands extend, they remain exposed for 2 seconds as a recovery window. This creates the intended opening for teammates to connect hand attackability later.

At the end of recovery, both hands return to their original local positions and normal material, then the boss returns to Rest.

## Attack3 Flow

Attack3 makes the boss jump up and stay airborne.

After 1 second in the air, the boss locks the player's current position as the landing target.

At 3 seconds total attack time, the boss lands at the locked target position and performs a circular area check.

The landing damage model is distance based for future integration: center damage is 100, outer-edge damage is 1, and damage scales down linearly by distance from the center. For this phase, the flow logs the calculated damage instead of requiring full player damage integration.

After landing, the boss returns to Rest.

## Attack Selection

Rest should no longer always start Attack1. It should choose among Attack1, Attack2, and Attack3 so the new attack flows can be tested.

The first version can use random selection with equal weight.

## Testing

Because the project currently has no visible automated Unity test setup for CrabBoss behavior, verification will focus on C# compilation readiness and static inspection.

Manual play-mode checks:

- When active, the boss eventually chooses Attack2 and Attack3.
- Attack2 hands prepare for 0.4 seconds, thrust to the locked player position, stay exposed for 2 seconds, and recover.
- Attack2 hit checks use the hand travel paths.
- Attack3 jumps, locks the player after 1 second, lands at 3 seconds, and logs distance-scaled landing damage.
- Attack1 still works as before.
