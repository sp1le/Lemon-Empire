using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class LeftHallUnlocker : MonoBehaviour
    {
        [Header("Objects to Deactivate when Unlocked")]
        [SerializeField] private GameObject[] blockerObjects;

        [Header("Objects to Activate when Unlocked")]
        [SerializeField] private GameObject[] unlockedObjects;

        private bool wasUnlocked = false;

        private void Start()
        {
            UpdateState(true);
        }

        private void Update()
        {
            if (UpgradeManager.IsLeftHallUnlocked != wasUnlocked)
            {
                UpdateState(false);
            }
        }

        private void UpdateState(bool force)
        {
            bool isUnlocked = UpgradeManager.IsLeftHallUnlocked;
            if (isUnlocked == wasUnlocked && !force) return;

            wasUnlocked = isUnlocked;

            // Blockers are active when NOT unlocked
            if (blockerObjects != null)
            {
                foreach (var obj in blockerObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(!isUnlocked);
                    }
                }
            }

            // Unlocked objects are active when unlocked
            if (unlockedObjects != null)
            {
                foreach (var obj in unlockedObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(isUnlocked);
                    }
                }
            }
        }
    }
}
