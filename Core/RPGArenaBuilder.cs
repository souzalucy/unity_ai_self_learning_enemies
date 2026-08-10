using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// RPG-style training arena with obstacles, pillars, and a central combat area.
    /// Right-click → Build Arena to generate.
    /// </summary>
    public class RPGArenaBuilder : TrainingArenaBuilder
    {
        [Header("RPG Features")]
        [Tooltip("Number of pillars/obstacles in the arena.")]
        public int obstacleCount = 8;

        [Tooltip("Width of each pillar.")]
        public float pillarWidth = 1.5f;

        [Tooltip("Height of each pillar.")]
        public float pillarHeight = 4f;

        [Tooltip("Material for pillars.")]
        public Material pillarMaterial;

        protected override void BuildGenreFeatures()
        {
            var obstacles = new GameObject("Obstacles");
            obstacles.transform.SetParent(_arenaRoot.transform);

            float hw = arenaSize.x * 0.35f;
            float hz = arenaSize.y * 0.35f;

            for (int i = 0; i < obstacleCount; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i}";
                pillar.transform.SetParent(obstacles.transform);

                Vector3 pos;
                int att = 0;
                do
                {
                    pos = new Vector3(Random.Range(-hw, hw), pillarHeight * 0.5f, Random.Range(-hz, hz));
                    att++;
                } while (att < 30 && !IsObstacleValid(pos, pillarWidth * 2f));

                pillar.transform.localPosition = pos;
                pillar.transform.localScale = new Vector3(pillarWidth, pillarHeight * 0.5f, pillarWidth);
                if (pillarMaterial != null) pillar.GetComponent<Renderer>().material = pillarMaterial;

                // Tag for cover detection
                pillar.tag = "Cover";
            }
        }

        private bool IsObstacleValid(Vector3 pos, float minDist)
        {
            // Keep away from player spawn
            if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(playerSpawnOffset.x, 0, playerSpawnOffset.z)) < minDist * 2f)
                return false;

            // Keep away from center
            if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), Vector3.zero) < minDist)
                return false;

            return true;
        }
    }
}
