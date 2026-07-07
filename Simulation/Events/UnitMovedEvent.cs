namespace Simulation.Events
{
    /// <summary>
    /// Event to represent a unit moving.
    /// </summary>
    public class UnitMovedEvent : IGameEvent
    {
        /// <summary>
        /// ID of the affected unit.
        /// </summary>
        public uint UnitId { get; }

        /// <summary>
        /// Previous coords of the affected unit.
        /// </summary>
        public (uint, uint) OldCoords { get; }

        /// <summary>
        /// New coords of the affected unit.
        /// </summary>
        public (uint, uint) NewCoords { get; }

        /// <summary>
        /// Constructor for <see cref="UnitMovedEvent"/>.
        /// </summary>
        /// <param name="unitId"></param>
        /// <param name="oldCoords"></param>
        /// <param name="newCoords"></param>
        public UnitMovedEvent(uint unitId, (uint, uint) oldCoords, (uint, uint) newCoords)
        {
            UnitId = unitId;
            OldCoords = oldCoords;
            NewCoords = newCoords;
        }
    }
}
