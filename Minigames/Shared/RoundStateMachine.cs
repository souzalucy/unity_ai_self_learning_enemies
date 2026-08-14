using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Owns the per-round win/lose state (score, wave, timer, game-over flag),
    /// extracted from MinigameManager so the manager stays a thin coordinator.
    /// Serialized so the values remain visible in the inspector.
    /// </summary>
    [System.Serializable]
    public class RoundStateMachine
    {
        [SerializeField] private int _score;
        [SerializeField] private int _currentWave = 1;
        [SerializeField] private float _timeRemaining;
        [SerializeField] private bool _gameOver;
        [SerializeField] private bool _playerWon;

        public int Score => _score;
        public int CurrentWave => _currentWave;
        public float TimeRemaining => _timeRemaining;
        public bool IsGameOver => _gameOver;
        public bool PlayerWon => _playerWon;

        public void BeginRound(float timeLimit)
        {
            _timeRemaining = timeLimit;
            _gameOver = false;
            _playerWon = false;
        }

        /// <summary>Advances the round timer. Returns true when it has just expired.</summary>
        public bool Tick(float deltaTime, bool hasTimeLimit)
        {
            if (_gameOver || !hasTimeLimit) return false;
            _timeRemaining -= deltaTime;
            return _timeRemaining <= 0f;
        }

        public void AddScore() => _score++;

        public bool CanAdvanceWave(int wavesToSurvive) => _currentWave < wavesToSurvive;

        public void AdvanceWave() => _currentWave++;

        public void End(bool playerWon)
        {
            _gameOver = true;
            _playerWon = playerWon;
        }

        public void ResetScore()
        {
            _score = 0;
            _currentWave = 1;
        }
    }
}
