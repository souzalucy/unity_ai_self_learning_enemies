# WebGL & Mobile ONNX Inference Guide

ML-Agents uses ONNX Runtime for inference. Here's how to verify and optimize for constrained platforms.

## Quick Compatibility Check

| Platform | ONNX Runtime | GPU Inference | Notes |
|----------|-------------|---------------|-------|
| Windows/Mac/Linux (CPU) | ✅ Full | ❌ | Default, works everywhere |
| Windows (DirectML) | ✅ Full | ✅ | Enable in Player Settings |
| iOS | ✅ Barracuda fallback | ✅ (ANE) | Uses Unity Barracuda for ONNX |
| Android | ✅ Barracuda fallback | ✅ (NNAPI) | Uses Unity Barracuda |
| WebGL | ⚠️ Limited | ❌ | Must use Barracuda CPU |

## WebGL Setup

### Prerequisites
- Unity 2021.3 LTS or newer
- IL2CPP scripting backend (required for WebGL)
- Player Settings → WebGL → Code Generation → **Faster Runtime**

### 1. Switch to Barracuda Inference
In `BehaviorParameters`:
- Set `Inference Device` to **CPU**
- ML-Agents will use Barracuda internally for WebGL builds

### 2. Strip Unnecessary Ops
In your training config, keep networks small:
```yaml
network_settings:
  hidden_units: 128   # Smaller than usual (was 256)
  num_layers: 2       # Fewer layers (was 3)
```

### 3. Export ONNX with Opset 11
ML-Agents exports with ONNX opset 9 by default, which Barracuda handles. If you export manually:
```python
import torch
torch.onnx.export(model, dummy_input, "model.onnx", opset_version=11)
```

### 4. Test Inference in Editor
```csharp
// Add to any MonoBehaviour to verify ONNX loads correctly
void Start() {
    var bp = GetComponent<BehaviorParameters>();
    Debug.Log($"Model: {bp.Model}");
    Debug.Log($"Inference Device: {bp.InferenceDevice}");
}
```

## Mobile Optimization

### Reduce Network Size
Prefer smaller networks for mobile:
```yaml
network_settings:
  hidden_units: 128
  num_layers: 2
```

### Enable GPU Inference (iOS)
- Player Settings → iOS → **Allow GPU Compute** = true
- Set `Inference Device` to **GPU**

### Enable NNAPI (Android)
- Player Settings → Android → **Auto Graphics API** = true
- ML-Agents will attempt NNAPI acceleration

## Profiling ONNX Inference

Add this debug component to measure inference time:

```csharp
using UnityEngine;
using Unity.MLAgents;

public class InferenceProfiler : MonoBehaviour
{
    private Agent _agent;
    private float _totalTime;
    private int _samples;

    void Start() { _agent = GetComponent<Agent>(); }

    void Update()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _agent.RequestDecision();
        sw.Stop();
        _totalTime += sw.ElapsedMilliseconds;
        _samples++;
        if (_samples >= 100)
        {
            Debug.Log($"Avg inference: {_totalTime / _samples:F2}ms");
            _totalTime = 0; _samples = 0;
        }
    }
}
```

**Targets:** <5ms for PC, <10ms for mobile, <20ms for WebGL per agent.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| WebGL crashes on load | Reduce network size, ensure IL2CPP |
| Model doesn't run on mobile | Verify Barracuda package is installed |
| Slow inference | Reduce hidden_units, decrease agents per scene |
| Barracuda error "unsupported op" | Re-export with opset 9, avoid LSTM layers |
| NaN outputs | Normalize observations properly, check model was trained with same config |
