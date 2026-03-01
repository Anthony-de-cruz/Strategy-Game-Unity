namespace GameLogic.Events
{
    /// <summary>
    /// Event to represent a unit being damaged.
    /// </summary>
    public class UnitDamagedEvent : IGameEvent
    {
        /// <summary>
        /// ID of the affected unit.
        /// </summary>
        public uint UnitId { get; }

        /// <summary>
        /// Previous strength of the affected unit.
        /// </summary>
        public uint OldStrength { get; }

        /// <summary>
        /// New strength of the affected unit.
        /// </summary>
        public uint NewStrength { get; }

        /// <summary>
        /// Constructor for <see cref="UnitDamagedEvent"/>.
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        public UnitDamagedEvent(uint unitId, uint oldStrength, uint newStrength)
        {
            UnitId = unitId;
            OldStrength = oldStrength;
            NewStrength = newStrength;
        }
    }
}
