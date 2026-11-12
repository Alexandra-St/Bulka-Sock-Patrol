using UnityEngine;

namespace BulkoGame.UI
{
    /// <summary>Keeps the full Level_01 width and world-space HUD visible across landscape aspect ratios.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class AspectFitCamera : MonoBehaviour
    {
        [SerializeField] private float baseOrthographicSize = 9f;
        [SerializeField] private float requiredWorldWidth = 31f;

        private Camera targetCamera;
        private float lastAspect;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            ApplyFit();
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(targetCamera.aspect, lastAspect)) ApplyFit();
        }

        private void ApplyFit()
        {
            lastAspect = targetCamera.aspect;
            float sizeNeededForWidth = requiredWorldWidth / (2f * Mathf.Max(.1f, lastAspect));
            targetCamera.orthographicSize = Mathf.Max(baseOrthographicSize, sizeNeededForWidth);
        }
    }
}
