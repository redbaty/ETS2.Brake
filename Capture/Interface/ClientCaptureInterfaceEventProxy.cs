using System;

namespace Capture.Interface
{
    /// <summary>
    /// Client event proxy for marshalling event handlers
    /// </summary>
    public class ClientCaptureInterfaceEventProxy : MarshalByRefObject
    {
        #region Event Declarations

        /// <summary>
        /// Client event used to communicate to the client that it is time to start recording
        /// </summary>
        public event RecordingStartedEvent RecordingStarted;

        /// <summary>
        /// Client event used to communicate to the client that it is time to stop recording
        /// </summary>
        public event RecordingStoppedEvent RecordingStopped;

        /// <summary>
        /// Client event used to communicate to the client that it is time to create a screenshot
        /// </summary>
        public event ScreenshotRequestedEvent ScreenshotRequested;

        /// <summary>
        /// Client event used to notify the hook to exit
        /// </summary>
        public event DisconnectedEvent Disconnected;

        /// <summary>
        /// Client event used to display in-game text
        /// </summary>
        public event DisplayTextEvent DisplayText;

        #endregion

        #region Lifetime Services

        public override object InitializeLifetimeService()
        {
            //Returning null holds the object alive
            //until it is explicitly destroyed
            return null;
        }

        #endregion

        public void RecordingStartedProxyHandler(CaptureConfig config) => RecordingStarted?.Invoke(config);

        public void RecordingStoppedProxyHandler() => RecordingStopped?.Invoke();

        public void DisconnectedProxyHandler() => Disconnected?.Invoke();

        public void ScreenshotRequestedProxyHandler(ScreenshotRequest request) => ScreenshotRequested?.Invoke(request);

        public void DisplayTextProxyHandler(DisplayTextEventArgs args) => DisplayText?.Invoke(args);
    }
}