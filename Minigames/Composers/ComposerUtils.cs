using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Shared helpers for MinigameComposer and the per-genre composer classes.
    /// </summary>
    public static class ComposerUtils
    {
        /// <summary>Returns the existing component of type T on <paramref name="go"/>, adding one if missing.</summary>
        public static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
