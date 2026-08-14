using System;
using System.Collections.Generic;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Lesson definition for curriculum learning.
    /// </summary>
    [System.Serializable]
    public class CurriculumLesson
    {
        public string lessonName = "Lesson 1";
        [Tooltip("Mean cumulative reward needed to advance.")]
        public float completionThreshold = 0.5f;
        [Tooltip("Minimum episodes before advancing.")]
        public int minEpisodes = 10;
        [Tooltip("Recent episodes to average for completion check.")]
        public int windowSize = 20;

        [Header("Environment Parameters")]
        public float enemyCount = 1f;
        public float enemyHealth = 1f;
        public float enemyDamage = 1f;
        public float enemySpeed = 1f;
        public float enemyFireRate = 1f;
        public float playerHealth = 1f;

        [Header("Custom")]
        public string[] customParamNames;
        public float[] customParamValues;
    }

    /// <summary>
    /// Manages curriculum learning by tracking agent performance and advancing through
    /// lessons of increasing difficulty. The active lesson's parameter values are exposed
    /// through GetParameter(key, defaultValue), mirroring ML-Agents'
    /// EnvironmentParameters.GetWithDefault so other game systems can read them.
    ///
    /// Attach to any GameObject. Call ReportEpisodeComplete(episodeReward) at end of each episode.
    /// Hook into OnLessonChanged to adjust spawners, stats, etc.
    /// </summary>
    public class CurriculumManager : MonoBehaviour
    {
        [Header("Curriculum")]
        public CurriculumLesson[] lessons;
        public bool autoAdvance = true;

        [Header("Tracking")]
        public int trackingWindowSize = 50;
        public bool logLessonChanges = true;

        private int _currentLessonIndex;
        private Queue<float> _recentRewards = new Queue<float>();
        private int _episodesInCurrentLesson;
        private readonly Dictionary<string, float> _parameters = new Dictionary<string, float>();

        public System.Action<CurriculumLesson> OnLessonChanged;
        public System.Action<int, CurriculumLesson> OnLessonAdvanced;

        public int CurrentLessonIndex => _currentLessonIndex;
        public CurriculumLesson CurrentLesson =>
            lessons != null && _currentLessonIndex < lessons.Length ? lessons[_currentLessonIndex] : null;


        private void Start()
        {
            if (lessons == null || lessons.Length == 0) return;
            ApplyCurrentLesson();
        }

        public void ReportEpisodeComplete(float episodeReward)
        {
            _recentRewards.Enqueue(episodeReward);
            while (_recentRewards.Count > trackingWindowSize) _recentRewards.Dequeue();
            _episodesInCurrentLesson++;

            if (ShouldCheckCompletion() && HasReachedThreshold()) AdvanceLesson();
        }

        private bool ShouldCheckCompletion()
        {
            if (!autoAdvance || lessons == null || _currentLessonIndex >= lessons.Length - 1) return false;
            var lesson = CurrentLesson;
            return lesson != null && _episodesInCurrentLesson >= lesson.minEpisodes;
        }

        private bool HasReachedThreshold()
        {
            var lesson = CurrentLesson;
            if (lesson == null) return false;

            float sum = 0f; int count = 0;
            foreach (float r in _recentRewards)
            {
                sum += r; count++;
                if (count >= lesson.windowSize) break;
            }
            return count > 0 && sum / count >= lesson.completionThreshold;
        }

        public void AdvanceLesson()
        {
            if (lessons == null || _currentLessonIndex >= lessons.Length - 1) return;
            _currentLessonIndex++; _episodesInCurrentLesson = 0; _recentRewards.Clear();
            ApplyCurrentLesson();
            if (logLessonChanges) Debug.Log($"[CurriculumManager] Lesson {_currentLessonIndex}: {CurrentLesson?.lessonName}");
            OnLessonAdvanced?.Invoke(_currentLessonIndex, CurrentLesson);
        }

        public void ApplyCurrentLesson()
        {
            var lesson = CurrentLesson;
            if (lesson == null) return;
            _parameters["enemy_count"] = lesson.enemyCount;
            _parameters["enemy_health"] = lesson.enemyHealth;
            _parameters["enemy_damage"] = lesson.enemyDamage;
            _parameters["enemy_speed"] = lesson.enemySpeed;
            _parameters["enemy_fire_rate"] = lesson.enemyFireRate;
            _parameters["player_health"] = lesson.playerHealth;
            if (lesson.customParamNames != null)
                for (int i = 0; i < lesson.customParamNames.Length && i < lesson.customParamValues.Length; i++)
                    _parameters[lesson.customParamNames[i]] = lesson.customParamValues[i];
            OnLessonChanged?.Invoke(lesson);
        }

        /// <summary>
        /// Returns the value for the given parameter key set by the current lesson,
        /// or <paramref name="defaultValue"/> if the key has not been set. Mirrors
        /// ML-Agents' EnvironmentParameters.GetWithDefault so game systems can read
        /// lesson values with the same pattern.
        /// </summary>
        public float GetParameter(string key, float defaultValue)
        {
            return _parameters.TryGetValue(key, out float value) ? value : defaultValue;
        }

        public void ResetCurriculum()
        {
            _currentLessonIndex = 0; _episodesInCurrentLesson = 0; _recentRewards.Clear();
            ApplyCurrentLesson();
        }

        public float GetRollingAverageReward()
        {
            if (_recentRewards.Count == 0) return 0f;
            float sum = 0f;
            foreach (float r in _recentRewards) sum += r;
            return sum / _recentRewards.Count;
        }
    }
}
