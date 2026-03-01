using System;
using System.Collections.Generic;

namespace GameLogic.Events
{
    public interface IGameEvent { }

    /// <summary>
    /// Generic event bus to forward published events to subscribers.
    /// </summary>
    public class EventBus
    {
        /// <summary>
        /// The mapping of events to the set of event handlers.
        /// </summary>
        private readonly Dictionary<Type, HashSet<Delegate>> _handlers = new Dictionary<Type, HashSet<Delegate>>();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameEvent"></param>
        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (_handlers.TryGetValue(typeof(T), out HashSet<Delegate> handlers))
                foreach (Delegate handler in handlers)
                    ((Action<T>)handler)(gameEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            Type type = typeof(T);
            if (_handlers.TryGetValue(type, out _))
                _handlers[type].Add(handler);
            else
                _handlers.Add(type, new HashSet<Delegate> { handler });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        public void Unsubscribe<T>(Action handler) where T : IGameEvent
        {
            if (_handlers.TryGetValue(typeof(T), out HashSet<Delegate> handlers))
                handlers.Remove(handler);
        }
    }
}
