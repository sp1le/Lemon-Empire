using UnityEngine;

namespace LemonEmpire.Player
{
    /// <summary>
    /// Создаёт отдельную камеру для рук чтобы они никогда не обрезались
    /// Руки рендерятся на отдельном слое поверх основной сцены
    /// </summary>
    public class ArmLayerCameraSetup : MonoBehaviour
    {
        [Header("Arm Layer")]
        [Tooltip("Слой для рук (создайте новый слой 'PlayerArms')")]
        [SerializeField] private string armLayerName = "PlayerArms";

        [Header("Camera Settings")]
        [Tooltip("FOV для камеры рук (может отличаться от основной)")]
        [SerializeField] private float armCameraFOV = 60f;

        [Tooltip("Near clip для камеры рук (очень близко)")]
        [SerializeField] private float armCameraNearClip = 0.01f;

        [Tooltip("Far clip для камеры рук (только руки)")]
        [SerializeField] private float armCameraFarClip = 2f;

        [Header("Auto Setup")]
        [Tooltip("Автоматически найти и настроить меши рук")]
        [SerializeField] private bool autoSetupArms = true;

        [Tooltip("Имена мешей которые считаются руками")]
        [SerializeField] private string[] armMeshNames = new string[] { "Arms", "Arm", "Hand", "Hands" };

        private Camera _mainCamera;
        private Camera _armCamera;
        private int _armLayer;

        private void Start()
        {
            // Bypassed: we hide the entire character body instead of using extra cameras/layers!
            return;
        }

        private void SetupArmLayer()
        {
            // Получаем или создаём слой для рук
            _armLayer = LayerMask.NameToLayer(armLayerName);

            if (_armLayer == -1)
            {
                Debug.LogError($"[ArmLayerCamera] Слой '{armLayerName}' не найден! Создайте его в Project Settings → Tags and Layers");
                return;
            }

            // Создаём камеру для рук
            CreateArmCamera();

            // Автоматически настраиваем меши рук
            if (autoSetupArms)
            {
                SetupArmMeshes();
            }

            Debug.Log($"[ArmLayerCamera] Настроена отдельная камера для рук на слое '{armLayerName}'");
        }

        private void CreateArmCamera()
        {
            // Создаём объект камеры
            GameObject armCamObj = new GameObject("ArmCamera");
            armCamObj.transform.SetParent(transform);
            armCamObj.transform.localPosition = Vector3.zero;
            armCamObj.transform.localRotation = Quaternion.identity;

            // Настраиваем камеру
            _armCamera = armCamObj.AddComponent<Camera>();

            // Рендерим только слой рук
            _armCamera.cullingMask = 1 << _armLayer;

            // Рендерим поверх основной камеры
            _armCamera.clearFlags = CameraClearFlags.Depth;
            _armCamera.depth = _mainCamera.depth + 1;

            // Настройки clipping
            _armCamera.nearClipPlane = armCameraNearClip;
            _armCamera.farClipPlane = armCameraFarClip;
            _armCamera.fieldOfView = armCameraFOV;

            // Исключаем слой рук из основной камеры
            _mainCamera.cullingMask &= ~(1 << _armLayer);

            Debug.Log($"[ArmLayerCamera] Создана камера для рук: depth={_armCamera.depth}, FOV={armCameraFOV}");
        }

        private void SetupArmMeshes()
        {
            // Ищем игрока в родителях
            Transform playerRoot = transform.parent;
            if (playerRoot == null)
            {
                Debug.LogWarning("[ArmLayerCamera] Не найден корневой объект Player");
                return;
            }

            // Получаем все рендереры
            var allRenderers = playerRoot.GetComponentsInChildren<Renderer>(true);
            int count = 0;

            foreach (var renderer in allRenderers)
            {
                if (IsArmMesh(renderer.name))
                {
                    // Переносим на слой рук
                    renderer.gameObject.layer = _armLayer;
                    count++;
                    Debug.Log($"[ArmLayerCamera] Меш '{renderer.name}' перенесён на слой '{armLayerName}'");
                }
            }

            if (count == 0)
            {
                Debug.LogWarning($"[ArmLayerCamera] Не найдено мешей рук. Проверьте имена: {string.Join(", ", armMeshNames)}");
            }
            else
            {
                Debug.Log($"[ArmLayerCamera] Настроено {count} мешей рук");
            }
        }

        private bool IsArmMesh(string meshName)
        {
            foreach (string armName in armMeshNames)
            {
                if (meshName.IndexOf(armName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Вручную добавить объект на слой рук
        /// </summary>
        public void AddToArmLayer(GameObject obj)
        {
            if (_armLayer == -1)
            {
                Debug.LogError("[ArmLayerCamera] Слой рук не настроен!");
                return;
            }

            obj.layer = _armLayer;
            Debug.Log($"[ArmLayerCamera] Объект '{obj.name}' добавлен на слой рук");
        }

        /// <summary>
        /// Изменить FOV камеры рук
        /// </summary>
        public void SetArmCameraFOV(float fov)
        {
            armCameraFOV = fov;
            if (_armCamera != null)
                _armCamera.fieldOfView = fov;
        }

        /// <summary>
        /// Синхронизировать FOV с основной камерой
        /// </summary>
        public void SyncFOVWithMainCamera()
        {
            if (_mainCamera != null && _armCamera != null)
            {
                _armCamera.fieldOfView = _mainCamera.fieldOfView;
            }
        }

        private void LateUpdate()
        {
            // Синхронизируем FOV если нужно
            if (_armCamera != null && _mainCamera != null)
            {
                // Можно добавить опцию для автосинхронизации
                // _armCamera.fieldOfView = _mainCamera.fieldOfView;
            }
        }

        private void OnDestroy()
        {
            // Очищаем камеру рук
            if (_armCamera != null)
            {
                Destroy(_armCamera.gameObject);
            }
        }

        /// <summary>
        /// Показать информацию о настройке в Console
        /// </summary>
        [ContextMenu("Показать информацию о слоях")]
        public void DebugLayerInfo()
        {
            Debug.Log("=== ARM LAYER CAMERA INFO ===");
            Debug.Log($"Arm Layer: {armLayerName} (index: {_armLayer})");
            Debug.Log($"Main Camera Culling Mask: {_mainCamera.cullingMask}");

            if (_armCamera != null)
            {
                Debug.Log($"Arm Camera Culling Mask: {_armCamera.cullingMask}");
                Debug.Log($"Arm Camera FOV: {_armCamera.fieldOfView}");
                Debug.Log($"Arm Camera Near/Far: {_armCamera.nearClipPlane} / {_armCamera.farClipPlane}");
            }

            Debug.Log("=== END INFO ===");
        }
    }
}
