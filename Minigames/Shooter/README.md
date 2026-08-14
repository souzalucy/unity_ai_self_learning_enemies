# Shooter Minigame — "Cover Shootout"

Eliminate cover-using AI before the clock runs out. Enemies flank, retreat, and duck behind cover.

## Build it

1. `Tools → Self-Learning Enemies → Minigames → Build Shooter Arena Scene`
2. Bake a NavMesh.
3. Press Play.

`ShooterArenaBuilder` generates cover walls, barricades, and elevated platforms (all tagged
`Cover`).

## Composition (matches `CreateShooterDefaults()`: obs 48, discrete [5,4], continuous 4)

| Layer  | Component(s)                                                              |
|--------|---------------------------------------------------------------------------|
| Brain  | `EnemyBrain` + `GenreProfile(Shooter)` + `BehaviorParameters` + `DecisionRequester` |
| Obs    | `ObsRaycastPerception` 7 rays (35) + `ObsTargetTransform` (8) + `ObsSelfStatus` (5) = 48 |
| Action | `ActionNavMeshMovement` (2 cont) + `ActionAimAndShoot` (2 cont + `[5]`) + `ActionItemUsage` (`[4]`) |
| Reward | `RewardCombatPerformance` + `RewardSurvival` + `RewardCoverUsage`          |

`ActionAimAndShoot` is the one genuinely new framework component. It replaces `ActionCombat` for
ranged combat by adding raycast line-of-sight and direct `RegisterHit`/`RegisterMiss` reporting:

- 2 continuous: `aim_yaw`, `aim_pitch`
- 1 discrete `[5]`: `none, shoot, reload, grenade, melee`

The `[4]` tactical branch (`none, cover, flank, retreat`) is wired by `EnemyWiring` into real
NavMesh movement (find cover / flank around the threat / retreat).

## Event wiring

- Raycast hit → `RegisterHit(dmg)`; miss → `RegisterMiss()`
- `RewardCoverUsage` auto-detects cover between the enemy and the player
- Kill the player → `EnemyBrain.ReportObjectiveComplete()`

## Play

WASD move, mouse aim, LMB fire, E heal.

## Train

```bash
mlagents-learn shooter_trainer_config.yaml --run-id=Shooter_01
```

(PPO + ICM curiosity — good for the sparse reward of landing hits under cover.)
