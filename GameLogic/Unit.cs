using System;

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
        public uint Actions
        {
            get => _actions;
            set
            {
                if (value == _actions) return;
                _eventBus.Publish(new UnitSpentActionEvent(Id, _actions, value));
                _actions = value;
            }
        }
        private uint _actions;

        /// <summary>
        /// 
        /// </summary>
        public uint Strength { get; set; }

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
        /// <param name="eventBus">Event bus to publish to.</param>
        public Unit(uint id, UnitTeam team, UnitType type, EventBus eventBus)
        {
            Id = id;
            Team = team;
            Type = type;
            _eventBus = eventBus;

            Strength = type switch
            {
                UnitType.Infantry => 5,
                UnitType.Tank => 10,
                _ => throw new NotImplementedException($"Unhandled unit type: {type}."),
            };
            _actions = 2;
        }

        /// <summary>
        ///     Reset
        /// </summary>
        public void ResetActions()
        {
            Actions = 2;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unitType"></param>
        /// <returns></returns>
        /// <exception cref="ImpossibleStateException"></exception>
        public static uint GetMovementByType(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Infantry => 6,
                UnitType.Tank => 8,
                _ => throw new ImpossibleStateException()
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public static uint GetRangeByType(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Infantry => 4,
                UnitType.Tank => 4,
                _ => throw new ImpossibleStateException()
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unitType"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static uint GetDamageByType(UnitType unitType, UnitType targetType)
        {
            return unitType switch
            {
                UnitType.Infantry => targetType switch
                {
                    UnitType.Infantry => 2,
                    UnitType.Tank => 2,
                    _ => throw new ImpossibleStateException()
                },
                UnitType.Tank => targetType switch
                {
                    UnitType.Infantry => 3,
                    UnitType.Tank => 5,
                    _ => throw new ImpossibleStateException()
                },
                _ => throw new ImpossibleStateException()
            };
        }
    }

    /// <summary>
    /// Readonly snapshot of a unit.
    /// Used for frontend state queries, AI drivers and tests.
    /// </summary>
    public readonly struct UnitView
    {
        public readonly uint Id;
        public readonly UnitTeam Team;
        public readonly UnitType Type;
        public readonly uint Strength;
        public readonly uint Actions;
        public readonly uint X;
        public readonly uint Y;

        public UnitView(Unit unit, uint xCoord, uint yCoord)
        {
            Id = unit.Id;
            Team = unit.Team;
            Type = unit.Type;
            Strength = unit.Strength;
            Actions = unit.Actions;
            X = xCoord;
            Y = yCoord;
        }
    }
}
