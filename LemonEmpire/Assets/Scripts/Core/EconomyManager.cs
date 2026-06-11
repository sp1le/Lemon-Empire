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
    }
}
