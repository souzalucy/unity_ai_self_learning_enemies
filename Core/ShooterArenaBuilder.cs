using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Shooter-style training arena with cover walls, barricades, and elevated positions.
    /// Right-click → Build Arena to generate.
    /// </summary>
    public class ShooterArenaBuilder : TrainingArenaBuilder
    {
        [Header("Cover Objects")]
        [Tooltip("Number of cover walls scattered around.")]
        public int coverWallCount = 6;

        [Tooltip("Dimensions of each cover wall.")]
        public Vector3 coverWallSize = new Vector3(4f, 2f, 0.5f);

        [Tooltip("Number of low barricades.")]
        public int barricadeCount = 4;

        [Tooltip("Barricade dimensions (low cover).")]
        public Vector3 barricadeSize = new Vector3(3f, 1f, 1f);

        [Tooltip("Number of elevated platforms.")]
        public int elevatedPlatformCount = 2;

        [Tooltip("Platform dimensions.")]
        public Vector3 platformSize = new Vector3(4f, 0.3f, 4f);

        [Tooltip("Platform height above ground.")]
        public float platformHeight = 3f;

        [Tooltip("Material for cover objects.")]
        public Material coverMaterial;

        protected override void BuildGenreFeatures()
        {
            var objects = new GameObject("CoverObjects");
            objects.transform.SetParent(_arenaRoot.transform);

            BuildCoverWalls(objects.transform);
            BuildBarricades(objects.transform);
            BuildPlatforms(objects.transform);
        }

        private void BuildCoverWalls(Transform parent)
        {
            float hw = arenaSize.x * 0.35f, hz = arenaSize.y * 0.35f;
            for (int i = 0; i < coverWallCount; i++)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"CoverWall_{i}";
                wall.transform.SetParent(parent);

                Vector3 pos = new Vector3(Random.Range(-hw, hw), coverWallSize.y * 0.5f, Random.Range(-hz, hz));
                // Randomly rotate for variety
                float rotY = Random.Range(0f, 180f);
                wall.transform.localPosition = pos;
                wall.transform.localRotation = Quaternion.Euler(0, rotY, 0);
                wall.transform.localScale = coverWallSize;
                wall.tag = "Cover";
                if (coverMaterial != null) wall.GetComponent<Renderer>().material = coverMaterial;
            }
        }

        private void BuildBarricades(Transform parent)
        {
            float hw = arenaSize.x * 0.3f, hz = arenaSize.y * 0.3f;
            for (int i = 0; i < barricadeCount; i++)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = $"Barricade_{i}";
                b.transform.SetParent(parent);
                Vector3 pos = new Vector3(Random.Range(-hw, hw), barricadeSize.y * 0.5f, Random.Range(-hz, hz));
                b.transform.localPosition = pos;
                b.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 180f), 0);
                b.transform.localScale = barricadeSize;
                b.tag = "Cover";
                if (coverMaterial != null) b.GetComponent<Renderer>().material = coverMaterial;
            }
        }

        private void BuildPlatforms(Transform parent)
        {
            float hw = arenaSize.x * 0.25f, hz = arenaSize.y * 0.25f;
            for (int i = 0; i < elevatedPlatformCount; i++)
            {
                var plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plat.name = $"Platform_{i}";
                plat.transform.SetParent(parent);
                Vector3 pos = new Vector3(Random.Range(-hw, hw), platformHeight, Random.Range(-hz, hz));
                plat.transform.localPosition = pos;
                plat.transform.localScale = platformSize;
                if (coverMaterial != null) plat.GetComponent<Renderer>().material = coverMaterial;

                // Add ramp
                var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = $"Ramp_{i}";
                ramp.transform.SetParent(plat.transform);
                ramp.transform.localPosition = new Vector3(0, -0.5f, platformSize.z * 0.5f + 1f);
                ramp.transform.localRotation = Quaternion.Euler(30f, 0, 0);
                ramp.transform.localScale = new Vector3(platformSize.x, 0.2f, 3f);
                if (coverMaterial != null) ramp.GetComponent<Renderer>().material = coverMaterial;
            }
        }
    }
}
