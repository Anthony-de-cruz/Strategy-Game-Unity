namespace GameLogic.Events
{
    /// <summary>
    /// Event to represent a unit being damaged.
    /// </summary>
    public class UnitAttackedEvent : IGameEvent
    {
        /// <summary>
        /// ID of the attacking unit.
        /// </summary>
        public uint AttackerId { get; }

        /// <summary>
        /// ID of the affected unit.
        /// </summary>
        public uint TargetId { get; }

        /// <summary>
        /// Previous strength of the affected unit.
        /// </summary>
        public uint OldStrength { get; }

        /// <summary>
        /// New strength of the affected unit.
        /// </summary>
        public uint NewStrength { get; }

        /// <summary>
        /// Constructor for <see cref="UnitAttackedEvent"/>.
        /// </summary>
        /// <param name="attackerId"></param>
        /// <param name="targetId"></param>
        /// <param name="oldStrength"></param>
        /// <param name="newStrength"></param>
        public UnitAttackedEvent(uint attackerId, uint targetId, uint oldStrength, uint newStrength)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            OldStrength = oldStrength;
            NewStrength = newStrength;
        }
    }
}
