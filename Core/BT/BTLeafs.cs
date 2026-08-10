using System;

namespace SelfLearningEnemies.BT
{
    /// <summary>
    /// Condition node: succeeds if the condition delegate returns true, fails otherwise.
    /// </summary>
    public class BTCondition : BTNode
    {
        private readonly Func<bool> _condition;

        public BTCondition(Func<bool> condition, string name = null) : base(name)
        {
            _condition = condition ?? (() => false);
        }

        public override BTStatus Tick()
        {
            return _condition() ? BTStatus.Success : BTStatus.Failure;
        }
    }

    /// <summary>
    /// Delegating condition node (no-alloc when using a static/anonymous check).
    /// </summary>
    public class BTCondition<T> : BTNode
    {
        private readonly Func<T, bool> _condition;
        private readonly T _context;

        public BTCondition(T context, Func<T, bool> condition, string name = null) : base(name)
        {
            _context = context;
            _condition = condition ?? ((_) => false);
        }

        public override BTStatus Tick()
        {
            return _condition(_context) ? BTStatus.Success : BTStatus.Failure;
        }
    }

    /// <summary>
    /// Leaf action node: executes an action and returns a configured result.
    /// Use BTStatus.Success for one-shot actions, BTStatus.Running for ongoing ones.
    /// </summary>
    public class BTActionNode : BTNode
    {
        private readonly Action _onTick;
        private readonly BTStatus _returnStatus;

        public BTActionNode(Action onTick, BTStatus returnStatus = BTStatus.Success, string name = null)
            : base(name)
        {
            _onTick = onTick ?? (() => { });
            _returnStatus = returnStatus;
        }

        public override BTStatus Tick()
        {
            _onTick();
            return _returnStatus;
        }
    }

    /// <summary>
    /// Inverter decorator: flips Success to Failure and vice versa. Running passes through.
    /// </summary>
    public class BTInverter : BTNode
    {
        private readonly BTNode _child;

        public BTInverter(BTNode child, string name = null) : base(name) { _child = child; }

        public override BTStatus Tick()
        {
            BTStatus s = _child.Tick();
            if (s == BTStatus.Success) return BTStatus.Failure;
            if (s == BTStatus.Failure) return BTStatus.Success;
            return BTStatus.Running;
        }
    }

    /// <summary>
    /// Repeater decorator: repeats the child N times (or indefinitely if N < 0).
    /// Always returns Running until the count is exhausted.
    /// </summary>
    public class BTRepeater : BTNode
    {
        private readonly BTNode _child;
        private readonly int _maxRepeats;
        private int _count;

        public BTRepeater(BTNode child, int maxRepeats = -1, string name = null) : base(name)
        {
            _child = child;
            _maxRepeats = maxRepeats;
        }

        public override BTStatus Tick()
        {
            if (_maxRepeats >= 0 && _count >= _maxRepeats)
            {
                _count = 0;
                return BTStatus.Success;
            }

            BTStatus s = _child.Tick();
            if (s != BTStatus.Running) _count++;
            return BTStatus.Running;
        }

        public override void Reset() { _count = 0; _child.Reset(); }
    }
}
