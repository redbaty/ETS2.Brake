using System;
using Overlay.Elements;

namespace Overlay.Interface
{
    [Serializable]
    public delegate void MessageReceivedEvent(MessageReceivedEventArgs message);

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

        public bool ShowMemoryUsage { get; set; }

        #region Events

        #region Server-side Events

        /// <summary>
        /// Server event for sending debug and error information from the client to server
        /// </summary>
        public event MessageReceivedEvent RemoteMessage;

        public event EventHandler MemoryVisibilityChanged;

        #endregion

        #endregion

        #region Public Methods

        #region Text methods

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

        public void SetMemoryUsageVisibility(bool visible)
        {
            ShowMemoryUsage = visible;
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

        #endregion

        /// <summary>
        /// Used 
        /// </summary>
        public void Ping()
        {
        }

        protected virtual void OnMemoryVisibilityChanged()
        {
            MemoryVisibilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}