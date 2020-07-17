namespace BaseWPFLibrary.Events
{
    public class BindEndCommandEventArgs
    {
        public BindEndCommandEventArgs(string inTitle, string inMessage, object inSender = null)
        {
            Title = inTitle;
            Message = inMessage;

            Sender = inSender;
        }

        public BindEndCommandEventArgs(string inTitle, string inMessage, object inSender, object inEventData = null)
        {
            Title = inTitle;
            Message = inMessage;

            Sender = inSender;
            EventData = inEventData;
        }


        /// <summary>
        /// A end message without message arguments.
        /// </summary>
        /// <param name="inSender">Sending object  - normally "this".</param>
        /// <param name="inEventData">Generic event data - Don't use to send messages.</param>
        public BindEndCommandEventArgs(object inSender, object inEventData = null)
        {
            Title = null;
            Message = null;

            Sender = inSender;
            EventData = inEventData;
        }


        public string Title { get; }
        public string Message { get; }

        public object Sender { get; }
        public object EventData { get; }
    }
}
