namespace Simulation.Events
{
    /// <summary>
    /// Event to represent a unit spending an action
    /// </summary>
    public class UnitSpentActionEvent : IGameEvent
    {
        /// <summary>
        /// ID of the affected unit.
        /// </summary>
        public uint UnitId { get; }

        /// <summary>
        /// Previous actions of the affected unit.
        /// </summary>
        public uint OldActions { get; }

        /// <summary>
        /// New actions of the affected unit.
        /// </summary>
        public uint NewActions { get; }

        /// <summary>
        /// Constructor for <see cref="UnitSpentActionEvent"/>.
        /// </summary>
        /// <param name="unitId"></param>
        /// <param name="oldActions"></param>
        /// <param name="newActions"></param>
        public UnitSpentActionEvent(uint unitId, uint oldActions, uint newActions)
        {
            UnitId = unitId;
            OldActions = oldActions;
            NewActions = newActions;
        }
    }
}
