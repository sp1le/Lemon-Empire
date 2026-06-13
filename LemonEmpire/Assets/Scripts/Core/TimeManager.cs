using System.Collections;
using UnityEngine;
using LemonEmpire.UI;
using LemonEmpire.Trading;

namespace LemonEmpire.Core
{

    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Time Settings")]
        [SerializeField] private float startHour = 8f;
        [SerializeField] private float endHour = 22f;
        [Tooltip("How many real minutes a full 24h day takes")]
        [SerializeField] private float realMinutesPerDay = 20f;

        [Header("Environment")]
        [SerializeField] private Light sun;

        public int CurrentDay { get; private set; } = 1;
        public float CurrentTimeOfDay { get; private set; }
        public bool IsShiftActive { get; private set; }

        public event System.Action OnShiftStarted;
        public event System.Action OnShiftEnded;

        private float _timeMultiplier;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _timeMultiplier = 24f / (realMinutesPerDay * 60f);
            Application.targetFrameRate = 180;
        }

        private void Start()
        {

            StartShift();
        }

        private void Update()
        {
            if (!IsShiftActive) return;

            CurrentTimeOfDay += Time.deltaTime * _timeMultiplier;

            UpdateSun();

            if (CurrentTimeOfDay >= endHour)
            {
                EndShift();
            }
        }

        private void UpdateSun()
        {
            if (sun == null) return;

            float sunAngle = (CurrentTimeOfDay / 24f) * 360f - 90f;
            sun.transform.rotation = Quaternion.Euler(sunAngle, 45f, 0f);
        }

        public void StartShift()
        {
            if (IsShiftActive) return;

            GameEventManager.RandomizeTrend();

            EconomyManager.Instance?.ResetDailyStats();

            CurrentTimeOfDay = startHour;
            IsShiftActive = true;

            UpdateSun();
            OnShiftStarted?.Invoke();
        }

        public void EndShift()
        {
            if (!IsShiftActive) return;

            IsShiftActive = false;

            StartCoroutine(WaitAndEndShiftRoutine());
        }

        private IEnumerator WaitAndEndShiftRoutine()
        {

            yield return new WaitUntil(() => FindObjectsByType<NPCBuyer>(FindObjectsSortMode.None).Length == 0);

            CurrentDay++;

            OnShiftEnded?.Invoke();

            var hud = FindFirstObjectByType<GameHUD>();
            if (hud != null)
            {
                hud.ShowDaySummary(this, EconomyManager.Instance);
            }
        }
    }
}
