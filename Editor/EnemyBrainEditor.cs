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
            int obsSize = _brain.GetTotalObservationSize();
            var obsSources = _brain.GetComponents<ObservationSource>();
            var actEffects = _brain.GetComponents<ActionEffect>();
            var rwdSources = _brain.GetComponents<RewardSource>();

            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Observation Sources", $"{obsSources.Length} attached ({obsSize} floats)");
            foreach (var s in obsSources)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"  {s.SourceName}  {s.ObservationSize} floats", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

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
            {
                EditorGUI.indentLevel++;
                string ds = a.DiscreteBranchSizes != null && a.DiscreteBranchSizes.Length > 0
                    ? $" [{string.Join(", ", a.DiscreteBranchSizes)}]" : "";
                EditorGUILayout.LabelField($"  {a.EffectName}  {a.DiscreteBranchCount} disc{ds}, {a.ContinuousActionCount} cont", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Reward Sources", $"{rwdSources.Length} attached");
            foreach (var r in rwdSources)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"  {r.SourceName} (wt: {r.RewardWeight:F1})", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            var bp = _brain.GetComponent<BehaviorParameters>();
            if (bp != null)
            {
                EditorGUILayout.Space(5);
                var bd = bp.BehaviorParametersData;
                EditorGUILayout.LabelField("BehaviorParameters", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Obs: {bd.observationSize}  Branches: {bd.numDiscreteActions}  Cont: {bd.numContinuousActions}");
                if (bd.observationSize != obsSize)
                    EditorGUILayout.HelpBox($"Obs size mismatch! BP expects {bd.observationSize}, components report {obsSize}. Click Auto-Configure.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate", GUILayout.Height(25))) _brain.ValidateSetup();
            if (GUILayout.Button("Refresh", GUILayout.Height(25))) { _brain.CacheComponents(); Repaint(); }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Auto-Configure BehaviorParameters", GUILayout.Height(30)))
                AutoConfigureBP();

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

            int obsSize = _brain.GetTotalObservationSize();
            var actEffects = _brain.GetComponents<ActionEffect>();
            var allSizes = new List<int>();
            int contTotal = 0;
            foreach (var a in actEffects)
            {
                if (a.DiscreteBranchSizes != null) allSizes.AddRange(a.DiscreteBranchSizes);
                contTotal += a.ContinuousActionCount;
            }

            var bpSo = new SerializedObject(bp);
            bpSo.FindProperty("m_BehaviorParametersData.observationSize").intValue = obsSize;
            bpSo.FindProperty("m_BehaviorParametersData.numDiscreteActions").intValue = allSizes.Count;
            bpSo.FindProperty("m_BehaviorParametersData.numContinuousActions").intValue = contTotal;

            var branchProp = bpSo.FindProperty("m_BehaviorParametersData.discreteActionBranchSizes");
            branchProp.arraySize = allSizes.Count;
            for (int i = 0; i < allSizes.Count; i++)
                branchProp.GetArrayElementAtIndex(i).intValue = allSizes[i];

            bpSo.ApplyModifiedProperties();
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
