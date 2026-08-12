# Learning Guide — Self-Learning Enemies for Unity

> **A file-by-file walkthrough of the entire codebase.**
> Read this to understand *how* and *why* every piece works. Assumes basic C# and Unity knowledge.

---

## How to Use This Guide

Start at **Part 1** and read sequentially. Each section explains a file\'s purpose, the key concepts it demonstrates, and how it connects to other files.

**Prerequisite knowledge:** C# interfaces, inheritance, Unity `MonoBehaviour`, `ScriptableObject`, `GetComponent<T>()`, and the concept of neural networks (but not ML-Agents specifics — those are explained here).

---

## Part 1: The Big Picture

### What This Project Does

Game AI is traditionally hand-crafted: `if player is close, attack; else patrol`. This works but does not adapt. **Reinforcement Learning (RL)** trains a neural network through trial and error — the enemy tries actions, receives rewards/penalties, and gradually improves.

Unity\'s **ML-Agents** toolkit provides the RL engine (PPO, SAC algorithms) and the bridge between C# and Python/PyTorch. This project wraps ML-Agents in a **composable component system** so you never write RL code directly — you just snap components onto a prefab.

### The 4-Layer Architecture

```
EnemyBrain (inherits Agent from ML-Agents)
├── ObservationSource[]  — "what the enemy perceives" (eyes, ears, status)
├── ActionEffect[]       — "what the enemy can do" (move, attack, use items)
├── RewardSource[]       — "how the enemy is scored" (hit = good, die = bad)
└── GenreProfile          — "preset configuration" (RPG vs Shooter vs Racing)
```

**The key insight:** Each layer is an abstract base class. The `EnemyBrain` discovers all attached components via `GetComponents<T>()` at runtime. You add/remove components in the Unity Inspector without touching code.

### Training Loop (Simplified)

```
1. EnemyBrain collects observations from all ObservationSources
2. Observations are sent to the neural network (via ML-Agents gRPC)
3. Network outputs actions (discrete choices + continuous values)
4. EnemyBrain slices actions and dispatches to each ActionEffect
5. Each RewardSource calculates a reward for this step
6. Total reward is sent back to the network for learning
7. Repeat thousands of times
```

---

## Part 2: Core Framework (21 files — including 5 EnemyBrain partial class files)

### `Core/GenreProfile.cs`

**What it is:** A `ScriptableObject` — Unity\'s data container asset type. Holds genre-specific presets.

**Key concepts:**
- `ScriptableObject` allows reusable configuration assets (right-click > Create > Self-Learning Enemies > RPG Profile)
- `CreateRPGDefaults()` / `CreateShooterDefaults()` / `CreateRacingDefaults()` are **factory methods** that create instances with sensible defaults
- The `EnemyGenre` enum drives genre-specific behavior (training config selection, default component choices)

**Why it exists:** Instead of configuring 20 inspector fields every time, drag one profile asset and get sensible defaults.

### `Core/ObservationSource.cs`

**What it is:** Abstract base class for all perception components.

```csharp
public abstract class ObservationSource : MonoBehaviour
{
    public abstract int ObservationSize { get; }          // How many floats
    public abstract void CollectObservations(VectorSensor sensor);  // Write data
    public virtual void OnEpisodeBegin() { }              // Reset per-episode
}
```

**Key concepts:**
- **Abstract class pattern:** Subclasses MUST implement `ObservationSize` and `CollectObservations`
- **`VectorSensor`** is ML-Agents\' observation buffer. `sensor.AddObservation(float)` appends a value. Order and count must be deterministic.

### `Core/ActionEffect.cs`

**What it is:** Abstract base class for all action components.

```csharp
public abstract class ActionEffect : MonoBehaviour
{
    public abstract int DiscreteBranchCount { get; }
    public abstract int[] DiscreteBranchSizes { get; }
    public abstract int ContinuousActionCount { get; }
    public abstract void ApplyActions(float[] discrete, float[] continuous);
}
```

**Key concepts:**
- **Discrete vs Continuous:** Discrete = pick one of N options (attack 1, attack 2). Continuous = float [-1,1] (move X, aim angle)
- **Branches:** Multiple independent discrete choices per step. Branch 0 = which attack, Branch 1 = which item.
- **Action slicing:** EnemyBrain concatenates all branches, then slices back when dispatching.

### `Core/RewardSource.cs`

**What it is:** Abstract base class for all scoring components.

```csharp
public abstract class RewardSource : MonoBehaviour
{
    protected float rewardWeight = 1.0f;    // Multiplier
    protected bool isActive = true;
    public abstract float CalculateReward(); // Called every step
}
```

**Key concepts:**
- **Reward shaping:** Rewards are the ONLY feedback. Well-designed rewards = fast learning.
- **Weight tuning:** Different signals have different scales. `rewardWeight` balances them.

### `Core/EnemyBrain.cs` (split into 5 partial class files)

**What it is:** The central orchestrator — inherits from ML-Agents\' `Agent` class. **Most important class. Split across 5 partial files for maintainability. All 5 files share the same partial class EnemyBrain declaration. See AGENTS.md File Map for the full breakdown.**

**Method-by-method:**

- **`Awake()` / `CacheComponents()` / `ConfigureActionSpace()`:** Discovers all components via `GetComponents<T>()`, calculates total branches and continuous actions, builds offset arrays for action slicing.

- **`Initialize()`:** Called by ML-Agents when ready. Re-caches components and logs genre.

- **`OnEpisodeBegin()`:** Resets episode counters, calls `OnEpisodeBegin()` on every component.

- **`CollectObservations(VectorSensor sensor)`:** Iterates all `ObservationSource` components, validates each adds exactly `ObservationSize` values (catches bugs).

- **`OnActionReceived(ActionBuffers actionBuffers)`:** Core loop: safe-refresh, slice actions, dispatch to effects, calculate rewards, send feedback.

- **`Heuristic(ActionBuffers actionBuffers)`:** Manual control via WASD/Space/E/Q for testing and demo recording.

- **`SafeRefreshComponents()`:** Hash-based detection of component changes mid-episode with re-entrancy guard.

- **`ReportObjectiveComplete()` / `ReportDeath()`:** Public API for game code.

- **`ValidateSetup()`:** Diagnostic that checks for misconfigurations.

**Design patterns demonstrated:**
- **Composition over inheritance:** Behavior is composed by attaching components, not subclassing EnemyBrain.
- **Observer pattern:** Components are "observed" (discovered) by the brain via GetComponents.
- **Strategy pattern:** Each ObservationSource/ActionEffect/RewardSource is a pluggable strategy.

---

## Part 3: Observation Components (8 files)

### `Observations/ObsSelfTransform.cs`

**Observation size:** 7 floats (position xyz, forward xyz, speed)

**Key concept — Normalization:** Neural networks work best with inputs in [-1, 1] or [0, 1]. Raw world positions (like 47.2, -12.8) would cause unstable training. This component divides position by `arenaSize` to normalize it.

**ML-Agents integration:** Uses `sensor.AddObservation()` to write each float. ML-Agents also has built-in running normalization that further standardizes values during training.

### `Observations/ObsTargetTransform.cs`

**Observation size:** 8 floats (relative direction xyz, normalized distance, facing dot product, target velocity xyz)

**Key concept — Relative observations:** Instead of absolute target position (which changes when the enemy moves), it reports the target RELATIVE to the enemy. This is translation-invariant — the same observation means the same thing regardless of world position.

**Facing dot product:** `Vector3.Dot(transform.forward, dirToTarget)` returns 1.0 if facing the target, -1.0 if facing away, 0.0 if perpendicular. A single float that captures angular relationship.

**ITargetProvider support:** If no direct Transform reference is set, it searches for an `ITargetProvider` component on the same GameObject.

### `Observations/ObsSelfStatus.cs`

**Observation size:** 5 floats (health%, mana%, shield%, alive flag, reserved)

**Key concept — IStatusProvider:** Preferred way is to implement `IStatusProvider` on your health component. `ObsSelfStatus` auto-detects it. Falls back to direct public fields for quick prototyping.

### `Observations/ObsRaycastPerception.cs`

**Observation size:** N × 5 (hit distance + 4-tag one-hot per ray)

**Key concept — Spatial perception via raycasting:** Fires N rays evenly spaced across a configurable FOV. Each ray reports:
- Hit distance (normalized 0-1, where 1 = max distance / no hit)
- Tag one-hot encoding (which type of object was hit)

**One-hot encoding:** A vector where exactly one element is 1.0 and the rest are 0.0. E.g., `[1,0,0,0]` = Player, `[0,1,0,0]` = Enemy. Neural networks handle one-hot vectors well because they represent categories without implying ordering.

**Performance note:** Uses `Physics.Raycast` per ray. At 8 rays × every decision step (e.g., every 5th frame), this is negligible. At 32 rays × every frame, consider reducing.

### `Observations/ObsGridSensor.cs`

**Observation size:** gridX × gridZ × tagCount

**Key concept — Grid-based perception:** Divides the area around the enemy into cells. Each cell uses `Physics.OverlapBox` to detect what is inside, then encodes the tag as a one-hot vector.

**Tradeoff vs raycasts:** Grid gives more complete spatial information but is more expensive (X×Z overlap checks). Use smaller grids (8×8) for real-time, larger for training.

**Gizmos:** Draws the grid in the Scene view with `OnDrawGizmosSelected`.

### `Observations/ObsSoundPerception.cs`

**Observation size:** maxSounds × 12 (relative pos xyz, intensity, 8-type one-hot per sound)

**Key concept — Event-driven observation:** Unlike raycasts (polled every step), sounds are EVENT-DRIVEN. `SoundEventManager.Emit()` is called when something happens (gunshot, footstep). `ObsSoundPerception` queries recent events within hearing range.

**Distance attenuation:** Sound intensity drops with distance: `attenuatedIntensity = intensity * max(0, 1 - distance * attenuation)`. Sounds far away are quieter.

**Requires:** `SoundEventManager` in the scene (auto-created singleton if missing).

### `Observations/ObsWaypointProgress.cs`

**Observation size:** numWaypoints × 3 (relative position xyz per waypoint)

**Key concept — Navigation awareness:** Instead of just knowing "where is the next waypoint", it knows the next N waypoints. This lets the agent anticipate turns and plan paths, not just react to the immediate next point.

### `Observations/ObsBehaviorTreeSuggestions.cs`

**Observation size:** continuousOutputs + 4 (BT-suggested actions, BT active flag, BT status one-hot)

**Key concept — BT intent as observation:** This is the bridge between the Behavior Tree and the neural network. It reads the BT\'s "suggested" actions and feeds them as observations so the network learns WHEN to trust the BT.

**Requires:** `ActionBehaviorTree` on the same GameObject.

---

## Part 4: Action Components (5 files)

### `Actions/ActionNavMeshMovement.cs`

**Actions:** 2 continuous [moveX, moveZ]

**Key concept — Converting continuous actions to movement:** The network outputs two floats in [-1, 1]. These are treated as a direction vector in the XZ plane. The component sets the NavMeshAgent destination to `currentPosition + direction * destinationDistance`.

**Design choice:** Why not use discrete actions (north/south/east/west)? Continuous actions allow smooth movement in any direction with any speed — the agent learns to make small adjustments, not just 4-directional movement.

### `Actions/ActionRigidBodyMovement.cs`

**Actions:** 3 continuous [steer, accelerate, brake]

**Key concept — Vehicle physics:** Uses realistic force-based movement. Tries WheelColliders first (for proper car physics), falls back to direct Rigidbody forces. The agent learns throttle control, braking, and steering simultaneously.

### `Actions/ActionCombat.cs`

**Actions:** 1 discrete branch with N+1 choices (0=none, 1=attack1, ..., N=special)

**Key concept — Cooldown gating:** Actions have cooldowns. If the agent picks an action on cooldown, nothing happens. This teaches the agent timing — it learns to NOT spam attacks and instead wait for cooldowns.

**ICombatTarget integration:** When an attack fires, it calls `ICombatTarget.TakeDamage()` on the assigned target. If no ICombatTarget, falls back to `SendMessage`.

### `Actions/ActionItemUsage.cs`

**Actions:** 1 discrete branch with N+1 choices

**Key concept — UnityEvent bridge:** Fires `OnItemUsed` UnityEvent with slot index and target. Game-specific logic (heal, buff, throw grenade) is wired in the Inspector or via code — the AI system doesn\'t need to know what items do, just when to use them.

### `Actions/ActionBehaviorTree.cs`

**Actions:** 1 discrete branch [FollowBT=0, OverrideBT=1] + C continuous

**Key concept — Hybrid AI:** The Behavior Tree runs each step and produces "suggested" actions. The neural network sees these suggestions (via ObsBehaviorTreeSuggestions) and chooses:
- **FollowBT (0):** Use the BT\'s suggested actions (optionally blended with learned via `btBlend`)
- **OverrideBT (1):** Use the network\'s own actions, ignoring the BT

The network learns a meta-policy: when to trust the hand-crafted BT vs. when it knows better.

**Default BTs:** `BuildDefaultCombatBT(Transform target)` creates a simple combat tree. Users replace this with their own trees.

---

## Part 5: Reward Components (5 files)

### `Rewards/RewardCombatPerformance.cs`

**Key concept — External registration API:** This reward source does NOT poll the game state. Instead, game code calls `RegisterHit()` / `RegisterMiss()` / `RegisterKill()` / `RegisterFriendlyFire()` from the damage pipeline. This decouples the AI from the combat system.

**Per-step accumulation:** Values accumulate during the step, then `CalculateReward()` returns the total and resets accumulators. This is important because `CalculateReward()` is called once per agent step, but multiple combat events can happen between steps.

### `Rewards/RewardSurvival.cs`

**Key concept — Dense vs sparse rewards:** Provides a small reward every step (dense) plus a large penalty on death (sparse). Dense rewards help the agent learn faster because it gets feedback every step, not just at episode end.

### `Rewards/RewardDistanceManagement.cs`

**Key concept — Gaussian reward shaping:** Uses a Gaussian (bell curve) centered at `preferredDistance`. Being at the perfect range gives maximum reward. Being too close or too far gives less. This creates a smooth gradient the agent can follow — unlike a binary "in range / out of range" reward.

**Math:** `exp(-(distance - preferred)² / (2 × sigma²))` — the Gaussian function produces a smooth peak.

### `Rewards/RewardWaypointProgress.cs`

**Key concept — Progress-based reward:** Rewards forward progress, not just reaching waypoints. The dot product of velocity vs. waypoint direction rewards moving TOWARD the next waypoint, even between checkpoints.

### `Rewards/RewardCoverUsage.cs`

**Key concept — State-change bonus:** Provides a flat reward per step in cover, PLUS a bonus for ENTERING cover (first step after being exposed). The enter-cover bonus helps the agent discover cover-seeking behavior — once it accidentally steps behind cover, it gets a spike that reinforces the behavior.

---

## Part 6: Interfaces (2 files)

### `Core/IStatusProvider.cs`

**Key concept — Dependency Inversion:** Instead of `ObsSelfStatus` directly reading `enemy.health`, it reads from `IStatusProvider`. Any component implementing this interface works — your custom health system, a Unity asset store solution, a mock for testing.

**`SimpleStatusProvider`:** A default implementation with `TakeDamage()`, `Heal()`, `Revive()`, shield absorption, and `OnHealthChanged` / `OnDeath` events. Use as-is or replace.

### `Core/ITargetProvider.cs`

**Key concept — Target abstraction:** Centralizes target selection logic. Instead of each component finding its own target via `GameObject.FindWithTag()`, they all read from one `ITargetProvider`. This ensures consistent targeting and lets you swap targeting logic (nearest, highest threat, line-of-sight) without changing observation/reward code.

**`SimpleTargetProvider`:** Range check + line-of-sight check + tag search. Fires `OnTargetAcquired` / `OnTargetLost` events.

---

## Part 7: Behavior Tree System (3 files in `Core/BT/`)

### Design Philosophy

Behavior Trees are the industry standard for game AI (used in Halo, The Last of Us, countless others). They are hierarchical state machines where nodes return **Success**, **Failure**, or **Running**.

Our implementation is minimal but complete — designed to be used with `ActionBehaviorTree` for the hybrid AI pattern.

### `Core/BT/BTNode.cs`

```csharp
public enum BTStatus { Success, Failure, Running }

public abstract class BTNode
{
    public abstract BTStatus Tick();
    public virtual void Reset() { }
}
```

**Key concept — Tick-based execution:** Unlike Unity\'s Update(), BT nodes are ticked on demand. Each `Tick()` call advances the node one step. `Running` means "I\'m not done yet, keep ticking me next frame".

### `Core/BT/BTComposites.cs` (Sequence + Selector)

**`BTSequence`:** Ticks children in order. If any child fails, the sequence fails immediately and resets. If all children succeed, the sequence succeeds. This is "AND" logic: do A, THEN do B, THEN do C.

**`BTSelector`:** Ticks children in order. If any child succeeds, the selector succeeds immediately. If all children fail, the selector fails. This is "OR" logic: try A, if that fails try B, if that fails try C.

### `Core/BT/BTLeafs.cs` (Condition, Action, Inverter, Repeater)

**`BTCondition`:** Wraps a `Func<bool>`. Returns Success if true, Failure if false. Used to check game state ("is enemy in range?", "is health low?").

**`BTActionNode`:** Wraps an `Action` delegate. Executes it and returns a configurable status. Used to perform actions ("move to cover", "attack player", "use health potion").

**`BTInverter`:** Decorator that flips Success to Failure and vice versa. "Is enemy NOT in range?" = Inverter(Condition("is enemy in range?")).

**`BTRepeater`:** Decorator that repeats its child N times. Always returns Running until exhausted.

**Example tree:**
```csharp
var tree = new BTSelector("Combat",
    // Priority 1: Attack if in range
    new BTSequence("Attack",
        new BTCondition(() => distance < 10f),
        new BTActionNode(() => Attack())
    ),
    // Priority 2: Chase if enemy visible
    new BTSequence("Chase",
        new BTCondition(() => hasLineOfSight),
        new BTActionNode(() => MoveToward())
    ),
    // Fallback: Patrol
    new BTActionNode(() => Patrol())
);
```

---

## Part 8: Sound Event System

### `Core/SoundEventManager.cs`

**Key concept — Singleton pattern:** There is one global `SoundEventManager` in the scene. Any code can call `SoundEventManager.Emit(position, intensity, type)` to broadcast a sound event. The manager stores recent events in a list, prunes old ones, and returns nearby events when queried.

**Why a singleton?** Sound events are inherently global — a gunshot at position X can be heard by all enemies within range. A singleton avoids duplicating event lists and ensures consistency.

**Performance:** Capped at `maxEvents` (default 64). Old events are pruned. Even with many emitters, the list stays small.

**SoundType enum:** 8 categories (Footstep, Gunshot, Explosion, Ability, Voice, Vehicle, Impact, Other). This one-hot encodes in observations so the network can distinguish between "I heard footsteps nearby" and "I heard an explosion far away".

---

## Part 9: Curriculum Learning

### `Core/CurriculumManager.cs`

**Key concept — Curriculum Learning:** Start training with easy tasks, then gradually increase difficulty. This prevents the agent from being overwhelmed early and helps it learn foundational skills first.

**How it works:**
1. Define `CurriculumLesson[]` with thresholds and environment parameters
2. After each episode, call `ReportEpisodeComplete(episodeReward)`
3. The manager tracks a rolling average reward
4. When average exceeds `completionThreshold`, it advances to the next lesson
5. `EnvironmentParameters` are updated (enemy_health, enemy_count, etc.)
6. Other game systems read these parameters via `Academy.Instance.EnvironmentParameters.GetWithDefault()`

**ML-Agents integration:** `EnvironmentParameters` is ML-Agents\' mechanism for passing dynamic values from the training environment to the trainer. The trainer can randomize within a range, and the curriculum narrows the range as lessons progress.

**`CurriculumLesson`:** A `[System.Serializable]` class (not a MonoBehaviour). Defines completion criteria (`completionThreshold`, `minEpisodes`, `windowSize`) and environment parameter values.

---

## Part 10: Imitation Learning (GAIL)

### `Core/DemoRecorderHelper.cs`

**Key concept — Demonstration recording:** Before training, a human plays the game in Heuristic mode. `DemoRecorderHelper` wraps ML-Agents\' built-in `DemonstrationRecorder` to save (.obs, .act, .reward) data to a `.demo` file.

**Why record demos?** Training from scratch (random actions) can take millions of steps. Starting from human demonstrations "bootstraps" the agent — it first learns to mimic the player, then improves via RL.

### `Training/gail_trainer_config.yaml`

**Key concept — GAIL (Generative Adversarial Imitation Learning):** A discriminator network tries to tell "real" (player) from "fake" (agent) behavior. The agent (generator) is rewarded for fooling the discriminator. Over time, the agent\'s behavior becomes indistinguishable from the player\'s.

**Why GAIL + extrinsic?** The config uses both `gail` (strength 0.5) and `extrinsic` (strength 0.3) reward signals. Pure GAIL might learn to stand still if the player stood still. Extrinsic rewards keep the agent task-focused.

---

## Part 11: Arena Builders

### `Core/TrainingArenaBuilder.cs`

**Key concept — Template Method pattern:** Base class defines `BuildArena()` which calls `BuildFloor()`, `BuildWalls()`, `BuildGenreFeatures()` (abstract), `CreateSpawnPoints()`, `SpawnEntities()`. Subclasses only implement `BuildGenreFeatures()`.

**Why procedural generation?** Training needs MANY parallel environments. Manually placing cover objects in 16 copies of a scene is tedious. The builder creates unique but consistent arenas with one click.

### `Core/RPGArenaBuilder.cs`

Creates pillars/obstacles tagged "Cover" with randomized but valid positions (away from center, away from player spawn).

### `Core/ShooterArenaBuilder.cs`

Creates three types of cover: tall walls, low barricades, elevated platforms with ramps. Random rotation adds variety.

### `Core/RacingTrackBuilder.cs`

**Key concept — Parametric track generation:** Uses math to generate an oval track (two straights + two semi-circles). `GetTrackPos(t)` maps a parameter t [0,1] to a world position along the track. `GetTrackDir(t)` computes the tangent direction.

**Why parametric?** Waypoints, start/finish line, and spawn points are all computed from the same parametric function. Change `straightLength` or `cornerRadius` and everything updates consistently.

---

## Part 12: Multi-Agent Coordination

### `Core/SquadBrain.cs`

**Key concept — Cooperative multi-agent RL:** Multiple agents share a team reward. If one agent sacrifices itself to help the team, it still benefits from the group reward. This prevents selfish behavior.

**ML-Agents integration:** Uses `SimpleMultiAgentGroup` — a built-in class that registers multiple agents and provides `AddGroupReward()` (distributed equally) and `EndGroupEpisode()` (ends episode for all members).

**Proximity bonus:** `ApplyProximityRewards()` gives small rewards for staying near allies. This encourages coordinated movement rather than agents scattering randomly.

---

## Part 13: Debug Visualization

### `Core/DebugGizmos.cs`

**Key concept — Unity Gizmos and Handles:** Gizmos draw in the Scene view (not in-game). `Handles.Label` adds floating text. This component provides complete visual debugging without runtime overhead (all code is `#if UNITY_EDITOR`).

**What it visualizes:**
- **Raycasts:** Cyan lines for misses, red lines for hits, spheres at hit points
- **Cover:** Green line = in cover, red line = exposed, labels
- **Distance rings:** Yellow = preferred range, orange = max range
- **Waypoints:** Yellow spheres with connecting lines
- **Reward heatmap:** Per-source values as colored labels above the enemy

**Design pattern:** Uses `SerializedObject` to read private fields from other components without requiring public accessors. This keeps the API clean while enabling debugging.

---

## Part 14: Training Configurations

### YAML Structure Explained

Each training config is a YAML file consumed by `mlagents-learn`. Key sections:

**`trainer_type`:** The RL algorithm. `ppo` (Proximal Policy Optimization) for most cases; `sac` (Soft Actor-Critic) for continuous control.

**`hyperparameters`:**
- `batch_size`: How many experiences to learn from at once. Larger = more stable, slower.
- `buffer_size`: How many experiences to collect before learning. Larger = more data per update.
- `learning_rate`: How fast the network updates. Too high = unstable; too low = slow.
- `beta`: Entropy bonus — encourages exploration. Higher = more random actions.
- `epsilon`: PPO clipping range — prevents too-large policy updates.
- `lambd`: GAE lambda — bias-variance tradeoff for advantage estimation (0.95 = standard).

**`network_settings`:**
- `hidden_units`: Neurons per hidden layer (256 = standard, 512 = complex tasks, 128 = mobile).
- `num_layers`: Hidden layers (3 = standard, 2 = simpler).
- `normalize`: Running mean/std normalization of observations (almost always true).

**`reward_signals`:**
- `extrinsic`: Standard task reward with `gamma` (discount factor — 0.99 = care about future) and `strength` (weight).
- `curiosity` (ICM): Intrinsic reward for exploring unfamiliar states. Helps in sparse-reward environments (shooter).
- `gail`: Adversarial reward from a discriminator trained on demos.

**`max_steps`:** Total training steps. 5M = typical for simple behaviors; 20M = complex.

**`time_horizon`:** Steps before bootstrapping. 128 = standard; 512 = longer-term dependencies (racing).

### Genre-Specific Config Differences

| Setting | RPG | Shooter | Racing | Why |
|---------|-----|---------|--------|-----|
| trainer | PPO | PPO | SAC | Racing is continuous control |
| hidden_units | 256 | 512 | 256 | Shooter needs more spatial reasoning |
| curiosity | No | Yes | No | Shooter has sparse rewards |
| time_horizon | 128 | 256 | 512 | Racing needs longer planning |
| max_steps | 5M | 10M | 20M | Racing takes longest to master |

---

## Part 15: Unit Tests

### `Editor/Tests/EnemyBrainTests.cs`

**Key concept — NUnit + Unity Test Framework:** Tests use `[SetUp]` to create a GameObject with EnemyBrain before each test and `[TearDown]` to destroy it after. This ensures test isolation.

**What is tested:**
- Component discovery (`CacheComponents` finds all types)
- Observation size summation
- Action branch size calculations
- SafeRefreshComponents (no false positives, detects additions, re-entrant guard)
- ValidateSetup (fails with no components, passes with components)
- Individual component observation sizes

### `Editor/Tests/BehaviorTreeAndCurriculumTests.cs`

**What is tested:**
- BT nodes: Condition (true/false), Sequence (all success / one failure), Selector (first success / all fail), Inverter, ActionNode execution, Repeater
- Curriculum: No-lessons safety, lesson advancement, rolling average, reset
- Interfaces: TakeDamage, Heal, shield absorption

**Why unit tests matter for ML:** If observation sizes change or action mapping breaks, training silently produces garbage. These tests catch those regressions before you waste hours of GPU time.

---

## Part 16: ML-Agents Integration Points

### How C# talks to Python

ML-Agents uses **gRPC** (Google Remote Procedure Call) over localhost. When you run `mlagents-learn`, it starts a Python process that:
1. Listens for Unity connections on port 5004
2. Receives observations and rewards from the C# agent
3. Runs the neural network (forward pass for actions, backward pass for training)
4. Sends actions back to the C# agent

### Key ML-Agents classes used

| Class | Used In | Purpose |
|-------|---------|---------|
| `Agent` | `EnemyBrain` (base class) | The agent interface: OnEpisodeBegin, CollectObservations, OnActionReceived, Heuristic |
| `VectorSensor` | All `ObservationSource` subclasses | Buffer for writing observation floats |
| `ActionBuffers` | `EnemyBrain.OnActionReceived` | Container for discrete + continuous actions from the network |
| `BehaviorParameters` | Required component | Holds model file, inference device, behavior type |
| `DecisionRequester` | Required component | Controls how often the agent requests decisions (every N Academy steps) |
| `SimpleMultiAgentGroup` | `SquadBrain` | Groups multiple agents for cooperative training |
| `DemonstrationRecorder` | `DemoRecorderHelper` | Records agent states + actions to .demo files |
| `Academy.Instance.EnvironmentParameters` | `CurriculumManager` | Dynamic float parameters the trainer can randomize |
| `Academy.Instance.StatsRecorder` | Any component | Log custom metrics to TensorBoard |

### Observation/action lifecycle

```
Frame N:   Decision requested by DecisionRequester
           → CollectObservations() called
           → Observations sent to Python via gRPC
           → Python runs neural network forward pass
           → Actions sent back to C#
           → OnActionReceived() called
           → Actions dispatched to ActionEffects
           → Rewards calculated
           → Rewards sent to Python for training
```

### Training vs Inference

| Mode | Behavior Type | What happens |
|------|--------------|--------------|
| Training | Default | Observations + actions exchanged; Python trains the network |
| Inference | Inference Only | Observations → ONNX Runtime forward pass → actions (no Python needed) |
| Heuristic | Heuristic Only | `Heuristic()` method called; keyboard input → actions |

---

## Part 17: Design Patterns Summary

| Pattern | Where | Why |
|---------|-------|-----|
| **Composition over inheritance** | EnemyBrain + components | Behavior built by attaching, not subclassing |
| **Strategy pattern** | ObservationSource / ActionEffect / RewardSource | Pluggable algorithms for perception, action, scoring |
| **Template Method** | TrainingArenaBuilder.BuildArena() | Fixed build sequence, customizable genre features |
| **Singleton** | SoundEventManager | Global sound event registry |
| **Observer** | IStatusProvider.OnHealthChanged, ITargetProvider.OnTargetAcquired | Decoupled event notifications |
| **Dependency Inversion** | IStatusProvider, ITargetProvider | High-level modules depend on abstractions, not concrete health/target systems |
| **Factory Method** | GenreProfile.CreateRPGDefaults() | Create pre-configured ScriptableObjects |
| **Decorator** | BTInverter, BTRepeater | Wrap BT nodes with modified behavior |
| **Composite** | BTSequence, BTSelector | Compose multiple BT nodes into a tree |

---

## Part 18: Extending the System

### Adding a new observation
1. Subclass `ObservationSource`
2. Implement `ObservationSize` (return a constant)
3. Implement `CollectObservations(VectorSensor sensor)` (add exactly `ObservationSize` floats)
4. Place in `Observations/`
5. Snap onto EnemyBrain — auto-discovered

### Adding a new action
1. Subclass `ActionEffect`
2. Implement `DiscreteBranchCount`, `DiscreteBranchSizes`, `ContinuousActionCount`
3. Implement `ApplyActions(float[] discrete, float[] continuous)`
4. Place in `Actions/`

### Adding a new reward
1. Subclass `RewardSource`
2. Implement `CalculateReward()` (return reward for this step)
3. Override `OnEpisodeBegin()` if you track per-episode state
4. Place in `Rewards/`

### Adding a new genre
1. Add enum to `EnemyGenre` in `GenreProfile.cs`
2. Add `CreateXDefaults()` factory method
3. Add button in `EnemyBrainEditor.OnInspectorGUI()`
4. Add `[MenuItem]` attribute in `EnemyBrainEditor`
5. Create training YAML in `Training/`
6. Consider what new Observation/Action/Reward components are needed

---

## Recommended Learning Path

1. **Read Part 1-2** to understand the architecture
2. **Read the EnemyBrain partial files** (`EnemyBrain.cs` through `EnemyBrain.Heuristic.cs`) — they form the central orchestrator
3. **Pick one Observation** (e.g., ObsSelfTransform) and trace it from `CollectObservations` to the sensor
4. **Pick one Action** (e.g., ActionCombat) and trace it from `ApplyActions` backwards
5. **Pick one Reward** (e.g., RewardSurvival) and trace how `CalculateReward` feeds into `AddReward`
6. **Read the BT system** (BTNode, composites, leafs) — small and self-contained
7. **Read the training config YAML files** with the Part 14 reference
8. **Browse the unit tests** — they document expected behavior
9. **Read the remaining features** (SoundEventManager, CurriculumManager, SquadBrain, DebugGizmos)
10. **Try extending:** add a custom Observation, train briefly, and watch the reward curve

---

*Learning guide complete. 48 files, 18 parts, one unified system.*
