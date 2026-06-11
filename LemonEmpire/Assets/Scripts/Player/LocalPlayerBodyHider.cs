using UnityEngine;

namespace LemonEmpire.Player
{
    /// <summary>
    /// Скрывает части тела для локального игрока (first-person view)
    /// Оставляет видимыми для других игроков в мультиплеере
    /// </summary>
    public class LocalPlayerBodyHider : MonoBehaviour
    {
        [Header("Body Parts to Hide")]
        [Tooltip("Имена мешей тела которые нужно скрыть для локального игрока")]
        [SerializeField] private string[] bodyMeshNames = new string[]
        {
            "Body",
            "Torso",
            "Chest",
            "Head"  // Голову тоже можно скрыть если мешает
        };

        [Header("Shadow Settings")]
        [Tooltip("Режим теней для скрытых частей тела")]
        [SerializeField] private UnityEngine.Rendering.ShadowCastingMode hiddenBodyShadowMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        [Header("Settings")]
        [SerializeField] private bool isLocalPlayer = true;
        [SerializeField] private bool hideOnStart = true;

        private SkinnedMeshRenderer[] _allMeshRenderers;
        private MeshRenderer[] _staticMeshRenderers;

        private void Start()
        {
            if (hideOnStart && isLocalPlayer)
            {
                HideBodyParts();
            }
        }

        /// <summary>
        /// Скрывает указанные части тела
        /// </summary>
        public void HideBodyParts()
        {
            // Получаем все SkinnedMeshRenderer (для анимированных моделей)
            _allMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var renderer in _allMeshRenderers)
            {
                if (ShouldHideMesh(renderer.name))
                {
                    // Вместо полного отключения, оставляем только тени
                    renderer.shadowCastingMode = hiddenBodyShadowMode;
                    Debug.Log($"[LocalPlayerBodyHider] Скрыт меш (тени остались): {renderer.name}");
                }
            }

            // Получаем все MeshRenderer (для статичных мешей)
            _staticMeshRenderers = GetComponentsInChildren<MeshRenderer>(true);

            foreach (var renderer in _staticMeshRenderers)
            {
                if (ShouldHideMesh(renderer.name))
                {
                    renderer.shadowCastingMode = hiddenBodyShadowMode;
                    Debug.Log($"[LocalPlayerBodyHider] Скрыт статичный меш (тени остались): {renderer.name}");
                }
            }
        }

        /// <summary>
        /// Показывает все части тела (для других игроков)
        /// </summary>
        public void ShowBodyParts()
        {
            if (_allMeshRenderers != null)
            {
                foreach (var renderer in _allMeshRenderers)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }

            if (_staticMeshRenderers != null)
            {
                foreach (var renderer in _staticMeshRenderers)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }

            Debug.Log("[LocalPlayerBodyHider] Все части тела показаны");
        }

        /// <summary>
        /// Скрывает конкретный меш по имени
        /// </summary>
        public void HideSpecificMesh(string meshName)
        {
            var allRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in allRenderers)
            {
                if (renderer.name.Equals(meshName, System.StringComparison.OrdinalIgnoreCase))
                {
                    renderer.shadowCastingMode = hiddenBodyShadowMode;
                    Debug.Log($"[LocalPlayerBodyHider] Скрыт меш (тени остались): {meshName}");
                    return;
                }
            }

            Debug.LogWarning($"[LocalPlayerBodyHider] Меш '{meshName}' не найден!");
        }

        /// <summary>
        /// Показывает конкретный меш по имени
        /// </summary>
        public void ShowSpecificMesh(string meshName)
        {
            var allRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in allRenderers)
            {
                if (renderer.name.Equals(meshName, System.StringComparison.OrdinalIgnoreCase))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    Debug.Log($"[LocalPlayerBodyHider] Показан меш: {meshName}");
                    return;
                }
            }

            Debug.LogWarning($"[LocalPlayerBodyHider] Меш '{meshName}' не найден!");
        }

        /// <summary>
        /// Устанавливает является ли этот игрок локальным
        /// </summary>
        public void SetLocalPlayer(bool isLocal)
        {
            isLocalPlayer = isLocal;

            if (isLocal)
                HideBodyParts();
            else
                ShowBodyParts();
        }

        /// <summary>
        /// Проверяет нужно ли скрывать меш по имени
        /// </summary>
        private bool ShouldHideMesh(string meshName)
        {
            return true; // Скрываем абсолютно всё тело (включая руки) локального игрока
        }

        /// <summary>
        /// Выводит список всех мешей в модели (для отладки)
        /// </summary>
        [ContextMenu("Показать все меши в модели")]
        public void DebugListAllMeshes()
        {
            Debug.Log("=== СПИСОК ВСЕХ МЕШЕЙ В МОДЕЛИ ===");

            var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Debug.Log($"\nSkinnedMeshRenderer ({skinnedRenderers.Length}):");
            foreach (var renderer in skinnedRenderers)
            {
                Debug.Log($"  - {renderer.name} (enabled: {renderer.enabled})");
            }

            var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            Debug.Log($"\nMeshRenderer ({meshRenderers.Length}):");
            foreach (var renderer in meshRenderers)
            {
                Debug.Log($"  - {renderer.name} (enabled: {renderer.enabled})");
            }

            Debug.Log("\n=== КОНЕЦ СПИСКА ===");
        }

        /// <summary>
        /// Добавляет имя меша в список скрываемых
        /// </summary>
        public void AddBodyPartToHide(string meshName)
        {
            System.Array.Resize(ref bodyMeshNames, bodyMeshNames.Length + 1);
            bodyMeshNames[bodyMeshNames.Length - 1] = meshName;

            if (isLocalPlayer)
            {
                HideSpecificMesh(meshName);
            }
        }
    }
}
