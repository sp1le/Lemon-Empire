using UnityEngine;

namespace LemonEmpire.Core
{

    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Starting Balance")]
        [SerializeField] private float startingMoney = 500f;

        private float _balance;

        public event System.Action<float> OnBalanceChanged;

        public float Balance => _balance;
        public float DailyRevenue { get; private set; }
        public float DailyExpenses { get; private set; }
        public int BottlesSold { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _balance = Mathf.Max(startingMoney, 500f); // Enforce starting balance of at least $500 to prevent premature depression
        }

        private void Start()
        {
            OnBalanceChanged?.Invoke(_balance);
        }

        public bool TrySpend(float amount)
        {
            if (amount <= 0f || _balance < amount) return false;

            _balance -= amount;
            DailyExpenses += amount;
            OnBalanceChanged?.Invoke(_balance);
            return true;
        }

        public void ForceSpend(float amount)
        {
            if (amount <= 0f) return;
            _balance -= amount;
            DailyExpenses += amount;
            OnBalanceChanged?.Invoke(_balance);
        }

        public void Earn(float amount)
        {
            if (amount <= 0f) return;

            _balance += amount;
            DailyRevenue += amount;
            BottlesSold++;
            OnBalanceChanged?.Invoke(_balance);
        }

        public void ResetDailyStats()
        {
            DailyRevenue = 0f;
            DailyExpenses = 0f;
            BottlesSold = 0;
        }

        private bool _isGameOver = false;
        private float _nextGameOverCheckTime = 0f;

        private void Update()
        {
            CheckGameOverCondition();
        }

        private void CheckGameOverCondition()
        {
            if (_isGameOver) return;

            // Only check if shift is active to avoid triggering in the main menu or before start
            if (TimeManager.Instance == null || !TimeManager.Instance.IsShiftActive) return;

            if (Time.time >= _nextGameOverCheckTime)
            {
                _nextGameOverCheckTime = Time.time + 1f; // Check once per second

                if (_balance < 10f && GetTotalGoodsCount() < 1)
                {
                    TriggerGameOver();
                }
            }
        }

        private int GetTotalGoodsCount()
        {
            int count = 0;
            var items = FindObjectsByType<ItemBase>(FindObjectsSortMode.None);
            foreach (var item in items)
            {
                if (item != null && item.ItemType == ItemType.BottledLemonade)
                {
                    count += item.Amount;
                }
            }
            return count;
        }

        private void TriggerGameOver()
        {
            _isGameOver = true;
            Debug.Log("[EconomyManager] Bankruptcy triggered! 0 goods and balance < $10.");

            if (LemonEmpire.Player.PlayerController.Instance != null)
            {
                LemonEmpire.Player.PlayerController.Instance.MovementLocked = true;
            }

            var hud = FindFirstObjectByType<LemonEmpire.UI.GameHUD>();
            if (hud != null)
            {
                hud.ShowGameOverPanel();
            }
        }
    }
}
