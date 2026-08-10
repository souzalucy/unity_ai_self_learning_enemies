using System.Collections.Generic;

namespace SelfLearningEnemies.BT
{
    /// <summary>
    /// Runs children in sequence. Fails on first child failure.
    /// Succeeds when all children succeed. Returns Running while a child is running.
    /// </summary>
    public class BTSequence : BTNode
    {
        private readonly List<BTNode> _children = new List<BTNode>();
        private int _currentIndex;

        public BTSequence(string name = null, params BTNode[] children) : base(name)
        {
            _children.AddRange(children);
        }

        public void AddChild(BTNode node) => _children.Add(node);

        public override BTStatus Tick()
        {
            while (_currentIndex < _children.Count)
            {
                BTStatus status = _children[_currentIndex].Tick();

                if (status == BTStatus.Failure)
                {
                    _currentIndex = 0;
                    return BTStatus.Failure;
                }

                if (status == BTStatus.Running)
                    return BTStatus.Running;

                _currentIndex++;
            }

            _currentIndex = 0;
            return BTStatus.Success;
        }

        public override void Reset()
        {
            _currentIndex = 0;
            foreach (var child in _children) child.Reset();
        }
    }

    /// <summary>
    /// Runs children in order. Succeeds on first child success.
    /// Fails when all children fail. Returns Running while a child is running.
    /// </summary>
    public class BTSelector : BTNode
    {
        private readonly List<BTNode> _children = new List<BTNode>();
        private int _currentIndex;

        public BTSelector(string name = null, params BTNode[] children) : base(name)
        {
            _children.AddRange(children);
        }

        public void AddChild(BTNode node) => _children.Add(node);

        public override BTStatus Tick()
        {
            while (_currentIndex < _children.Count)
            {
                BTStatus status = _children[_currentIndex].Tick();

                if (status == BTStatus.Success)
                {
                    _currentIndex = 0;
                    return BTStatus.Success;
                }

                if (status == BTStatus.Running)
                    return BTStatus.Running;

                _currentIndex++;
            }

            _currentIndex = 0;
            return BTStatus.Failure;
        }

        public override void Reset()
        {
            _currentIndex = 0;
            foreach (var child in _children) child.Reset();
        }
    }
}
