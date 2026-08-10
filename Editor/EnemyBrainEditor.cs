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

            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            _brain.CacheComponents();
            int obsSize = _brain.GetTotalObservationSize();
            var obsSources = _brain.GetComponents<ObservationSource>();
            var actEffects = _brain.GetComponents<ActionEffect>();
            var rwdSources = _brain.GetComponents<RewardSource>();

            EditorGUILayout.LabelField("Observation Sources", $"{obsSources.Length} attached ({obsSize} floats)");
            foreach (var s in obsSources)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"• {s.SourceName} — {s.ObservationSize} floats", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);
            int discBranches = 0, contActions = 0;
            foreach (var a in actEffects)
            {
                discBranches += a.DiscreteBranchCount;
                contActions += a.ContinuousActionCount;
            }
            EditorGUILayout.LabelField("Action Effects", $"{actEffects.Length} attached ({discBranches} discrete, {contActions} continuous)");
            foreach (var a in actEffects)
            {
                EditorGUI.indentLevel++;
                string ds = a.DiscreteBranchSizes != null && a.DiscreteBranchSizes.Length > 0
                    ? $" [{string.Join(", ", a.DiscreteBranchSizes)}]" : "";
                EditorGUILayout.LabelField($"• {a.EffectName} — {a.DiscreteBranchCount} disc{ds}, {a.ContinuousActionCount} cont", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Reward Sources", $"{rwdSources.Length} attached");
            foreach (var r in rwdSources)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"• {r.SourceName} (wt: {r.RewardWeight:F1})", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Validate Setup", GUILayout.Height(25)))
                _brain.ValidateSetup();
            if (GUILayout.Button("Refresh Components", GUILayout.Height(25)))
            { _brain.CacheComponents(); Repaint(); }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Create Profiles", EditorStyles.boldLabel);

        private void CreateProfile(GenreProfile profile, string name)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Genre Profile", name, "asset", "Choose save location.");
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (_brain.genreProfile == null)
            { _brain.genreProfile = profile; EditorUtility.SetDirty(_brain); }
            Debug.Log($"[EnemyBrainEditor] Created: {path}");
            Selection.activeObject = profile;
        }

        [MenuItem("Assets/Create/Self-Learning Enemies/RPG Profile", false, 100)]
        private static void CreateRPGProfileMenu() => ProjectWindowUtil.CreateAsset(GenreProfile.CreateRPGDefaults(), "RPG_Profile.asset");

        [MenuItem("Assets/Create/Self-Learning Enemies/Shooter Profile", false, 101)]
        private static void CreateShooterProfileMenu() => ProjectWindowUtil.CreateAsset(GenreProfile.CreateShooterDefaults(), "Shooter_Profile.asset");

        [MenuItem("Assets/Create/Self-Learning Enemies/Racing Profile", false, 102)]
        private static void CreateRacingProfileMenu() => ProjectWindowUtil.CreateAsset(GenreProfile.CreateRacingDefaults(), "Racing_Profile.asset");
    }
}

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("RPG")) CreateProfile(GenreProfile.CreateRPGDefaults(), "RPG_Profile");
            if (GUILayout.Button("Shooter")) CreateProfile(GenreProfile.CreateShooterDefaults(), "Shooter_Profile");
            if (GUILayout.Button("Racing")) CreateProfile(GenreProfile.CreateRacingDefaults(), "Racing_Profile");
            EditorGUILayout.EndHorizontal();
        }
