using System;
using UnityEngine;

namespace LemonEmpire.Core
{
    public class PlayerVitals : MonoBehaviour
    {
        public static PlayerVitals Instance { get; private set; }

        [Header("Vitals (GDD)")]
        [Range(0f, 100f)]
        [SerializeField] private float satiety = 100f;
        [Range(0f, 100f)]
        [SerializeField] private float morale = 100f;
        [Range(-30f, 50f)]
        [SerializeField] private float lookScore = 0f;

        [Header("Decay Settings")]
        [Tooltip("Satiety lost per real second. Day=20min → 100/1200=0.083")]
        [SerializeField] private float satietyDecayPerSec = 0.083f;
        [Tooltip("Morale passive decay per real second (very slow)")]
        [SerializeField] private float moraleDecayPerSec = 0.01f;

        [Header("Thresholds")]
        [SerializeField] private float lowSatietyThreshold = 30f;
        [SerializeField] private float lowMoraleThreshold = 20f;
        [SerializeField] private float speedPenalty = 0.35f;

        public event Action OnVitalsChanged;

        public float Satiety
        {
            get => satiety;
            set
            {
                satiety = Mathf.Clamp(value, 0f, 100f);
                OnVitalsChanged?.Invoke();
            }
        }

        public float Morale
        {
            get => morale;
            set
            {
                morale = Mathf.Clamp(value, 0f, 100f);
                OnVitalsChanged?.Invoke();
            }
        }

        public float LookScore
        {
            get => lookScore;
            set
            {
                lookScore = Mathf.Clamp(value, -30f, 50f);
                OnVitalsChanged?.Invoke();
            }
        }

        /// <summary>True when satiety is critically low.</summary>
        public bool IsHungry => satiety < lowSatietyThreshold;

        /// <summary>True when morale is critically low — unlocks desperate dialogue options.</summary>
        public bool IsDesperate => morale < lowMoraleThreshold;

        /// <summary>Speed multiplier based on satiety. 1.0 = normal, lower when hungry.</summary>
        public float SpeedMultiplier => IsHungry
            ? 1f - speedPenalty * (1f - satiety / lowSatietyThreshold)
            : 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            satiety = Mathf.Max(0f, satiety - satietyDecayPerSec * dt);
            morale = Mathf.Max(0f, morale - moraleDecayPerSec * dt);

            // Invoke event every frame to keep HUD smooth
            OnVitalsChanged?.Invoke();
        }

        public float CalculateReaction(float quality)
        {
            return (quality * 0.5f) + (lookScore * 0.3f) + UnityEngine.Random.Range(-20f, 20f);
        }

        /// <summary>Called on failed deals to penalize morale.</summary>
        public void OnDealFailed()
        {
            Morale -= 10f;
        }

        /// <summary>Called on successful deals to slightly boost morale.</summary>
        public void OnDealSuccess()
        {
            Morale += 3f;
        }
    }
}
