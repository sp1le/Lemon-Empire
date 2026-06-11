using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LemonEmpire.Core;
using LemonEmpire.Production;

namespace LemonEmpire.Player
{
    public class PlayerStatusEffects : MonoBehaviour
    {
        public static PlayerStatusEffects Instance { get; private set; }

        [Header("State Tracking")]
        [SerializeField] private float _sugarRushTimer = 0f;
        [SerializeField] private float _coffeeCollapseTimer = 0f;
        [SerializeField] private int _failedNegotiationsCount = 0;
        [SerializeField] private int _sweetSodasConsumedCount = 0;
        [SerializeField] private int _carbonatedSodasConsumedCount = 0;
        [SerializeField] private int _alcoholicSodasConsumedCount = 0;
        [SerializeField] private float _carbonationTimer = 0f;
        [SerializeField] private float _alcoholTimer = 0f;
        private float _nextHiccupTime = 0f;

        [Header("FOV Warp Settings")]
        private float _baseFov = 60f;
        private bool _baseFovCached = false;

        private bool _isSleeping = false;
        private float _nextSleepTriggerTime = 0f;

        // Public properties to expose states
        public bool IsSugarRushActive => _sugarRushTimer > 0f;
        public bool IsCoffeeCollapseActive => _coffeeCollapseTimer >= 180f;
        public bool IsLemonParanoiaActive => _failedNegotiationsCount >= 3;
        public bool IsFinancialDepressionActive => EconomyManager.Instance != null && EconomyManager.Instance.Balance < 20;
        public bool IsSanitationPanicActive => ShopCleanlinessManager.Instance != null && ShopCleanlinessManager.Instance.Cleanliness < 20f;
        public bool IsSleeping => _isSleeping;
        public bool IsCarbonationOverloadActive => _carbonationTimer > 0f;
        public bool IsAlcoholIntoxicationActive => _alcoholTimer > 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Cached base FOV will be set dynamically in UpdateCameraFov() when main camera is initialized
        }

        private void Update()
        {
            // Update timers
            if (_sugarRushTimer > 0f)
            {
                _sugarRushTimer -= Time.deltaTime;
            }

            // Carbonation & Alcohol decay
            if (_carbonationTimer > 0f)
            {
                _carbonationTimer -= Time.deltaTime;
                if (Time.time >= _nextHiccupTime)
                {
                    TriggerHiccup();
                }
            }
            if (_alcoholTimer > 0f)
            {
                _alcoholTimer -= Time.deltaTime;
            }

            // Coffee Collapse tracking
            var vitals = PlayerVitals.Instance;
            if (vitals != null && vitals.Satiety <= 0.001f)
            {
                _coffeeCollapseTimer += Time.deltaTime;

                if (IsCoffeeCollapseActive && !_isSleeping && Time.time >= _nextSleepTriggerTime)
                {
                    StartCoroutine(TriggerSleepCoroutine());
                }
            }
            else
            {
                _coffeeCollapseTimer = 0f;
                if (_isSleeping)
                {
                    StopAllCoroutines();
                    _isSleeping = false;
                    var controller = FindFirstObjectByType<PlayerController>();
                    if (controller != null)
                    {
                        controller.MovementLocked = false;
                    }
                }
            }

            UpdateCameraFov();
        }

        private IEnumerator TriggerSleepCoroutine()
        {
            _isSleeping = true;
            Debug.Log("[PlayerStatusEffects] Coffee Collapse triggered a 3-second sleep!");

            var controller = FindFirstObjectByType<PlayerController>();
            if (controller != null)
            {
                controller.MovementLocked = true;
            }

            yield return new WaitForSeconds(3f);

            if (controller != null)
            {
                controller.MovementLocked = false;
            }

            _isSleeping = false;
            _nextSleepTriggerTime = Time.time + Random.Range(20f, 45f);
        }

        public void TriggerSugarRush(float duration = 30f)
        {
            _sugarRushTimer = Mathf.Max(_sugarRushTimer, duration);
            Debug.Log($"[PlayerStatusEffects] Sugar Rush triggered for {duration} seconds!");
        }

        public void TriggerCarbonationOverload(float duration = 30f)
        {
            _carbonationTimer = Mathf.Max(_carbonationTimer, duration);
            _nextHiccupTime = Time.time + Random.Range(2f, 5f);
            Debug.Log($"[PlayerStatusEffects] Carbonation Overload triggered for {duration} seconds!");
        }

        public void TriggerAlcoholIntoxication(float duration = 40f)
        {
            _alcoholTimer = Mathf.Max(_alcoholTimer, duration);
            Debug.Log($"[PlayerStatusEffects] Alcohol Intoxication triggered for {duration} seconds!");
        }

        private void TriggerHiccup()
        {
            _nextHiccupTime = Time.time + Random.Range(6f, 12f);
            Debug.Log("[PlayerStatusEffects] *Ик!* Слишком много газов!");
            var fpsCamera = FindFirstObjectByType<ThirdPersonCamera>();
            if (fpsCamera != null)
            {
                fpsCamera.TriggerHiccupKick();
            }
        }

        public void ConsumeSweetSoda()
        {
            _sweetSodasConsumedCount++;
            Debug.Log($"[PlayerStatusEffects] Sweet soda consumed! Count: {_sweetSodasConsumedCount}/3");
            if (_sweetSodasConsumedCount >= 3)
            {
                TriggerSugarRush(30f);
                _sweetSodasConsumedCount = 0; // reset
            }
        }

        public void ConsumeCarbonatedSoda()
        {
            _carbonatedSodasConsumedCount++;
            Debug.Log($"[PlayerStatusEffects] Carbonated soda consumed! Count: {_carbonatedSodasConsumedCount}/3");
            if (_carbonatedSodasConsumedCount >= 3)
            {
                TriggerCarbonationOverload(30f);
                _carbonatedSodasConsumedCount = 0; // reset
            }
        }

        public void ConsumeAlcoholicSoda()
        {
            _alcoholicSodasConsumedCount++;
            Debug.Log($"[PlayerStatusEffects] Alcoholic soda consumed! Count: {_alcoholicSodasConsumedCount}/3");
            if (_alcoholicSodasConsumedCount >= 3)
            {
                TriggerAlcoholIntoxication(40f);
                _alcoholicSodasConsumedCount = 0; // reset
            }
        }

        public void NotifyDealFailed()
        {
            _failedNegotiationsCount++;
            Debug.Log($"[PlayerStatusEffects] Deal failed. Failed count: {_failedNegotiationsCount}");
        }

        public void NotifyDealSuccess()
        {
            // Reset paranoia on success, or keep it cumulative? Let's just track it but keep it cumulative as requested.
            Debug.Log("[PlayerStatusEffects] Deal succeeded!");
        }

        public void ResetParanoia()
        {
            _failedNegotiationsCount = 0;
        }

        public float GetSpeedMultiplier()
        {
            float mult = 1f;

            if (IsSugarRushActive)
            {
                mult *= 2.0f;
            }

            if (IsSanitationPanicActive)
            {
                var playerCarry = FindFirstObjectByType<PlayerCarry>();
                if (playerCarry != null && playerCarry.IsCarrying && playerCarry.CarriedItem != null && playerCarry.CarriedItem.GetComponent<BroomTool>() != null)
                {
                    mult *= 3.0f;
                }
            }

            return mult;
        }

        private void UpdateCameraFov()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (!_baseFovCached)
            {
                _baseFov = cam.fieldOfView;
                _baseFovCached = true;
            }

            float targetFov = IsSugarRushActive ? _baseFov * 1.50f : _baseFov;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * 6f);
        }
    }
}
