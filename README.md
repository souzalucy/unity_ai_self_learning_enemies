# Self-Learning Enemies for Unity

A plug-and-play, genre-agnostic reinforcement learning framework for creating intelligent enemies in Unity games. Built on top of Unity ML-Agents.

Supports **RPG**, **Shooter**, and **Racing** genres out of the box — extensible to any game style.

## Quick Start (5 Minutes)

### 1. Prerequisites
- Unity 2021.3+
- [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents) package (Release 21)
- Python 3.8+ with `mlagents` installed: `pip install mlagents`

### 2. Add to Your Enemy Prefab
1. Add `EnemyBrain` component (auto-adds `BehaviorParameters` + `DecisionRequester`)
2. Drag in a `GenreProfile` asset (RPG / Shooter / Racing)
3. Snap on Observation components:
   - `ObsSelfTransform` — enemy position, velocity, facing
   - `ObsTargetTransform` — player position, distance, angle
   - `ObsSelfStatus` — health, mana, shield
   - `ObsRaycastPerception` — spatial awareness via raycasts
   - `ObsWaypointProgress` — racing navigation
4. Snap on Action components:
   - `ActionNavMeshMovement` — RPG/Shooter movement
   - `ActionRigidBodyMovement` — Racing driving
   - `ActionCombat` — attack abilities
   - `ActionItemUsage` — items / specials
5. Snap on Reward components:
   - `RewardCombatPerformance` — damage dealt, kills
   - `RewardSurvival` — staying alive
   - `RewardDistanceManagement` — optimal positioning
   - `RewardWaypointProgress` — racing progress
   - `RewardCoverUsage` — cover tactics (shooter)

### 3. Train
```bash
cd Assets/SelfLearningEnemies/Training
mlagents-learn rpg_trainer_config.yaml --run-id=my_first_enemy
```
Press Play in Unity. Training begins automatically.

### 4. Deploy
Assign the generated `.onnx` file to `BehaviorParameters → Model` and set `Behavior Type → Inference Only`.

## Architecture

```
Your Enemy Prefab
├── EnemyBrain (Agent)
│   ├── GenreProfile (ScriptableObject)
│   ├── ObservationSource[]  ← what it sees
│   ├── ActionEffect[]       ← what it does
│   └── RewardSource[]       ← how it's scored
├── BehaviorParameters
└── DecisionRequester
```

Each layer is modular: add/remove components to customize the enemy without touching code.

## File Structure

```
SelfLearningEnemies/
├── Core/
│   ├── GenreProfile.cs         # ScriptableObject: genre presets
│   ├── ObservationSource.cs    # Abstract: what the enemy perceives
│   ├── ActionEffect.cs         # Abstract: what the enemy can do
│   ├── RewardSource.cs         # Abstract: how the enemy is scored
│   └── EnemyBrain.cs           # Main Agent: ties everything together
├── Observations/
│   ├── ObsSelfTransform.cs     # Own position, velocity, facing
│   ├── ObsTargetTransform.cs   # Target relative position
│   ├── ObsSelfStatus.cs        # Health, mana, shield
│   ├── ObsRaycastPerception.cs # Raycast-based spatial awareness
│   └── ObsWaypointProgress.cs  # Racing waypoints
├── Actions/
│   ├── ActionNavMeshMovement.cs # NavMeshAgent movement
│   ├── ActionRigidBodyMovement.cs # Racing physics
│   ├── ActionCombat.cs         # Attack / abilities
│   └── ActionItemUsage.cs      # Item / special usage
├── Rewards/
│   ├── RewardCombatPerformance.cs
│   ├── RewardSurvival.cs
│   ├── RewardDistanceManagement.cs
│   ├── RewardWaypointProgress.cs
│   └── RewardCoverUsage.cs
├── Editor/
│   ├── EnemyBrainEditor.cs     # Custom inspector + profile creator
│   └── SelfLearningEnemies.Editor.asmdef
├── Training/
│   ├── rpg_trainer_config.yaml
│   ├── shooter_trainer_config.yaml
│   └── racing_trainer_config.yaml
├── SelfLearningEnemies.asmdef
└── README.md
```

## Creating Custom Components

Extend any abstract base:

```csharp
public class MyCustomObservation : ObservationSource
{
    public override int ObservationSize => 3;
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(myValue1);
        sensor.AddObservation(myValue2);
        sensor.AddObservation(myValue3);
    }
}
```

Snap it onto your EnemyBrain GameObject — it's automatically discovered.

## Key Interfaces

- `ICombatTarget` — implement on damageable objects for `ActionCombat`
- `IDamageable` — simpler alternative damage interface

## Heuristic Testing

Set `Behavior Type → Heuristic Only` on BehaviorParameters to control the enemy manually:
- **WASD** — movement
- **Space** — action 1
- **E** — action 2
- **Q** — action 3

## License

MIT — use freely in any project.
