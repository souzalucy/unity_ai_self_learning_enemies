# Genre Profiles

Profiles are Unity ScriptableObject assets created via the Editor.

## How to Create

### Method 1: Inspector
1. Select your enemy GameObject with `EnemyBrain` attached
2. In the Inspector, under "Quick Create Profiles", click **RPG**, **Shooter**, or **Racing**
3. Choose a save location — the profile is automatically assigned to your EnemyBrain

### Method 2: Assets Menu
1. Right-click in the Project window
2. Navigate to **Create → Self-Learning Enemies → RPG Profile** (or Shooter / Racing)
3. Drag the created asset onto your EnemyBrain's `Genre Profile` field

## Profile Details

| Profile  | Algorithm | Discrete Actions | Continuous | Obs Size | Stack |
|----------|-----------|-----------------|------------|----------|-------|
| RPG      | PPO       | 1 branch (6)     | 2          | 28       | 3     |
| Shooter  | PPO + ICM | 2 branches (5,4) | 4          | 48       | 4     |
| Racing   | SAC       | 2 branches (3,3) | 3          | 36       | 2     |

Profiles are fully customizable after creation — change any value in the Inspector.
