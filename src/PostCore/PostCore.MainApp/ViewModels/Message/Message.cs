namespace PostCore.MainApp.ViewModels.Message
{
    public enum MessageType
    {
        Info,
        Error
    }

    public class MessageViewModel
    {
        public MessageType Type { get; set; }
        public string Message { get; set; }

        public static MessageViewModel MakeInfo(string message)
        {
            return new MessageViewModel
            {
                Type = MessageType.Info,
                Message = message
            };
        }

        public static MessageViewModel MakeError(string message)
        {
            return new MessageViewModel
            {
                Type = MessageType.Error,
                Message = message
            };
        }
    }
}
