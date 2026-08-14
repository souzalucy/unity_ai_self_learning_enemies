# Minigames — Playable Experiments

The `SelfLearningEnemies` package is a **component library**, not a game. This folder adds the
missing "integration glue" that turns each genre into a playable minigame — a human player, a
game-loop manager, and the small scripts that wire gameplay events into the reward sources.

## What's here

```
Minigames/
├── Shared/                 Reused by all three genres
│   ├── MinigameSettings.cs   ScriptableObject: genre, experiment mode, difficulty, prefabs
│   ├── MinigameComposer.cs   Auto-builds a full player/enemy composition from settings
│   ├── MinigameManager.cs    Game loop: spawn, win/lose, waves/laps, resets, reward API
│   ├── MinigameHUD.cs        IMGUI overlay (HP, score, timer, AI reward + last action)
│   ├── PlayerController.cs   WASD + mouse aim + hitscan fire (RPG/Shooter)
│   ├── PlayerCarController.cs Steer/accel/brake car (Racing)
│   ├── PlayerStatus.cs       Player death -> "player lost"
│   ├── StatusDamageReceiver.cs ICombatTarget/IDamageable wrapper for SimpleStatusProvider
│   ├── EnemyWiring.cs        Enemy death -> ReportDeath, hit -> RegisterHit, tactical moves
│   └── ActionAimAndShoot.cs  NEW action effect: 2 continuous aim + raycast hit/miss (Shooter)
├── RPG/README.md
├── Shooter/README.md
└── Racing/
    ├── TrackCheckpoint.cs    Waypoint trigger -> reward + lap counting
    └── README.md
```

## The "experiment switch"

Each enemy carries `BehaviorParameters`. One dropdown in `MinigameSettings` (`experimentMode`)
controls everything:

| Mode           | What happens                                                    |
|----------------|-----------------------------------------------------------------|
| Heuristic Only | You drive an enemy with WASD / Space / E / Q                     |
| Training       | Run `mlagents-learn` and the policy learns live                 |
| Inference Only | Load a trained `.onnx` model (assign it in `MinigameSettings`)  |

## One-click scene generation

`Tools → Self-Learning Enemies → Minigames → Build {RPG|Shooter|Racing} Scene`

This creates a scene with an arena builder + `MinigameManager` + `MinigameHUD` + settings/profile
assets, all wired together. At Play, `MinigameManager` builds the arena and `MinigameComposer`
assembles the player and enemies from bare primitives — **no prefab wiring required**.

You can still use your own prefabs: assign them in `MinigameSettings.playerPrefab` /
`enemyPrefab`. The composer is idempotent, so it only fills in missing components.

## Required setup (once per project)

1. **Tags** — define `Player`, `Enemy`, `Cover`, `Waypoint` (already referenced by the framework).
2. **NavMesh** — bake a NavMesh for RPG/Shooter (`ActionNavMeshMovement` + player movement require it).
3. **ML-Agents** — install `com.unity.ml-agents` (Release 21).

## Training

Each genre reuses its existing YAML config:

```bash
mlagents-learn rpg_trainer_config.yaml     --run-id=RPG_01
mlagents-learn shooter_trainer_config.yaml --run-id=Shooter_01
mlagents-learn racing_trainer_config.yaml  --run-id=Racing_01
```

Set `experimentMode = Training`, press Play, then start `mlagents-learn` in the terminal.

## Recording GAIL demos

Set `experimentMode = Heuristic Only`, drive an enemy well, then use the existing
`DemoRecorderHelper` (add it to the enemy, or see `Training/gail_trainer_config.yaml`) to save
`.demo` files into `Training/Demos/`.

## Caveats

- Observation/action sizes are auto-set by `MinigameComposer` (it mirrors the editor's
  Auto-Configure). The totals match the `GenreProfile.Create*Defaults()` presets:
  RPG 28 obs / [6] / 2 cont, Shooter 48 obs / [5,4] / 4 cont, Racing 36 obs / [3,3] / 3 cont.
- Racing `ActionRigidBodyMovement` forces are untuned — calibrate `motorForce`/`maxSteerAngle`
  per vehicle.
- Racing requires `buildArenaOnStart = true` so `RacingTrackBuilder.Waypoints` is populated at
  runtime (it is not serialized).
