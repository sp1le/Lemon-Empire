using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class CoolerStand : DisplayStand
    {
        [Header("Cooler Settings")]
        [SerializeField] private float targetTemp = 5f;
        [SerializeField] private float coolRate = 2f;
        [SerializeField] private int weeklyElectricityFee = 50;

        protected override void Start()
        {
            base.Start();
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnShiftEnded += HandleShiftEnded;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnShiftEnded -= HandleShiftEnded;
            }
        }

        private void Update()
        {
            // Loop through all shelfItems. If an item's temperature is above targetTemp, reduce it towards targetTemp
            for (int i = shelfItems.Count - 1; i >= 0; i--)
            {
                var item = shelfItems[i];
                if (item != null)
                {
                    if (item.Temperature > targetTemp)
                    {
                        item.Temperature = Mathf.Max(targetTemp, item.Temperature - Time.deltaTime * coolRate);
                    }
                }
            }
        }

        private void HandleShiftEnded()
        {
            // TimeManager.Instance.CurrentDay is already incremented when OnShiftEnded fires.
            // Day starts at 1, so when the 7th shift ends, day goes from 7 to 8.
            // 8 - 1 = 7, so (CurrentDay - 1) % 7 == 0.
            if (TimeManager.Instance != null && (TimeManager.Instance.CurrentDay - 1) % 7 == 0)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.TrySpend(weeklyElectricityFee);
                    Debug.Log($"CoolerStand: Deducted weekly electricity fee of ${weeklyElectricityFee}");
                }
            }
        }
    }
}
