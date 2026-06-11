using UnityEngine;

namespace LemonEmpire.Player
{
    /// <summary>
    /// Настраивает камеру для first-person view
    /// Исправляет clipping рук и настраивает FOV
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FirstPersonCameraSetup : MonoBehaviour
    {
        [Header("Clipping Settings")]
        [Tooltip("Near clipping plane - меньше значение = руки ближе к камере не обрезаются")]
        [SerializeField] private float nearClipPlane = 0.01f; // Очень близко!

        [Tooltip("Far clipping plane")]
        [SerializeField] private float farClipPlane = 1000f;

        [Header("FOV Settings")]
        [Tooltip("Field of View для first-person")]
        [SerializeField] private float fieldOfView = 75f;

        [Header("Arm Layer (опционально)")]
        [Tooltip("Если руки на отдельном слое, можно настроить отдельную камеру")]
        [SerializeField] private bool useArmLayer = false;
        [SerializeField] private LayerMask armLayerMask;
        [SerializeField] private Camera armCamera;

        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = GetComponent<Camera>();
            SetupCamera();

            if (useArmLayer && armCamera == null)
            {
                CreateArmCamera();
            }
        }

        private void SetupCamera()
        {
            // Устанавливаем near clip очень близко чтобы руки не обрезались
            _mainCamera.nearClipPlane = nearClipPlane;
            _mainCamera.farClipPlane = farClipPlane;
            _mainCamera.fieldOfView = fieldOfView;

            Debug.Log($"[FirstPersonCamera] Near clip: {nearClipPlane}, FOV: {fieldOfView}");
        }

        /// <summary>
        /// Создаёт отдельную камеру для рук (продвинутый метод)
        /// Руки рендерятся поверх всего с отдельным near clip
        /// </summary>
        private void CreateArmCamera()
        {
            GameObject armCamObj = new GameObject("ArmCamera");
            armCamObj.transform.SetParent(transform);
            armCamObj.transform.localPosition = Vector3.zero;
            armCamObj.transform.localRotation = Quaternion.identity;

            armCamera = armCamObj.AddComponent<Camera>();
            armCamera.clearFlags = CameraClearFlags.Depth; // Рендерим поверх основной камеры
            armCamera.cullingMask = armLayerMask;
            armCamera.depth = _mainCamera.depth + 1; // Рендерим после основной камеры
            armCamera.nearClipPlane = 0.01f; // Очень близко для рук
            armCamera.farClipPlane = 2f; // Только руки (близко)
            armCamera.fieldOfView = fieldOfView;

            // Исключаем arm layer из основной камеры
            _mainCamera.cullingMask &= ~armLayerMask;

            Debug.Log("[FirstPersonCamera] Создана отдельная камера для рук");
        }

        /// <summary>
        /// Изменить near clip plane в runtime
        /// </summary>
        public void SetNearClipPlane(float value)
        {
            nearClipPlane = Mathf.Max(0.01f, value);
            if (_mainCamera != null)
                _mainCamera.nearClipPlane = nearClipPlane;
        }

        /// <summary>
        /// Изменить FOV в runtime
        /// </summary>
        public void SetFieldOfView(float fov)
        {
            fieldOfView = Mathf.Clamp(fov, 30f, 120f);
            if (_mainCamera != null)
                _mainCamera.fieldOfView = fieldOfView;
            if (armCamera != null)
                armCamera.fieldOfView = fieldOfView;
        }

        private void OnValidate()
        {
            // Применяем изменения в Editor
            if (_mainCamera == null)
                _mainCamera = GetComponent<Camera>();

            if (_mainCamera != null)
            {
                _mainCamera.nearClipPlane = nearClipPlane;
                _mainCamera.farClipPlane = farClipPlane;
                _mainCamera.fieldOfView = fieldOfView;
            }
        }
    }
}
