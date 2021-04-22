using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The struct used to transport the state of a video stream over the network.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VideoStreamState : IEquatable<VideoStreamState>
    {
        /// <summary>
        /// Is the video streaming server active.
        /// </summary>
        public bool isRunning;

        /// <summary>
        /// The port the video streaming server is listening on.
        /// </summary>
        public int port;

        /// <inheritdoc/>
        public bool Equals(VideoStreamState other)
        {
            return isRunning == other.isRunning && port == other.port;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current VideoStreamState.
        /// </summary>
        /// <param name="obj">The object to compare with the current VideoStreamState.</param>
        /// <returns>
        /// true if the specified object is equal to the current VideoStreamState; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is VideoStreamState other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for the VideoStreamState.
        /// </summary>
        /// <returns>
        /// The hash value generated for this VideoStreamState.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = isRunning.GetHashCode();
                hashCode = (hashCode * 397) ^ port.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Returns a string that represents the current VideoStreamState.
        /// </summary>
        /// <returns>
        /// A string that represents the current VideoStreamState.
        /// </returns>
        public override string ToString()
        {
            return $"(IsRunning {isRunning}, Port {port})";
        }

        /// <summary>
        /// Determines whether the two specified VideoStreamState are equal.
        /// </summary>
        /// <param name="a">The first VideoStreamState.</param>
        /// <param name="b">The second VideoStreamState.</param>
        /// <returns>
        /// true if the specified VideoStreamState are equal; otherwise, false.
        /// </returns>
        public static bool operator==(VideoStreamState a, VideoStreamState b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether the two specified VideoStreamState are different.
        /// </summary>
        /// <param name="a">The first VideoStreamState.</param>
        /// <param name="b">The second VideoStreamState.</param>
        /// <returns>
        /// true if the specified VideoStreamState are different; otherwise, false.
        /// </returns>
        public static bool operator!=(VideoStreamState a, VideoStreamState b)
        {
            return !(a == b);
        }
    }
}