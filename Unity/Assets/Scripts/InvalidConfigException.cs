using System;
using System.Runtime.Serialization;

namespace Assets.Scripts
{
    /// <summary>
    /// Exception thrown when a unity script has a bad configuration via the Unity editor.
    /// Indicates bad state.
    /// </summary>
    [Serializable]
    public sealed class InvalidConfigException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidConfigException"/> class.
        /// </summary>
        public InvalidConfigException() : base("Bad script configuration state.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidConfigException"/> class with a custom message.
        /// </summary>
        public InvalidConfigException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidConfigException"/> class with a custom message and inner exception.
        /// </summary>
        public InvalidConfigException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidConfigException"/> class with serialized data.
        /// </summary>
        private InvalidConfigException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}