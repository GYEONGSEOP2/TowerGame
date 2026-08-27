using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    /// <summary>Applies the gameplay frame-rate target used by mobile player builds.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class MobileRuntimeSettings : MonoBehaviour
    {
        [Min(30)] public int targetFrameRate = 120;

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_IOS
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            OnDemandRendering.renderFrameInterval = 1;
#endif
        }
    }
}
