using GameLogic.Events;

namespace GameLogic
{
    /// <summary>
    /// Represents a team to which a unit belongs to.
    /// </summary>
    public enum UnitTeam
    {
        Blue,
        Red
    }

    /// <summary>
    /// Represents a controllable unit owned by a player.
    /// </summary>
    public enum UnitType
    {
        Infantry,
        Tank
    }

    /// <summary>
    /// Represents a controllable unit owned by a team.
    /// </summary>
    public class Unit
    {
        /// <summary>
        /// 
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// 
        /// </summary>
        public UnitTeam Team { get; }

        /// <summary>
        /// 
        /// </summary>
        public UnitType Type { get; }

        /// <summary>
        /// 
        /// </summary>
        public uint Strength
        {
            get => _strength;
            set
            {
                if (value == _strength)
                    return;
                _eventBus.Publish(new UnitDamagedEvent(Id, _strength, value));
                _strength = value;
            }
        }
        private uint _strength;

        /// <summary>
        /// 
        /// </summary>
        private readonly EventBus _eventBus;

        /// <summary>
        /// Constructor for <see cref="Unit"/>.
        /// </summary>
        /// <param name="id">The unit ID.</param>
        /// <param name="team">The team the unit belongs to.</param>
        /// <param name="type">The type of unit.</param>
        /// <param name="strength">The starting strength.</param>
        /// <param name="eventBus">Event bus to publish to.</param>
        public Unit(uint id, UnitTeam team, UnitType type, uint strength, EventBus eventBus)
        {
            Id = id;
            Team = team;
            Type = type;
            _strength = strength;
            _eventBus = eventBus;
        }
    }
}
