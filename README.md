# Self-Learning Enemies for Unity

> **Plug-and-play reinforcement learning for game enemies. RPG, Shooter, Racing — one framework.**

Add modular components to any prefab, choose a genre profile, run one CLI command, and get a trained neural network driving intelligent enemy behavior. Built on Unity ML-Agents (PPO / SAC / GAIL).

**69 files · ~6,700 lines · 61 unit tests · all priority levels complete**

---

## Quick Start (5 Minutes)

### 1. Prerequisites
- **Unity 2021.3+** with [ML-Agents](https://github.com/Unity-Technologies/ml-agents) package (Release 21)
- **Python 3.8+** with `pip install mlagents`

### 2. Setup Your Enemy
1. Add `EnemyBrain` to any prefab
2. Click **RPG / Shooter / Racing** in the inspector to create a `GenreProfile`
3. Snap on Observation, Action, and Reward components (see catalog below)
4. Click **Auto-Configure BehaviorParameters** — sizes are set automatically
5. Confirm **Behavior Name** is `EnemyBrain` (Auto-Configure sets it — it must match the `behaviors:` key in the training YAML)
6. (Optional) Implement `IStatusProvider` / `ITargetProvider` on your character for automatic stat/target discovery

### 3. Train
```bash
cd Assets/SelfLearningEnemies/Training
mlagents-learn rpg_trainer_config.yaml --run-id=my_enemy
```
Press Play. Training starts immediately.

### 4. Deploy
Assign the `.onnx` to `BehaviorParameters → Model`, set `Behavior Type → Inference Only`.

### 5. Heuristic Testing
Set `Behavior Type → Heuristic Only` to control manually:
- **WASD** — move · **Space / E / Q** — actions 1/2/3

---

## Architecture

```
EnemyBrain (Agent — 5 partial class files)
├── EnemyBrain.cs                   Core lifecycle + public API
├── EnemyBrain.StepLogic.cs         OnActionReceived + reward calc
├── EnemyBrain.ActionMapping.cs     ConfigureActionSpace + dispatch
├── EnemyBrain.ComponentDiscovery.cs CacheComponents + safety refresh
├── EnemyBrain.Heuristic.cs         Keyboard heuristic controls
├── GenreProfile (ScriptableObject preset)
├── ObservationSource[]  ─── CollectObservations(VectorSensor)
├── ActionEffect[]       ─── ApplyActions(discrete[], continuous[])
├── RewardSource[]       ─── CalculateReward() → float
├── BehaviorParameters   ─── .onnx model + inference device
└── DecisionRequester    ─── step interval
```

Every layer auto-discovered via `GetComponents<T>()`. No manual wiring.

### Interfaces (Decoupled Data)

| Interface | Default Impl | Used By |
|-----------|-------------|---------|
| `IStatusProvider` | `SimpleStatusProvider` | `ObsSelfStatus` |
| `ITargetProvider` | `SimpleTargetProvider` | `ObsTargetTransform`, `RewardDistanceManagement`, `RewardCoverUsage` |
| `ICombatTarget` | *(implement on your damageable)* | `ActionCombat` |
| `IDamageable` | *(simpler alternative)* | `ActionCombat` fallback |

---

## Component Catalog

### Observations (What the Enemy Sees)

| Component | Size | Description |
|-----------|------|-------------|
| `ObsSelfTransform` | 7 | Normalized position, forward, speed |
| `ObsTargetTransform` | 8 | Relative pos, distance, facing dot, target velocity |
| `ObsSelfStatus` | 5 | Health%, mana%, shield%, alive, cooldown |
| `ObsRaycastPerception` | N×5 | N rays over FOV: hit distance + 4-tag one-hot |
| `ObsGridSensor` | X×Z×T | Grid cells via Physics.OverlapBox, tag one-hot per cell |
| `ObsSoundPerception` | M×12 | M loudest sounds: relative pos(3), intensity(1), type one-hot(8) |
| `ObsWaypointProgress` | N×3 | N upcoming waypoint relative positions |
| `ObsBehaviorTreeSuggestions` | C+4 | BT-suggested actions + active flag + status one-hot |

### Actions (What the Enemy Does)

| Component | Disc | Cont | Description |
|-----------|------|------|-------------|
| `ActionNavMeshMovement` | 0 | 2 [dx,dz] | Sets NavMeshAgent destination |
| `ActionRigidBodyMovement` | 0 | 3 [steer,accel,brake] | WheelCollider or Rigidbody forces |
| `ActionCombat` | 1 branch | 0 | Attack slots with cooldowns |
| `ActionItemUsage` | 1 branch | 0 | Item/ability slots with UnityEvent |
| `ActionBehaviorTree` | 1 branch [FollowBT,OverrideBT] | C | Hybrid: runs BT; network chooses to follow/override |

### Rewards (How the Enemy Is Scored)

| Component | Signal | Best For |
|-----------|--------|----------|
| `RewardCombatPerformance` | +hit, +kill, –miss, –friendly fire | RPG, Shooter |
| `RewardSurvival` | +per-sec, –death, +completion | All |
| `RewardDistanceManagement` | Gaussian peak at preferred range | Kiting / gap-closing |
| `RewardWaypointProgress` | +waypoint, +speed alignment | Racing, patrol |
| `RewardCoverUsage` | +in-cover, –exposed, +enter-cover | Shooter |


---

## Advanced Features

### Behavior Trees (Hybrid AI)
Add `ActionBehaviorTree` + `ObsBehaviorTreeSuggestions` to blend hand-crafted BT logic with learned actions. Build trees with `BTSequence`, `BTSelector`, `BTCondition`, `BTActionNode`, `BTInverter`, `BTRepeater`. The network learns *when* to trust the BT vs. take its own actions.

### Curriculum Learning
Add `CurriculumManager` to any GameObject. Define lessons with increasing difficulty. Monitors rolling average reward and auto-advances when thresholds are met. Exposes lesson values via `GetParameter(key, defaultValue)`.

### Squad Coordination
Add `SquadBrain` to manage teams sharing group rewards via `SimpleMultiAgentGroup`. Includes proximity bonuses for allies.

### Imitation Learning (GAIL)
Record player demos with `DemoRecorderHelper`, train with `gail_trainer_config.yaml`. GAIL discriminator rewards mimicking player behavior.

### Debug Visualization
Add `DebugGizmos` to see raycasts, cover detection, distance rings, waypoints, and per-source reward labels in the Scene view.

### Arena Builders
Right-click → **Build Arena**: `RPGArenaBuilder` (pillars), `ShooterArenaBuilder` (cover+platforms), `RacingTrackBuilder` (oval track with waypoints).

---

## Training Configurations

| Config | Algorithm | Best For |
|--------|-----------|----------|
| `rpg_trainer_config.yaml` | PPO, 256×3 | Discrete combat |
| `shooter_trainer_config.yaml` | PPO + ICM, 512×3 | Sparse-reward exploration |
| `racing_trainer_config.yaml` | SAC, 256×3 | Continuous control |
| `gail_trainer_config.yaml` | PPO + GAIL | Learn from demos |

---

## Platform Support

| Platform | Runtime | Notes |
|----------|---------|-------|
| Desktop | ONNX CPU/GPU | Full support |
| iOS | Barracuda (ANE) | Auto fallback |
| Android | Barracuda (NNAPI) | Auto fallback |
| WebGL | Barracuda CPU | Reduce to 128×2 |

---

## Guides

| Guide | File |
|-------|------|
| TensorBoard monitoring | `Training/TensorBoard_Guide.md` |
| ONNX warm-start / resume | `Training/ONNX_WarmStart_Guide.md` |
| WebGL & mobile inference | `Training/WebGL_Mobile_Guide.md` |
| Developer reference | `AGENTS.md` |

---

## Complete File Structure

```
SelfLearningEnemies/
├── Core/                           (24 files)
│   ├── EnemyBrain.cs               Main Agent orchestrator (partial class)
│   ├── EnemyBrain.StepLogic.cs     OnActionReceived + reward calculation
│   ├── EnemyBrain.ActionMapping.cs Action space config + dispatch
│   ├── EnemyBrain.ComponentDiscovery.cs Component cache + safety refresh
│   ├── EnemyBrain.Heuristic.cs     Keyboard heuristic controls
│   ├── EnemyBrain.Telemetry.cs     Read-only reward/step/last-action accessors
│   ├── GenreProfile.cs             ScriptableObject presets
│   ├── ObservationSource.cs        Abstract perception base
│   ├── ActionEffect.cs             Abstract action base
│   ├── RewardSource.cs             Abstract scoring base
│   ├── IStatusProvider.cs          Health interface + SimpleStatusProvider
│   ├── ITargetProvider.cs          Target interface + SimpleTargetProvider
│   ├── SoundEventManager.cs        Global sound event system
│   ├── CurriculumManager.cs        Lesson-based difficulty progression
│   ├── SquadBrain.cs               Multi-agent group coordination
│   ├── DemoRecorderHelper.cs       GAIL demo recording wrapper
│   ├── DebugGizmos.cs              Scene view visualization overlay
│   ├── TrainingArenaBuilder.cs     Base arena generator
│   ├── RPGArenaBuilder.cs          RPG training arena
│   ├── ShooterArenaBuilder.cs      Shooter training arena
│   ├── RacingTrackBuilder.cs       Racing track generator
│   └── BT/                         (3 files: BTNode, Composites, Leafs)
├── Observations/                   (8 files)
├── Actions/                        (5 files)
├── Rewards/                        (5 files)
├── Minigames/                      (19 files)
│   ├── Composers/                  (5: ComposerUtils, BehaviorConfigurator, Rpg/Shooter/RacingComposer)
│   ├── Shared/                     (12: settings, composer, manager, RoundStateMachine, EnemySpawner, HUD, controllers, wiring, …)
│   └── Racing/                     (2: TrackCheckpoint, RacingRoundController)
├── Editor/                         (2 files + Tests/)
│   └── Tests/                      (6 test files, 61 tests)
├── Training/                       (4 YAML + 3 guides + Demos/)
├── Profiles/                       (README)
├── SelfLearningEnemies.asmdef
├── AGENTS.md
├── .editorconfig                   C# code quality rules
├── .quality-gate.yml               AI code-quality gate thresholds
└── README.md
```

---

## Creating Custom Components

```csharp
// Custom observation
public class MyObs : ObservationSource
{
    public override int ObservationSize => 3;
    public override void CollectObservations(VectorSensor s)
    {
        s.AddObservation(a); s.AddObservation(b); s.AddObservation(c);
    }
}

// Custom action
public class MyAction : ActionEffect
{
    public override int DiscreteBranchCount => 1;
    public override int[] DiscreteBranchSizes => new[] { 5 };
    public override int ContinuousActionCount => 2;
    public override void ApplyActions(float[] d, float[] c) { /* ... */ }
}

// Custom reward
public class MyReward : RewardSource
{
    public override float CalculateReward() => score * 0.1f;
}
```

Snap onto the EnemyBrain GameObject — auto-discovered. No registration needed.

---

## License

MIT — use freely in any project.
