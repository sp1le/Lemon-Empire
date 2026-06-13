using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class WarehouseUpgradeUnlocker : MonoBehaviour
    {
        [Header("Blocker Objects to Deactivate on Level")]
        [SerializeField] private GameObject blocker1; // Wall at X = 0 (deactivated at lvl >= 1)
        [SerializeField] private GameObject blocker2; // Wall at X = -3 (deactivated at lvl >= 2)
        [SerializeField] private GameObject blocker3; // Walls at X = -6 and X = -9 (deactivated at lvl >= 3)

        private int lastCheckedLevel = -1;

        private void Start()
        {
            UpdateState(true);
        }

        private void Update()
        {
            if (UpgradeManager.WarehouseUpgradeLevel != lastCheckedLevel)
            {
                UpdateState(false);
            }
        }

        private void UpdateState(bool force)
        {
            int lvl = UpgradeManager.WarehouseUpgradeLevel;
            if (lvl == lastCheckedLevel && !force) return;

            lastCheckedLevel = lvl;

            if (blocker1 != null) blocker1.SetActive(lvl < 1);
            if (blocker2 != null) blocker2.SetActive(lvl < 2);
            if (blocker3 != null) blocker3.SetActive(lvl < 3);

            Debug.Log($"[WarehouseUpgradeUnlocker] Updated blockers. Current upgrade level: {lvl}");
        }
    }
}
