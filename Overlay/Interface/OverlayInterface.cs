using System;
using Overlay.Elements;

namespace Overlay.Interface
{
    [Serializable]
    public delegate void MessageReceivedEvent(MessageReceivedEventArgs message);

    [Serializable]
    public delegate void DisconnectedEvent();

    [Serializable]
    public delegate void DisplayTextEvent(DisplayTextEventArgs args);

    [Serializable]
    public class OverlayInterface : MarshalByRefObject
    {
        /// <summary>
        /// The progress bar width, in percentage
        /// </summary>
        public int ProgressPercentage { get; private set; }

        /// <summary>
        /// The main PercentageText
        /// </summary>
        public TextElement PercentageText { private get; set; }

        #region Events

        #region Server-side Events

        /// <summary>
        /// Server event for sending debug and error information from the client to server
        /// </summary>
        public event MessageReceivedEvent RemoteMessage;

        #endregion

        #region Client-side Events

        /// <summary>
        /// Client event used to notify the hook to exit
        /// </summary>
        public event DisconnectedEvent Disconnected;

        /// <summary>
        /// Client event used to display a piece of text in-game
        /// </summary>
        public event DisplayTextEvent DisplayText;

        #endregion

        #endregion

        #region Public Methods

        #region Still image Capture

        /// <summary>
        /// Sets the main textElement text
        /// </summary>
        /// <param name="fps"></param>
        public void SetText(string fps)
        {
            try
            {
                PercentageText.Text = fps;
            }
            catch
            {
                Message(MessageType.Warning, "Failed to set DirectX text");
            }
        }


        /// <summary>
        /// Sets the progress bar width
        /// </summary>
        /// <param name="progress"></param>
        public void SetProgress(int progress)
        {
            ProgressPercentage = progress;
        }

        #endregion

        /// <summary>
        /// Tell the client process to disconnect
        /// </summary>
        public void Disconnect()
        {
            SafeInvokeDisconnected();
        }

        /// <summary>
        /// Send a message to all handlers of <see cref="OverlayInterface.RemoteMessage"/>.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Message(MessageType messageType, string format, params object[] args)
        {
            Message(messageType, string.Format(format, args));
        }

        public void Message(MessageType messageType, string message)
        {
            SafeInvokeMessageRecevied(new MessageReceivedEventArgs(messageType, message));
        }

        /// <summary>
        /// Display text in-game for the default duration of 5 seconds
        /// </summary>
        /// <param name="text"></param>
        public void DisplayInGameText(string text)
        {
            DisplayInGameText(text, new TimeSpan(0, 0, 5));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="duration"></param>
        public void DisplayInGameText(string text, TimeSpan duration)
        {
            if (duration.TotalMilliseconds <= 0)
                throw new ArgumentException("Duration must be larger than 0", nameof(duration));
            SafeInvokeDisplayText(new DisplayTextEventArgs(text, duration));
        }

        #endregion

        #region Private: Invoke message handlers


        private void SafeInvokeMessageRecevied(MessageReceivedEventArgs eventArgs)
        {
            if (RemoteMessage == null)
                return; //No Listeners

            MessageReceivedEvent listener = null;
            var dels = RemoteMessage.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (MessageReceivedEvent) del;
                    listener.Invoke(eventArgs);
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    RemoteMessage -= listener;
                }
            }
        }


        private void SafeInvokeDisconnected()
        {
            if (Disconnected == null)
                return; //No Listeners

            DisconnectedEvent listener = null;
            var dels = Disconnected.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (DisconnectedEvent) del;
                    listener.Invoke();
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    Disconnected -= listener;
                }
            }
        }

        private void SafeInvokeDisplayText(DisplayTextEventArgs displayTextEventArgs)
        {
            if (DisplayText == null)
                return; //No Listeners

            DisplayTextEvent listener = null;
            var dels = DisplayText.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (DisplayTextEvent) del;
                    listener.Invoke(displayTextEventArgs);
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    DisplayText -= listener;
                }
            }
        }

        #endregion

        /// <summary>
        /// Used 
        /// </summary>
        public void Ping()
        {
        }
    }
}