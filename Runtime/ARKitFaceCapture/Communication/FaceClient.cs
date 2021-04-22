using System;
using Unity.LiveCapture.CompanionApp;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;
using UnityEngine.Scripting;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// An interface used to communicate with the face capture companion app.
    /// </summary>
    public interface IFaceClient : ICompanionAppClient
    {
        /// <summary>
        /// An event invoked when a face pose sample is received.
        /// </summary>
        event Action<FaceSample> facePoseSampleReceived;
    }

    /// <summary>
    /// A class used to communicate with the face capture companion app.
    /// </summary>
    [Preserve]
    [Client(k_ClientType)]
    public class FaceClient : CompanionAppClient, IFaceClient
    {
        /// <summary>
        /// The type of client this device supports.
        /// </summary>
        const string k_ClientType = "ARKit Face Capture";

        /// <inheritdoc />
        public event Action<FaceSample> facePoseSampleReceived;

        /// <inheritdoc />
        public FaceClient(NetworkBase network, Remote remote, ClientInitialization data)
            : base(network, remote, data)
        {
            m_Protocol.Add(new BinaryReceiver<FaceSample>(FaceMessages.ToServer.k_FacePoseSample,
                ChannelType.UnreliableUnordered)).AddHandler((pose) =>
                {
                    facePoseSampleReceived?.Invoke(pose);
                });
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => k_ClientType;
    }
}
