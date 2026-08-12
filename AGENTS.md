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

| File | Lines | Role |
|------|-------|------|
| `Core/GenreProfile.cs` | 129 | ScriptableObject with enum, presets, factory methods |
| `Core/ObservationSource.cs` | 39 | Abstract: `ObservationSize`, `CollectObservations()`, `OnEpisodeBegin()` |
| `Core/ActionEffect.cs` | 44 | Abstract: `DiscreteBranchCount/Sizes`, `ContinuousActionCount`, `ApplyActions()` |
| `Core/RewardSource.cs` | 54 | Abstract: `CalculateReward()`, `RewardWeight`, `IsActive` |
| `Core/EnemyBrain.cs` | 111 | Main `Agent`: lifecycle, public API, ValidateSetup (partial class) |
| `Core/EnemyBrain.StepLogic.cs` | 38 | Partial: `OnActionReceived`, `CalculateStepReward` |
| `Core/EnemyBrain.ActionMapping.cs` | 63 | Partial: `ConfigureActionSpace`, `DispatchActions`, slicing helpers |
| `Core/EnemyBrain.ComponentDiscovery.cs` | 58 | Partial: `CacheComponents`, `SafeRefreshComponents`, `ComputeComponentHash` |
| `Core/EnemyBrain.Heuristic.cs` | 46 | Partial: `Heuristic` + keyboard input helpers |
| `Observations/ObsSelfTransform.cs` | 47 | 7 floats: normalized pos, forward, speed |
| `Observations/ObsTargetTransform.cs` | 66 | 8 floats: relative dir, distance, facing dot, target velocity |
| `Observations/ObsSelfStatus.cs` | 53 | 5 floats: health%, mana%, shield%, alive, reserved |
| `Observations/ObsRaycastPerception.cs` | 74 | N×5 floats: hit distance + 4-tag one-hot per ray |
| `Observations/ObsWaypointProgress.cs` | 56 | N×3 floats: relative waypoint offsets |
| `Actions/ActionNavMeshMovement.cs` | 58 | 2 continuous → NavMeshAgent destination |
| `Actions/ActionRigidBodyMovement.cs` | 74 | 3 continuous → WheelCollider / Rigidbody forces |
| `Actions/ActionCombat.cs` | 100 | 1 discrete branch → attack slots with cooldowns, `ICombatTarget` |
| `Actions/ActionItemUsage.cs` | 65 | 1 discrete branch → item slots with `UnityEvent<int, Transform>` |
| `Rewards/RewardCombatPerformance.cs` | 72 | External `RegisterHit/Miss/Kill/FriendlyFire()` API |
| `Rewards/RewardSurvival.cs` | 62 | Per-step survival + death penalty + completion bonus |
| `Rewards/RewardDistanceManagement.cs` | 66 | Gaussian-shaped range preference |
| `Rewards/RewardWaypointProgress.cs` | 82 | Waypoint-pass + speed-direction alignment |
| `Rewards/RewardCoverUsage.cs` | 92 | Raycast-based cover detection + enter-cover bonus |
| `Editor/EnemyBrainEditor.cs` | 140 | Custom inspector with live stats, Validate, profile creator |
| `Training/rpg_trainer_config.yaml` | 50 | PPO, 256×3, optional curriculum |
| `Training/shooter_trainer_config.yaml` | 43 | PPO + ICM curiosity, 512×3 |
| `Training/racing_trainer_config.yaml` | 41 | SAC, 256×3, continuous-optimized |
| `SelfLearningEnemies.asmdef` | 16 | Assembly: depends on `Unity.ML-Agents` |
| `Editor/SelfLearningEnemies.Editor.asmdef` | 16 | Editor assembly: depends on main + `Unity.ML-Agents` |
| `Editor/Tests/EnemyBrainTests.cs` | 134 | 12 tests: component discovery, action mapping, safety |
| `Editor/Tests/BehaviorTreeAndCurriculumTests.cs` | 159 | 13 tests: BT nodes, curriculum, IStatusProvider |
| `Editor/Tests/GenreAndRewardTests.cs` | 114 | 8 tests: GenreProfile factories + combat reward |
| `Editor/Tests/RewardSubclassTests.cs` | 170 | 12 tests: Survival, Distance, Cover, Waypoint rewards |
| `Editor/Tests/EnemyBrainBranchTests.cs` | 93 | 9 tests: ReportDeath/Objective, Validate, debugMode |
| `README.md` | 136 | User-facing setup guide |
| `Profiles/README.md` | 24 | How to create `.asset` profiles |
| `.editorconfig` | 57 | C# code quality rules (Roslyn/ca1502/ca1822/etc.) |

**Total: 48 files (33 .cs source, 5 test .cs, 4 .yaml, 5 .md, 3 .asmdef, 1 .editorconfig)**


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
2. **Observation size must match BehaviorParameters manually** — editor shows but doesn't auto-set
3. **No built-in curriculum loader** — YAML configs have commented-out blocks
4. **Racing forces are untuned** — motor/brake/turn values need per-vehicle calibration
5. **`ObsSelfStatus` uses public fields** — game health system must update them each frame
6. **No multi-agent coordination** — each `EnemyBrain` trains independently
7. **Heuristic mode limited** — 3 discrete + 2 continuous actions hardcoded
8. **No `OnValidate` auto-sync** — manual button in editor instead

---

## Next Steps

### Immediate — Integration Validation
- [ ] Install into a real Unity project with ML-Agents
- [ ] Create minimal training arena (flat plane, player, 1 enemy)
- [ ] Run `mlagents-learn rpg_trainer_config.yaml --run-id=test` and verify training
- [ ] Test heuristic mode with WASD/Space

### High Priority ✅
- [x] `ObsGridSensor` — `Observations/ObsGridSensor.cs` (Physics.OverlapBox grid, tag one-hot per cell)
- [x] `ObsSoundPerception` — `Observations/ObsSoundPerception.cs` + `Core/SoundEventManager.cs` (global sound event system)
- [x] `ActionBehaviorTree` — `Actions/ActionBehaviorTree.cs` + `Observations/ObsBehaviorTreeSuggestions.cs` + `Core/BT/` (hybrid BT + learned actions, FollowBT/OverrideBT branching)
- [x] `CurriculumManager` — `Core/CurriculumManager.cs` (lesson thresholds, rolling reward tracking, EnvironmentParameters integration)
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
7. Consider new Observation/Action/Reward components needed

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
