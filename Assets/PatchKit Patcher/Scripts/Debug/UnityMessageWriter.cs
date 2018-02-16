using System;
using JetBrains.Annotations;
using PatchKit.Logging;

namespace PatchKit.Unity.Patcher.Debug
{
    public class UnityMessageWriter : IMessagesStreamObserver
    {
        [NotNull]
        private readonly IMessageFormatter _formatter;

        public UnityMessageWriter([NotNull] IMessageFormatter formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException("formatter");
            }

            _formatter = formatter;
        }

        public void OnNext(Message message, MessageContext context)
        {
            var text = _formatter.Format(message, context);

            switch (message.Type)
            {
                case MessageType.Trace:
                    UnityEngine.Debug.Log(text);
                    break;
                case MessageType.Debug:
                    UnityEngine.Debug.Log(text);
                    break;
                case MessageType.Warning:
                    UnityEngine.Debug.LogWarning(text);
                    break;
                case MessageType.Error:
                    UnityEngine.Debug.LogError(text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}