namespace GameLogic.Events
{
    /// <summary>
    /// Event to represent a turn state change.
    /// </summary>
    public class TurnStateChangeEvent : IGameEvent
    {
        /// <summary>
        /// Previous turn state.
        /// </summary>
        public TurnState OldState { get; }

        /// <summary>
        /// New turn state.
        /// </summary>
        public TurnState NewState { get; }

        /// <summary>
        /// Constructor for <see cref="TurnStateChangeEvent"/>.
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        public TurnStateChangeEvent(TurnState oldState, TurnState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
}

