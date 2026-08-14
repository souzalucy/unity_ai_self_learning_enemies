using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEngine;

namespace SelfLearningEnemies.Editor
{
    [CustomEditor(typeof(EnemyBrain))]
    public class EnemyBrainEditor : UnityEditor.Editor
    {
        private EnemyBrain _brain;
        private void OnEnable() { _brain = (EnemyBrain)target; }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10);

            _brain.CacheComponents();
            DrawStatus();
            DrawBehaviorParameters();
            DrawActions();
            DrawProfiles();
        }

        private void DrawStatus()
        {
            int obsSize = _brain.GetTotalObservationSize();
            var obsSources = _brain.GetComponents<ObservationSource>();
            var actEffects = _brain.GetComponents<ActionEffect>();
            var rwdSources = _brain.GetComponents<RewardSource>();

            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Observation Sources", $"{obsSources.Length} attached ({obsSize} floats)");
            foreach (var s in obsSources)
                DrawIndentedLabel($"  {s.SourceName}  {s.ObservationSize} floats");

            EditorGUILayout.Space(3);
            var allBranchSizes = new List<int>();
            int contActions = 0;
            foreach (var a in actEffects)
            {
                contActions += a.ContinuousActionCount;
                if (a.DiscreteBranchSizes != null) allBranchSizes.AddRange(a.DiscreteBranchSizes);
            }
            EditorGUILayout.LabelField("Action Effects", $"{actEffects.Length} attached ({allBranchSizes.Count} branches, {contActions} cont)");
            foreach (var a in actEffects)
                DrawIndentedLabel(BuildActionLabel(a));

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Reward Sources", $"{rwdSources.Length} attached");
            foreach (var r in rwdSources)
                DrawIndentedLabel($"  {r.SourceName} (wt: {r.RewardWeight:F1})");
        }

        private static void DrawIndentedLabel(string text)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
        }

        private static string BuildActionLabel(ActionEffect a)
        {
            string ds = a.DiscreteBranchSizes != null && a.DiscreteBranchSizes.Length > 0
                ? $" [{string.Join(", ", a.DiscreteBranchSizes)}]"
                : "";
            return $"  {a.EffectName}  {a.DiscreteBranchCount} disc{ds}, {a.ContinuousActionCount} cont";
        }

        private void DrawBehaviorParameters()
        {
            var bp = _brain.GetComponent<BehaviorParameters>();
            if (bp == null) return;

            EditorGUILayout.Space(5);
            int obsSize = _brain.GetTotalObservationSize();
            var bpParams = bp.BrainParameters;
            var actionSpec = bpParams.ActionSpec;
            EditorGUILayout.LabelField("BehaviorParameters", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Obs: {bpParams.VectorObservationSize}  Branches: {actionSpec.NumDiscreteActions}  Cont: {actionSpec.NumContinuousActions}");
            if (bpParams.VectorObservationSize != obsSize)
                EditorGUILayout.HelpBox($"Obs size mismatch! BP expects {bpParams.VectorObservationSize}, components report {obsSize}. Click Auto-Configure.", MessageType.Warning);
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate", GUILayout.Height(25))) _brain.ValidateSetup();
            if (GUILayout.Button("Refresh", GUILayout.Height(25))) { _brain.CacheComponents(); Repaint(); }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Auto-Configure BehaviorParameters", GUILayout.Height(30)))
                AutoConfigureBP();
        }

        private void DrawProfiles()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Profiles", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("RPG")) CreateProfile(GenreProfile.CreateRPGDefaults(), "RPG_Profile");
            if (GUILayout.Button("Shooter")) CreateProfile(GenreProfile.CreateShooterDefaults(), "Shooter_Profile");
            if (GUILayout.Button("Racing")) CreateProfile(GenreProfile.CreateRacingDefaults(), "Racing_Profile");
            EditorGUILayout.EndHorizontal();
        }

        private void AutoConfigureBP()
        {
            _brain.CacheComponents();
            var bp = _brain.GetComponent<BehaviorParameters>();
            if (bp == null) { Debug.LogError("[EnemyBrainEditor] No BehaviorParameters found."); return; }

            // Keep the behavior name in sync with Training/*.yaml (behaviors.EnemyBrain).
            bp.BehaviorName = EnemyBrain.DefaultBehaviorName;

            int obsSize = _brain.GetTotalObservationSize();
            var actEffects = _brain.GetComponents<ActionEffect>();
            var allSizes = new List<int>();
            int contTotal = 0;
            foreach (var a in actEffects)
            {
                if (a.DiscreteBranchSizes != null) allSizes.AddRange(a.DiscreteBranchSizes);
                contTotal += a.ContinuousActionCount;
            }

            var brainParameters = bp.BrainParameters;
            brainParameters.VectorObservationSize = obsSize;

            var actionSpec = brainParameters.ActionSpec;
            actionSpec.NumContinuousActions = contTotal;
            actionSpec.BranchSizes = allSizes.ToArray();
            brainParameters.ActionSpec = actionSpec;

            EditorUtility.SetDirty(bp);
            Debug.Log($"[EnemyBrainEditor] BP auto-configured: obs={obsSize}, branches={allSizes.Count} [{string.Join(",", allSizes)}], cont={contTotal}");
            Repaint();
        }

        private void CreateProfile(GenreProfile profile, string name)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save", name, "asset", "Choose location.");
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (_brain.genreProfile == null) { _brain.genreProfile = profile; EditorUtility.SetDirty(_brain); }
            Selection.activeObject = profile;
        }

        [MenuItem("Assets/Create/Self-Learning Enemies/RPG Profile", false, 100)]
        private static void CreateRPG() => ProjectWindowUtil.CreateAsset(GenreProfile.CreateRPGDefaults(), "RPG_Profile.asset");
        [MenuItem("Assets/Create/Self-Learning Enemies/Shooter Profile", false, 101)]
        private static void CreateShooter() => ProjectWindowUtil.CreateAsset(GenreProfile.CreateShooterDefaults(), "Shooter_Profile.asset");
        [MenuItem("Assets/Create/Self-Learning Enemies/Racing Profile", false, 102)]
        private static void CreateRacing() => ProjectWindowUtil.CreateAsset(GenreProfile.CreateRacingDefaults(), "Racing_Profile.asset");
    }
}
