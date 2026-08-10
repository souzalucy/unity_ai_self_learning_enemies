using UnityEngine;
#if UNITY_ML_AGENTS
using Unity.MLAgents.Demonstrations;
#endif

namespace SelfLearningEnemies
{
    /// <summary>
    /// Helper to set up demonstration recording for GAIL training.
    /// Add this component alongside EnemyBrain to easily record player demonstrations.
    ///
    /// Usage:
    ///   1. Add this component to your EnemyBrain GameObject
    ///   2. Set Behavior Type to "Heuristic Only"
    ///   3. Set a demo name
    ///   4. Hit Play and control the enemy with WASD/Space
    ///   5. Press Stop — the .demo file is saved to Assets/Demos/
    ///   6. Move .demo files to Training/Demos/ for GAIL training
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    public class DemoRecorderHelper : MonoBehaviour
    {
        [Header("Recording Settings")]
        [Tooltip("Name for the demo file (without extension).")]
        public string demoName = "PlayerDemo";

        [Tooltip("Automatically start recording when Play is pressed.")]
        public bool autoRecord = true;

        [Tooltip("Maximum number of steps to record.")]
        public int maxRecordSteps = 5000;

        [Header("Status")]
        [SerializeField] private bool _isRecording;
        [SerializeField] private int _recordedSteps;

        private EnemyBrain _brain;

#if UNITY_ML_AGENTS
        private DemonstrationRecorder _recorder;
#endif

        private void Awake()
        {
            _brain = GetComponent<EnemyBrain>();
        }

        private void Start()
        {
#if UNITY_ML_AGENTS
            _recorder = GetComponent<DemonstrationRecorder>();
            if (_recorder == null)
            {
                _recorder = gameObject.AddComponent<DemonstrationRecorder>();
            }

            _recorder.DemonstrationName = demoName;
            _recorder.Record = false;
#endif

            if (autoRecord)
                StartRecording();
        }

        public void StartRecording()
        {
#if UNITY_ML_AGENTS
            if (_recorder != null)
            {
                _recorder.Record = true;
                _isRecording = true;
                _recordedSteps = 0;
                Debug.Log($"[DemoRecorder] Started recording demo: {demoName}");
            }
            else
            {
                Debug.LogWarning("[DemoRecorder] DemonstrationRecorder not available. Is ML-Agents installed?");
            }
#else
            Debug.LogWarning("[DemoRecorder] ML-Agents not detected. Install the package to record demos.");
#endif
        }

        public void StopRecording()
        {
#if UNITY_ML_AGENTS
            if (_recorder != null && _recorder.Record)
            {
                _recorder.Record = false;
                _isRecording = false;
                Debug.Log($"[DemoRecorder] Stopped recording. {_recordedSteps} steps saved to {demoName}.demo");
            }
#endif
        }

        private void Update()
        {
            if (!_isRecording) return;
            _recordedSteps++;

            if (_recordedSteps >= maxRecordSteps)
            {
                StopRecording();
            }
        }

        private void OnDestroy()
        {
            if (_isRecording) StopRecording();
        }
    }
}
