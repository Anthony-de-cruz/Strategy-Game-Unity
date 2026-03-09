using GameLogic.Events;
using System.Net.NetworkInformation;

namespace GameLogic
{
    /// <summary>
    /// 
    /// </summary>
    public enum TurnState
    {
        Init,
        BlueTurn,
        BlueAction,
        BlueVictory,
        RedTurn,
        RedAction,
        RedVictory
    }

    /// <summary>
    /// 
    /// </summary>
    public static class TurnStateExt
    {
        /// <summary>
        /// Get <see cref="TurnState"/> string from <paramref name="turnState"/>.
        /// </summary>
        /// <param name="turnState"></param>
        /// <returns></returns>
        public static string ToString(this TurnState turnState) => turnState switch
        {
            TurnState.Init => "",
            TurnState.BlueTurn => "Blufor Turn",
            TurnState.RedTurn => "Redfor Turn",
            TurnState.BlueAction => "Blufor Action...",
            TurnState.RedAction => "Redfor Action...",
            TurnState.BlueVictory => "Blufor Victory!",
            TurnState.RedVictory => "Redfor Victory!",
            _ => turnState.ToString()
        };
    }

    /// <summary>
    /// 
    /// </summary>
    public class TurnStateMachine
    {
        /// <summary>
        /// 
        /// </summary>
        public TurnState State
        {
            get => _state;
            private set
            {
                if (_state == value)
                    return;
                _eventBus.Publish(new TurnStateChangeEvent(_state, value, TurnCounter));
                _state = value;
            }
        }
        private TurnState _state = TurnState.Init;

        /// <summary>
        /// 
        /// </summary>
        public int TurnCounter { get; private set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        private readonly EventBus _eventBus;

        /// <summary>
        /// Constructor for <see cref="TurnStateMachine"/>.
        /// </summary>
        /// <param name="eventBus"></param>
        public TurnStateMachine(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Init()
        {
            if (State != TurnState.Init)
                return;
            State = TurnState.BlueTurn;
        }

        /// <summary>
        /// 
        /// </summary>
        public void EndTurn()
        {
            switch (State)
            {
                case TurnState.BlueTurn:
                    State = TurnState.RedTurn;
                    break;
                case TurnState.RedTurn:
                    TurnCounter++;
                    State = TurnState.BlueTurn;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void BeginAction()
        {
            switch (State)
            {
                case TurnState.BlueTurn:
                    State = TurnState.BlueAction;
                    break;
                case TurnState.RedTurn:
                    State = TurnState.RedAction;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void EndAction()
        {
            switch (State)
            {
                case TurnState.BlueAction:
                    State = TurnState.BlueTurn;
                    break;
                case TurnState.RedAction:
                    State = TurnState.RedTurn;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void BlueVictory()
        {
            State = TurnState.BlueVictory;
        }

        /// <summary>
        /// 
        /// </summary>
        public void RedVictory()
        {
            State = TurnState.RedVictory;
        }
    }
}
