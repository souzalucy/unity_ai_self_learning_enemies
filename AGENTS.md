# AGENTS.md — Self-Learning Enemies for Unity

> **For AI coding agents and developers working on this project.**
> This file explains the architecture, conventions, dependencies, and next steps.

---

## Project Overview

**Self-Learning Enemies** is a plug-and-play reinforcement learning framework for Unity that creates intelligent enemy AI across multiple game genres (RPG, Shooter, Racing). It wraps Unity's official **ML-Agents** toolkit (PyTorch-based PPO/SAC) behind a composable component architecture.

**Goal:** Drop this into any Unity project, snap a few components onto a prefab, run one CLI command, and get a trained neural network driving smart enemy behavior — with zero RL code from the user.

**Status:** Core framework complete. Ready for integration testing inside a real Unity project with ML-Agents.

---

## Architecture Deep Dive

### The 4-Layer Abstraction

```
EnemyBrain (Agent)
  ├── ObservationSource[]   →  CollectObservations(VectorSensor)
  ├── ActionEffect[]         →  ApplyActions(float[] discrete, float[] continuous)
  └── RewardSource[]         →  CalculateReward() → float
```

**Discovery:** `EnemyBrain.Awake()` calls `GetComponents<T>()` for each layer. No manual wiring.

**Action Mapping:** `ConfigureActionSpace()` concatenates all `DiscreteBranchSizes` and sums `ContinuousActionCount`, then stores offset arrays so it can slice the global action buffer back into per-effect slices at dispatch time.

**Observation Ordering:** Sources iterated in sibling index order. Must be deterministic. Size mismatches caught and logged.

### GenreProfile System

`GenreProfile` is a `ScriptableObject` preset — it doesn't drive runtime behavior directly:
- Pre-configures `BehaviorParameters` observation/action sizes
- Provides `survivalRewardPerSecond`, `deathPenalty`, `objectiveCompleteReward` defaults
- References the correct training YAML path

Default profiles created via `GenreProfile.CreateRPGDefaults()` (etc.) — factory methods called by `EnemyBrainEditor` menu items.

### Training Pipeline

```
Unity (C#)                          Python (mlagents-learn)
──────────                          ────────────────────────
EnemyBrain.OnActionReceived()  ──→  PPO/SAC policy update
  ↓                                   ↓
CollectObservations()           ←──  New action selection
  ↓
ApplyActions() + CalculateReward()
```

Communication via gRPC over localhost (transparently handled by ML-Agents).

---

## File Map

### Core (`Core/`)
| File | Role |
|------|------|
| `GenreProfile.cs` | ScriptableObject with enum, presets, factory methods |
| `ObservationSource.cs` / `ActionEffect.cs` / `RewardSource.cs` | Abstract bases for the 4-layer abstraction |
| `EnemyBrain.cs` (+ partials: `StepLogic`, `ActionMapping`, `ComponentDiscovery`, `Heuristic`, `Telemetry`) | Main `Agent`: lifecycle, public API, action mapping, discovery, heuristic controls |
| `IStatusProvider.cs` / `ITargetProvider.cs` | Decoupled status/target interfaces + `SimpleStatusProvider` / `SimpleTargetProvider` |
| `SoundEventManager.cs` | Global sound event system (`Emit`, `GetRecentSounds`) |
| `CurriculumManager.cs` | Lesson-based difficulty progression + `GetParameter` |
| `SquadBrain.cs` | Multi-agent group coordination (`SimpleMultiAgentGroup`) |
| `DemoRecorderHelper.cs` | GAIL demo recording wrapper |
| `DebugGizmos.cs` | Scene view visualization overlay |
| `TrainingArenaBuilder.cs` + `RPGArenaBuilder` / `ShooterArenaBuilder` / `RacingTrackBuilder` | Procedural arena builders |
| `BT/` | Behavior Tree nodes (`BTNode`, `BTComposites`, `BTLeafs`) |

### Observations / Actions / Rewards
| Directory | Files |
|-----------|-------|
| `Observations/` (8) | `ObsSelfTransform`, `ObsTargetTransform`, `ObsSelfStatus`, `ObsRaycastPerception`, `ObsGridSensor`, `ObsSoundPerception`, `ObsWaypointProgress`, `ObsBehaviorTreeSuggestions` |
| `Actions/` (5) | `ActionNavMeshMovement`, `ActionRigidBodyMovement`, `ActionCombat`, `ActionItemUsage`, `ActionBehaviorTree` |
| `Rewards/` (5) | `RewardCombatPerformance`, `RewardSurvival`, `RewardDistanceManagement`, `RewardWaypointProgress`, `RewardCoverUsage` |

### Editor, Training, Config
| File | Role |
|------|------|
| `Editor/EnemyBrainEditor.cs` | Custom inspector, live stats, Auto-Configure, profile creator |
| `Editor/MinigameSceneBuilder.cs` | One-click scene generation |
| `Editor/Tests/` (6 files, 61 tests) | Editor unit tests |
| `Training/*.yaml` (4) | PPO / SAC / GAIL trainer configs |
| `SelfLearningEnemies.asmdef` + `Editor/*.asmdef` | Assembly definitions |
| `.quality-gate.yml` + `.editorconfig` | AI code-quality gate thresholds + C# style rules |

**Total: 69 `.cs` files (~6,700 lines) — 24 Core, 5 Actions, 8 Observations, 5 Rewards, 8 Editor (incl. 6 tests), 19 Minigames — plus 4 `.yaml`, 13 `.md`, 3 `.asmdef`, and config.**

---

## Minigames (Playable Integration Layer)

`Minigames/` turns the component library into three playable experiments. It is the "integration
glue" the framework deliberately leaves to you — a player controller, a game-loop manager, and
scripts that wire gameplay events into the reward sources.

| File | Role |
|------|------|
| `Minigames/Shared/MinigameSettings.cs` | ScriptableObject: genre, experiment mode, difficulty, prefabs |
| `Minigames/Shared/MinigameComposer.cs` | Public entry points (`ConfigurePlayer`/`ConfigureEnemy`) — orchestrates the genre composers |
| `Minigames/Composers/` (5) | `ComposerUtils` (`Ensure<T>`), `BehaviorConfigurator` (BP sizing + experiment mode), `RpgComposer`, `ShooterComposer`, `RacingComposer` |
| `Minigames/Shared/MinigameManager.cs` | Game loop: spawn, win/lose, waves/laps, resets, reward API (delegates state to `RoundStateMachine`) |
| `Minigames/Shared/RoundStateMachine.cs` | Serializable win/lose state: score, wave, timer, game-over flag |
| `Minigames/Shared/EnemySpawner.cs` | Stateless factory for spawning the player + enemies |
| `Minigames/Racing/RacingRoundController.cs` | Lap/checkpoint bookkeeping for racing |
| `Minigames/Shared/MinigameHUD.cs` | IMGUI overlay (HP, score, timer, AI reward + last action) |
| `Minigames/Shared/PlayerController.cs` | WASD + mouse aim + hitscan fire (RPG/Shooter) |
| `Minigames/Shared/PlayerCarController.cs` | Steer/accel/brake car (Racing) |
| `Minigames/Shared/PlayerStatus.cs` | Player death → "player lost" |
| `Minigames/Shared/StatusDamageReceiver.cs` | `ICombatTarget`/`IDamageable` wrapper for `SimpleStatusProvider` |
| `Minigames/Shared/EnemyWiring.cs` | Death→`ReportDeath`, hit→`RegisterHit`, tactical moves |
| `Minigames/Shared/ActionAimAndShoot.cs` | New `ActionEffect`: 2 continuous aim + raycast hit/miss (Shooter) |
| `Minigames/Racing/TrackCheckpoint.cs` | Waypoint trigger → reward + lap counting |
| `Editor/MinigameSceneBuilder.cs` | One-click scene generation (`Tools → … → Minigames`) |

Also included: `Core/EnemyBrain.Telemetry.cs` (read-only reward/step/last-action accessors) and a
`SimpleStatusProvider.Configure()` method for sizing stats from settings.


## Dependencies

### Required (Unity)
- **Unity 2021.3+**
- **Unity ML-Agents** package (Release 21, `com.unity.ml-agents`)
- **Unity NavMesh** (built-in, for `ActionNavMeshMovement`)

### Required (Python — training only)
- **Python 3.8–3.11**
- **mlagents**: `pip install mlagents`
- **PyTorch** (auto-installed with mlagents)

### Optional
- `ActionRigidBodyMovement` uses `WheelCollider` for racing; falls back to Rigidbody forces

---

## Conventions

### Naming
- **Abstract bases:** `ObservationSource`, `ActionEffect`, `RewardSource`
- **Concrete:** `Obs*` for observations, `Action*` for actions, `Reward*` for rewards
- **Namespace:** `SelfLearningEnemies` (runtime), `SelfLearningEnemies.Editor` (editor)

### Code Patterns
- `[Tooltip]` on all serialized fields
- `XmlDoc` on public API methods
- Per-step accumulators reset in `CalculateReward()` and `OnEpisodeBegin()`
- `OnEpisodeBegin()` called on all components by `EnemyBrain`
- Optional deps use `TryGetComponent<T>()`

### API Surface for Game Integration
- `EnemyBrain.ReportObjectiveComplete()` — enemy achieved goal
- `EnemyBrain.ReportDeath()` — enemy died
- `EnemyBrain.ValidateSetup()` — debug diagnostics
- `RewardCombatPerformance.RegisterHit/Miss/Kill()` — from damage pipeline
- `RewardWaypointProgress.RegisterWaypointReached()` — from checkpoint system
- `RewardSurvival.ReportDeath/ReportEpisodeComplete()` — lifecycle
- `ICombatTarget.TakeDamage()` — implement on damageable objects
- `ActionCombat.OnAttackExecuted` — UnityEvent for VFX/animation
- `ActionItemUsage.OnItemUsed` — UnityEvent for item effects

---

## Known Limitations

1. **ML-Agents must be installed** — `asmdef` references `Unity.ML-Agents` by name
2. **BehaviorParameters must be kept in sync** — the editor's **Auto-Configure** button and the runtime `BehaviorConfigurator` both set sizes, but manual Inspector edits can still drift them
3. **No built-in curriculum loader** — YAML configs have commented-out blocks
4. **Racing forces are untuned** — motor/brake/turn values need per-vehicle calibration
5. **`ObsSelfStatus` uses public fields** — game health system must update them each frame
6. **Squad coordination is opt-in** — `SquadBrain` provides shared rewards, but each `EnemyBrain` still trains independently without it
7. **Heuristic mode limited** — 3 discrete + 2 continuous actions hardcoded
8. **No `OnValidate` auto-sync** — manual button in editor instead

---

## Next Steps

### Immediate — Integration Validation ✅
- [x] Playable minigames for all three genres — `Minigames/` (RPG "Arena Brawl", Shooter "Cover Shootout", Racing "Rival Time Trial")
- [x] Player controller, game-loop manager, HUD, and reward-wiring glue — `Minigames/Shared/`
- [x] One-click scene generation — `Tools → Self-Learning Enemies → Minigames`
- [x] Test heuristic mode with WASD/Space — switch `MinigameSettings.experimentMode` to Heuristic Only
- [ ] Install into a real Unity project with ML-Agents and run `mlagents-learn rpg_trainer_config.yaml --run-id=test` to verify training end-to-end

### High Priority ✅
- [x] `ObsGridSensor` — `Observations/ObsGridSensor.cs` (Physics.OverlapBox grid, tag one-hot per cell)
- [x] `ObsSoundPerception` — `Observations/ObsSoundPerception.cs` + `Core/SoundEventManager.cs` (global sound event system)
- [x] `ActionBehaviorTree` — `Actions/ActionBehaviorTree.cs` + `Observations/ObsBehaviorTreeSuggestions.cs` + `Core/BT/` (hybrid BT + learned actions, FollowBT/OverrideBT branching)
- [x] `CurriculumManager` — `Core/CurriculumManager.cs` (lesson thresholds, rolling reward tracking, `GetParameter` lesson value exposure)
- [x] GAIL imitation learning — `Training/gail_trainer_config.yaml` + `Core/DemoRecorderHelper.cs` (demo recording + GAIL reward signal config)

### Medium Priority ✅
- [x] `IStatusProvider` / `ITargetProvider` — `Core/IStatusProvider.cs` + `Core/ITargetProvider.cs` + default impls (SimpleStatusProvider, SimpleTargetProvider)
- [x] Auto-configure BehaviorParameters — `Editor/EnemyBrainEditor.cs` `AutoConfigureBP()` via SerializedObject
- [x] Mid-episode safety — `EnemyBrain.SafeRefreshComponents()` with component hash detection, called each `OnActionReceived`
- [x] Training arena builders — `Core/TrainingArenaBuilder.cs` (base), `Core/RPGArenaBuilder.cs`, `Core/ShooterArenaBuilder.cs`, `Core/RacingTrackBuilder.cs`

### Lower Priority ✅
- [x] Gizmos — `Core/DebugGizmos.cs` (raycasts, cover, distance rings, waypoints, reward heatmap overlay)
- [x] TensorBoard guide — `Training/TensorBoard_Guide.md` (metrics, interpretation, multi-run comparison)
- [x] ONNX warm-start — `Training/ONNX_WarmStart_Guide.md` (resume, init_path, behavioral cloning pre-training)
- [x] Multi-agent SquadBrain — `Core/SquadBrain.cs` (SimpleMultiAgentGroup, shared rewards, proximity bonuses)
- [x] WebGL/mobile ONNX — `Training/WebGL_Mobile_Guide.md` (platform matrix, Barracuda fallback, profiling)
- [x] Editor unit tests — `Editor/Tests/EnemyBrainTests.cs` + `Editor/Tests/BehaviorTreeAndCurriculumTests.cs` (26 tests total)

---

## Adding a New Genre

1. Add enum to `EnemyGenre` in `GenreProfile.cs`
2. Add `Create*Defaults()` factory method
3. Add button in `EnemyBrainEditor.OnInspectorGUI()`
4. Add `[MenuItem]` in `EnemyBrainEditor`
5. Create training YAML in `Training/`
6. Document in README files
7. Add a genre composer class in `Minigames/Composers/` and a dispatch case in `MinigameComposer`
8. Consider new Observation/Action/Reward components needed

## Adding a New Component

1. Inherit from `ObservationSource`, `ActionEffect`, or `RewardSource`
2. Implement required abstract members
3. Place in `Observations/`, `Actions/`, or `Rewards/`
4. No registration — `EnemyBrain` auto-discovers via `GetComponents<T>()`

---

## External Resources

- [ML-Agents Docs](https://github.com/Unity-Technologies/ml-agents/tree/release_21_docs/docs)
- [Training Config Reference](https://github.com/Unity-Technologies/ml-agents/blob/release_21_docs/docs/Training-Configuration-File.md)
- [PPO Paper](https://arxiv.org/abs/1707.06347)
- [SAC Paper](https://arxiv.org/abs/1801.01290)
- [ICM Curiosity Paper](https://arxiv.org/abs/1705.05363)
- [Unity ML-Agents Forum](https://forum.unity.com/forums/ml-agents.453/)

---

## Deployment

Copy `self_learning_enemies/` into `Assets/SelfLearningEnemies/` of any Unity project with ML-Agents installed via Package Manager.

Project root: `/home/lucy/Games/AI_tools/self_learning_enemies/`
