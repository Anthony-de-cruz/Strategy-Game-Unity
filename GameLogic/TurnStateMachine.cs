using GameLogic.Events;

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
    public class TurnStateMachine
    {
        public TurnState State
        {
            get => _state;
            private set
            {
                if (_state == value)
                    return;
                _eventBus.Publish(new TurnStateChangeEvent(_state, value));
                _state = value;
            }
        }
        private TurnState _state = TurnState.Init;

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
