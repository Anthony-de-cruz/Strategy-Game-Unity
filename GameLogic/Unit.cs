using System;

using GameLogic.Events;
using GameLogic.MyApp.Exceptions;

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
        public uint CurrentActions
        {
            get => _currentActions;
            set
            {
                if (value == _currentActions) return;
                _eventBus.Publish(new UnitSpentActionEvent(Id, _currentActions, value));
                _currentActions = value;
            }
        }
        private uint _currentActions;

        /// <summary>
        ///     The total number of actions this unit can perform per turn.
        /// </summary>
        public uint Actions { get; }

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
        /// <param name="eventBus">Event bus to publish to.</param>
        public Unit(uint id, UnitTeam team, UnitType type, EventBus eventBus)
        {
            Id = id;
            Team = team;
            Type = type;
            _eventBus = eventBus;

            _strength = type switch
            {
                UnitType.Infantry => 5,
                UnitType.Tank => 10,
                _ => throw new NotImplementedException($"Unhandled unit type: {type}."),
            };
            Actions = 2;
        }

        /// <summary>
        ///     Reset
        /// </summary>
        public void ResetActions()
        {
            CurrentActions = Actions;
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
}
