using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class ShopUpgradeVisualsManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnLoad()
        {
            var go = new GameObject("ShopUpgradeVisualsManager");
            go.AddComponent<ShopUpgradeVisualsManager>();
            Debug.Log("[ShopUpgradeVisualsManager] Dynamically initialized in the scene.");
        }

        private GameObject _jukebox;
        private GameObject _vending;
        private GameObject _trashBin;

        private void Start()
        {
            // Find active or inactive scene objects at startup before they are hidden
            var jb = FindFirstObjectByType<Jukebox>(FindObjectsInactive.Include);
            if (jb != null) _jukebox = jb.gameObject;

            var vm = FindFirstObjectByType<VendingMachine>(FindObjectsInactive.Include);
            if (vm != null) _vending = vm.gameObject;

            var tb = FindFirstObjectByType<TrashBin>(FindObjectsInactive.Include);
            if (tb != null) _trashBin = tb.gameObject;

            UpdateVisuals();
        }

        private void Update()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_jukebox != null)
            {
                bool shouldBeActive = UpgradeManager.HasJukebox;
                if (_jukebox.activeSelf != shouldBeActive)
                {
                    _jukebox.SetActive(shouldBeActive);
                    Debug.Log($"[ShopUpgradeVisualsManager] Jukebox active state set to {shouldBeActive}");
                }
            }

            if (_vending != null)
            {
                bool shouldBeActive = UpgradeManager.HasSnackVending;
                if (_vending.activeSelf != shouldBeActive)
                {
                    _vending.SetActive(shouldBeActive);
                    Debug.Log($"[ShopUpgradeVisualsManager] VendingMachine active state set to {shouldBeActive}");
                }
            }

            if (_trashBin != null)
            {
                bool shouldBeActive = UpgradeManager.HasTrashBin;
                if (_trashBin.activeSelf != shouldBeActive)
                {
                    _trashBin.SetActive(shouldBeActive);
                    Debug.Log($"[ShopUpgradeVisualsManager] TrashBin active state set to {shouldBeActive}");
                }
            }
        }
    }
}
