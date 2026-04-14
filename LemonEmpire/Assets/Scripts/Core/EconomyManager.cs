using UnityEngine;

namespace LemonEmpire.Core
{

    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Starting Balance")]
        [SerializeField] private int startingMoney = 500;

        private int _balance;

        public event System.Action<int> OnBalanceChanged;

        public int Balance => _balance;
        public int DailyRevenue { get; private set; }
        public int DailyExpenses { get; private set; }
        public int BottlesSold { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _balance = startingMoney;
        }

        private void Start()
        {
            OnBalanceChanged?.Invoke(_balance);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || _balance < amount) return false;

            _balance -= amount;
            DailyExpenses += amount;
            OnBalanceChanged?.Invoke(_balance);
            return true;
        }

        public void Earn(int amount)
        {
            if (amount <= 0) return;

            _balance += amount;
            DailyRevenue += amount;
            BottlesSold++;
            OnBalanceChanged?.Invoke(_balance);
        }

        public void ResetDailyStats()
        {
            DailyRevenue = 0;
            DailyExpenses = 0;
            BottlesSold = 0;
        }
    }
}
