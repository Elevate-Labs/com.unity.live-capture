#if URP_10_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.LiveCapture.Rendering
{
    internal interface IRenderPipelineBridge
    {
#if URP_10_2_OR_NEWER
        /// <summary>
        /// Returns a reference to a render feature and adds this reference it if not already present.
        /// </summary>
        T RequestRenderFeature<T>() where T : ScriptableRendererFeature;

        /// <summary>
        /// Indicates whether or not a render feature is currently active on the renderer.
        /// </summary>
        /// <param name="result">The render feature, if active.</param>
        bool HasRenderFeature<T>(out T result) where T : ScriptableRendererFeature;
#endif
    }
}
