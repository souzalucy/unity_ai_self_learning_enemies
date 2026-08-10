# ONNX Model Warm-Start Guide

Warm-starting lets you resume training from a previously trained model, saving days of compute.

## Method 1: Resume Training (Preferred)

If you have the full checkpoint directory, use `--resume`:
```bash
mlagents-learn config.yaml --run-id=continued_training --resume
```
This restores the model weights, optimizer state, and step count.

## Method 2: Initialize from ONNX (Warm-Start)

To start fresh training with a pre-trained model as a starting point:

### Step 1: Train a base model
```bash
mlagents-learn rpg_trainer_config.yaml --run-id=base_model
# Wait for training to complete or produce a good checkpoint
```

### Step 2: Configure warm-start in your training YAML
Add an `init_path` to the behavior config:
```yaml
behaviors:
  EnemyBrain:
    trainer_type: ppo
    init_path: results/base_model/EnemyBrain/EnemyBrain-500000.onnx
    # ... rest of config
```

### Step 3: Train with warm-start
```bash
mlagents-learn warmstart_config.yaml --run-id=fine_tuned_model
```
The agent starts with the base model's policy and continues learning.

## Method 3: Behavioral Cloning Pre-Training (BC + PPO)

1. **Record demonstrations** (see GAIL guide)
2. **Pre-train** with behavioral cloning:
   ```yaml
   behaviors:
     EnemyBrain:
       trainer_type: ppo
       pretraining:
         demo_path: Demos/
         strength: 0.5
         steps: 50000
   ```
3. This trains the agent to mimic demonstrations before switching to RL.

## Use Cases

| Scenario | Method |
|----------|--------|
| Training was interrupted | `--resume` |
| Fine-tune for a new level | ONNX warm-start |
| Transfer to harder difficulty | Warm-start from easy model |
| Bootstrap from player demos | Behavioral Cloning pre-training |
| Curriculum next lesson | Resume with new config |

## Important Notes

- **Observation/action space must match** between the original model and the new training config
- **Network architecture must match** (hidden_units, num_layers)
- The warm-start ONNX file only provides the policy; value function and optimizer start fresh
- For full state restoration (including optimizer momentum), always use `--resume`
