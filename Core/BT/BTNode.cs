namespace SelfLearningEnemies.BT
{
    public enum BTStatus { Success, Failure, Running }

    /// <summary>
    /// Base class for all Behavior Tree nodes.
    /// </summary>
    public abstract class BTNode
    {
        protected string _name;
        public string NodeName => _name ?? GetType().Name;

        public BTNode(string name = null) { _name = name; }
        public abstract BTStatus Tick();

        /// <summary>Override to reset any internal state when the node is re-entered.</summary>
        public virtual void Reset() { }
    }
}
