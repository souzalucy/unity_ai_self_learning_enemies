using System;
using System.Collections.Generic;
using Unity.MLAgents;
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
    /// lessons of increasing difficulty. Works with ML-Agents' EnvironmentParameters.
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

            if (!autoAdvance || lessons == null || _currentLessonIndex >= lessons.Length - 1) return;
            var lesson = CurrentLesson;
            if (lesson == null || _episodesInCurrentLesson < lesson.minEpisodes) return;

            float sum = 0f; int count = 0;
            foreach (float r in _recentRewards) { sum += r; count++; if (count >= lesson.windowSize) break; }
            if (count > 0 && sum / count >= lesson.completionThreshold) AdvanceLesson();
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
            var env = Academy.Instance.EnvironmentParameters;
            env.SetParameter("enemy_count", lesson.enemyCount);
            env.SetParameter("enemy_health", lesson.enemyHealth);
            env.SetParameter("enemy_damage", lesson.enemyDamage);
            env.SetParameter("enemy_speed", lesson.enemySpeed);
            env.SetParameter("enemy_fire_rate", lesson.enemyFireRate);
            env.SetParameter("player_health", lesson.playerHealth);
            if (lesson.customParamNames != null)
                for (int i = 0; i < lesson.customParamNames.Length && i < lesson.customParamValues.Length; i++)
                    env.SetParameter(lesson.customParamNames[i], lesson.customParamValues[i]);
            OnLessonChanged?.Invoke(lesson);
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
