# Racing Minigame — "Rival Time Trial"

Beat the AI rival(s) over N laps. A continuous-control (SAC) showcase.

## Build it

1. `Tools → Self-Learning Enemies → Minigames → Build Racing Track Scene`
2. Press Play (no NavMesh needed — cars use Rigidbody physics).

`RacingTrackBuilder` generates an oval track with 20 waypoints (tagged `Waypoint`) and a
start/finish line. `MinigameManager.SetupRacing()` adds `TrackCheckpoint` triggers to each
waypoint and assigns them to every enemy's observation/reward components.

> **Important:** keep `buildArenaOnStart = true`. `RacingTrackBuilder.Waypoints` is not
> serialized, so the track must be built at runtime for the waypoint list to be populated.

## Composition (matches `CreateRacingDefaults()`: obs 36, discrete [3,3], continuous 3)

| Layer  | Component(s)                                                              |
|--------|---------------------------------------------------------------------------|
| Brain  | `EnemyBrain` + `GenreProfile(Racing)` + `BehaviorParameters` + `DecisionRequester` |
| Obs    | `ObsSelfTransform` (7) + `ObsWaypointProgress` 3 waypoints (9) + `ObsRaycastPerception` 4 rays (20) = 36 |
| Action | `ActionRigidBodyMovement` (3 cont) + two `ActionItemUsage` (`[3]` boost/item, `[3]` draft/block) |
| Reward | `RewardWaypointProgress`                                                  |

## Event wiring

- `TrackCheckpoint` (trigger) → `RewardWaypointProgress.RegisterWaypointReached(i)` + updates
  `ObsWaypointProgress.currentWaypointIndex` + reports to the manager for lap counting.
- Crossing the start/finish N times → `MinigameManager.EndGame()` declares the winner.

The player car uses `PlayerCarController`, which mirrors `ActionRigidBodyMovement`'s physics
exactly, so the human/AI comparison is fair.

## Play

WASD / arrow keys = steer + accelerate, Space = brake.

## Train

```bash
mlagents-learn racing_trainer_config.yaml --run-id=Racing_01
```

## Tuning

`ActionRigidBodyMovement` forces are untuned. Calibrate `motorForce`, `brakeForce`,
`maxSteerAngle`, and `maxSpeed` per vehicle on both the enemy and the player car.
