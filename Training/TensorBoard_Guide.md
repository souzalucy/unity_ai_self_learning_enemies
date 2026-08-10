# TensorBoard Integration Guide

TensorBoard provides real-time visualization of your training metrics: reward curves, loss, entropy, learning rate, and more.

## Launching TensorBoard

1. **Start training** as usual:
   ```bash
   mlagents-learn rpg_trainer_config.yaml --run-id=my_experiment
   ```

2. **In a second terminal**, launch TensorBoard pointing at the results directory:
   ```bash
   tensorboard --logdir results --port 6006
   ```

3. **Open** http://localhost:6006 in your browser.

## Key Metrics to Watch

| Metric | What it Means | Good Sign |
|--------|---------------|-----------|
| `Environment/Cumulative Reward` | Total reward per episode | Steadily increasing |
| `Environment/Episode Length` | Steps per episode | Stable or slightly increasing |
| `Losses/Policy Loss` | How much the policy is changing | Decreasing then stabilizing |
| `Losses/Value Loss` | Value function prediction error | Decreasing |
| `Policy/Entropy` | Randomness of actions | Starts high, gradually decreases |
| `Policy/Learning Rate` | Current learning rate | Following schedule (linear decay) |
| `Is Training` | Whether the agent is currently training | Should be 1 (if training) |

## Interpreting Reward Curves

```
Reward
  ^
  |     ~~~~~~~~~~~~~~~  ← converged
  |    /
  |   /
  |  /
  | /
  +------------------------→ Steps
```

- **Rising steadily:** Good — agent is learning
- **Flat but low:** Agent needs more exploration (increase entropy bonus, add curiosity)
- **Flat and high:** Agent may have converged — try increasing difficulty (curriculum)
- **Erratic / spiky:** Batch size may be too small, or environment too random
- **Sudden drop:** Check for bugs in reward function or environment reset

## Multi-Run Comparison

To compare different experiments:
```bash
tensorboard --logdir results --port 6006
```
TensorBoard automatically detects subdirectories in `results/` and shows them as separate runs.

## Smoothing

Use the smoothing slider in TensorBoard (left sidebar) to reduce noise. A value of 0.6–0.9 is usually good.

## Saving TensorBoard Logs

Logs are stored in `results/<run-id>/` as TFEvent files. Archive the entire `results/` directory to preserve training history.

## Advanced: Custom Metrics

You can log custom metrics via `Academy.Instance.StatsRecorder`:
```csharp
Academy.Instance.StatsRecorder.Add("MyCustomMetric", value);
```
These appear under `Environment/MyCustomMetric` in TensorBoard.
