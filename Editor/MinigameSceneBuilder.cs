using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SelfLearningEnemies.Minigames.Editor
{
    /// <summary>
    /// One-click scene generator. Creates a complete, playable minigame scene with an
    /// arena builder + MinigameManager + settings/profile assets wired together. Entities
    /// are auto-composed at runtime by MinigameComposer, so no prefab wiring is required.
    /// </summary>
    public static class MinigameSceneBuilder
    {
        private const string SceneDir = "Assets/SelfLearningEnemies/Minigames";
        private const string SettingsDir = "Assets/SelfLearningEnemies/Minigames/Settings";

        [MenuItem("Tools/Self-Learning Enemies/Minigames/Build RPG Arena Scene")]
        public static void BuildRPGArena() => BuildScene(EnemyGenre.RPG);

        [MenuItem("Tools/Self-Learning Enemies/Minigames/Build Shooter Arena Scene")]
        public static void BuildShooterArena() => BuildScene(EnemyGenre.Shooter);

        [MenuItem("Tools/Self-Learning Enemies/Minigames/Build Racing Track Scene")]
        public static void BuildRacingTrack() => BuildScene(EnemyGenre.Racing);

        private static void BuildScene(EnemyGenre genre)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = CreateSettings(genre);

            var builderGo = new GameObject($"{genre} Arena Builder");
            TrainingArenaBuilder builder;
            switch (genre)
            {
                case EnemyGenre.RPG:
                    builder = builderGo.AddComponent<RPGArenaBuilder>();
                    break;
                case EnemyGenre.Shooter:
                    builder = builderGo.AddComponent<ShooterArenaBuilder>();
                    break;
                default:
                    builder = builderGo.AddComponent<RacingTrackBuilder>();
                    builder.arenaSize = new Vector2(80f, 60f);
                    break;
            }
            builder.enemyCount = settings.enemyCount;

            var managerGo = new GameObject("MinigameManager");
            var manager = managerGo.AddComponent<MinigameManager>();
            manager.settings = settings;
            manager.arenaBuilder = builder;
            managerGo.AddComponent<MinigameHUD>();

            EnsureCamera();
            EnsureLight();

            Directory.CreateDirectory(SceneDir);
            string scenePath = $"{SceneDir}/{genre}Minigame.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[MinigameSceneBuilder] Built {genre} minigame scene at {scenePath}. " +
                      "Assign prefabs in MinigameSettings, or leave them null to auto-compose at runtime. " +
                      "For RPG/Shooter, bake a NavMesh before playing.");
        }

        private static MinigameSettings CreateSettings(EnemyGenre genre)
        {
            Directory.CreateDirectory(SettingsDir);
            string path = $"{SettingsDir}/{genre}MinigameSettings.asset";

            var existing = AssetDatabase.LoadAssetAtPath<MinigameSettings>(path);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<MinigameSettings>();
            settings.genre = genre;
            settings.genreProfile = CreateProfile(genre);

            switch (genre)
            {
                case EnemyGenre.RPG:
                    settings.enemyCount = 3;
                    settings.timeLimitSeconds = 120f;
                    settings.wavesToSurvive = 3;
                    break;
                case EnemyGenre.Shooter:
                    settings.enemyCount = 4;
                    settings.timeLimitSeconds = 90f;
                    break;
                case EnemyGenre.Racing:
                    settings.enemyCount = 3;
                    settings.timeLimitSeconds = 180f;
                    settings.lapsToWin = 3;
                    break;
            }

            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static GenreProfile CreateProfile(EnemyGenre genre)
        {
            string path = $"{SettingsDir}/{genre}Profile.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GenreProfile>(path);
            if (existing != null) return existing;

            GenreProfile profile = genre switch
            {
                EnemyGenre.RPG => GenreProfile.CreateRPGDefaults(),
                EnemyGenre.Shooter => GenreProfile.CreateShooterDefaults(),
                _ => GenreProfile.CreateRacingDefaults()
            };

            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        private static void EnsureLight()
        {
            var light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
