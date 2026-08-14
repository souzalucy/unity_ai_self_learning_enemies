using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Minimal IMGUI HUD showing player HP, score/wave/laps, timer, and (optionally)
    /// the AI's current episode reward + last action. No canvas/TextMeshPro required.
    /// </summary>
    public class MinigameHUD : MonoBehaviour
    {
        [Tooltip("Manager to read state from. Auto-found if null.")]
        public MinigameManager manager;

        [Tooltip("Show per-enemy reward + last action readouts.")]
        public bool showEnemyTelemetry = true;

        private void OnGUI()
        {
            if (manager == null) manager = MinigameManager.Instance;
            if (manager == null || manager.settings == null) return;

            GUILayout.BeginArea(new Rect(10f, 10f, 460f, 500f));
            DrawStatus();
            if (showEnemyTelemetry) DrawEnemyTelemetry();
            GUILayout.EndArea();
        }

        private void DrawStatus()
        {
            var settings = manager.settings;
            GUILayout.Label($"<b>{settings.genre} Minigame</b>");

            var player = manager.Player;
            DrawHealth(player);
            DrawScore(settings, player);
            DrawTimer(settings);
            DrawGameOver();

            GUILayout.Label($"Mode: {settings.experimentMode}");
        }

        private void DrawHealth(PlayerStatus player)
        {
            if (player == null) return;
            float frac = player.MaxHealth > 0.001f ? Mathf.Clamp01(player.Health / player.MaxHealth) : 0f;
            GUILayout.BeginHorizontal();
            GUILayout.Label("HP", GUILayout.Width(24f));
            DrawBar(frac, 180f, 16f);
            GUILayout.Label($"{player.Health:F0}/{player.MaxHealth:F0}");
            GUILayout.EndHorizontal();
        }

        private void DrawScore(MinigameSettings settings, PlayerStatus player)
        {
            GUILayout.Space(4f);
            GUILayout.Label($"Score: {manager.Score}");

            switch (settings.genre)
            {
                case EnemyGenre.RPG:
                    GUILayout.Label($"Wave: {manager.CurrentWave}/{settings.wavesToSurvive}");
                    break;
                case EnemyGenre.Racing:
                    int playerLaps = manager.GetLaps(player != null ? player.gameObject : null);
                    GUILayout.Label($"Laps: {playerLaps}/{settings.lapsToWin}");
                    break;
            }
        }

        private void DrawTimer(MinigameSettings settings)
        {
            if (settings.timeLimitSeconds > 0f)
                GUILayout.Label($"Time: {Mathf.Max(0f, manager.TimeRemaining):F1}s");
        }

        private void DrawGameOver()
        {
            if (!manager.IsGameOver) return;
            string msg = manager.PlayerWon ? "VICTORY!" : "DEFEAT";
            GUILayout.Label($"<b><color={(manager.PlayerWon ? "green" : "red")}>{msg}</color></b>");
        }

        private void DrawEnemyTelemetry()
        {
            GUILayout.Space(8f);
            GUILayout.Label("<b>AI telemetry</b>");
            foreach (var brain in manager.Enemies)
            {
                if (brain == null) continue;

                string disc = "[]";
                if (brain.LastDiscreteActions != null)
                    disc = "[" + string.Join(",", brain.LastDiscreteActions) + "]";

                string cont = "[]";
                if (brain.LastContinuousActions != null)
                {
                    var parts = new string[brain.LastContinuousActions.Length];
                    for (int i = 0; i < parts.Length; i++)
                        parts[i] = brain.LastContinuousActions[i].ToString("F2");
                    cont = "[" + string.Join(",", parts) + "]";
                }

                GUILayout.Label($"  {brain.name}: reward {brain.EpisodeReward:F2} | step {brain.EpisodeStep} | disc {disc} cont {cont}");
            }
        }

        private static void DrawBar(float fraction, float width, float height)
        {
            Rect bg = GUILayoutUtility.GetRect(width, height);
            GUI.color = Color.black;
            GUI.DrawTexture(bg, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, fraction);
            GUI.DrawTexture(new Rect(bg.x, bg.y, bg.width * Mathf.Clamp01(fraction), bg.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
