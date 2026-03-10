using System;
using System.Runtime.Serialization;

namespace GameLogic
{
    namespace MyApp.Exceptions
    {
        /// <summary>
        /// Exception thrown when the program reaches a state that should be impossible.
        /// Indicates a logic or invariant violation.
        /// </summary>
        [Serializable]
        public sealed class ImpossibleStateException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ImpossibleStateException"/> class.
            /// </summary>
            public ImpossibleStateException() : base("The program reached a state that should be impossible.") { }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImpossibleStateException"/> class with a custom message.
            /// </summary>
            public ImpossibleStateException(string message) : base(message) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImpossibleStateException"/> class with a custom message and inner exception.
            /// </summary>
            public ImpossibleStateException(string message, Exception innerException) : base(message, innerException) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImpossibleStateException"/> class with serialized data.
            /// </summary>
            private ImpossibleStateException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }
}