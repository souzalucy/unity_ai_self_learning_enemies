using System.Collections.Generic;
using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Stateless factory for spawning the player and enemies in the minigames,
    /// extracted from MinigameManager. Keeps the manager focused on game-loop state.
    /// </summary>
    public static class EnemySpawner
    {
        public static PlayerStatus SpawnPlayer(MinigameSettings settings, Vector3 spawnOffset)
        {
            GameObject go;
            if (settings.playerPrefab != null)
            {
                go = Object.Instantiate(settings.playerPrefab, spawnOffset, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(settings.genre == EnemyGenre.Racing ? PrimitiveType.Cube : PrimitiveType.Capsule);
                go.transform.position = spawnOffset;
            }
            go.name = "Player";
            MinigameComposer.ConfigurePlayer(go, settings);
            return go.GetComponent<PlayerStatus>();
        }

        public static List<EnemyBrain> SpawnEnemies(MinigameSettings settings, TrainingArenaBuilder arenaBuilder, Transform player)
        {
            var enemies = new List<EnemyBrain>();
            Vector3[] spawns = ComputeEnemySpawns(settings, arenaBuilder, settings.enemyCount);

            for (int i = 0; i < settings.enemyCount; i++)
            {
                Vector3 pos = i < spawns.Length ? spawns[i] : spawns[0] + Vector3.right * i * 2f;

                GameObject go;
                if (settings.enemyPrefab != null)
                {
                    go = Object.Instantiate(settings.enemyPrefab, pos, Quaternion.identity);
                }
                else
                {
                    go = GameObject.CreatePrimitive(settings.genre == EnemyGenre.Racing ? PrimitiveType.Cube : PrimitiveType.Capsule);
                    go.transform.position = pos;
                }
                go.name = $"Enemy_{i}";

                var brain = MinigameComposer.ConfigureEnemy(go, settings, player);
                if (brain != null) enemies.Add(brain);
            }
            return enemies;
        }

        public static Vector3[] ComputeEnemySpawns(MinigameSettings settings, TrainingArenaBuilder arenaBuilder, int count)
        {
            if (arenaBuilder is RacingTrackBuilder track && track.Waypoints != null && track.Waypoints.Count > 0)
                return ComputeTrackSpawns(track, count);

            Vector2 size = arenaBuilder != null ? arenaBuilder.arenaSize : new Vector2(50f, 50f);
            return ComputeRingSpawns(size, count);
        }

        private static Vector3[] ComputeTrackSpawns(RacingTrackBuilder track, int count)
        {
            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                int idx = (i * track.Waypoints.Count / Mathf.Max(1, count)) % track.Waypoints.Count;
                result[i] = track.Waypoints[idx].position + Vector3.right * (i % 2 == 0 ? 2f : -2f);
            }
            return result;
        }

        private static Vector3[] ComputeRingSpawns(Vector2 size, int count)
        {
            float radius = Mathf.Min(size.x, size.y) * 0.35f;
            var spawns = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? (360f / count) * i : 0f;
                spawns[i] = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, 1f, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            }
            return spawns;
        }
    }
}
