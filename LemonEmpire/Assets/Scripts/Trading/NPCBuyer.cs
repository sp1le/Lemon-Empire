using UnityEngine;
using UnityEngine.AI;
using LemonEmpire.Core;
using LemonEmpire.UI;
using LemonEmpire.Network;
using System.Collections;
using System.Collections.Generic;
using LemonEmpire.Production;
using LemonEmpire.Player;

namespace LemonEmpire.Trading
{
    public enum NPCState
    {
        Walking,
        Evaluating,
        Buying,
        Leaving
    }

    public enum NPCArchetype
    {
        Kids,
        Athletes,
        Hipsters,
        PartyAnimals
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCBuyer : MonoBehaviour, IInteractable
    {
        public enum BarCustomerState
        {
            None,
            WalkingToQueue,
            WaitingInLine,
            WaitingForOrder,
            WaitingForDrink,
            Drinking,
            Leaving
        }
        [Header("NPC Settings")]
        [SerializeField] private float evaluateTime = 2f;
        [SerializeField] private float priceToleranceBase = 1.0f;
        [SerializeField] private float playerRequiredDistance = 8f;

        private NavMeshAgent _agent;
        private NPCState _state = NPCState.Walking;
        private ServiceCounter _targetCounter;
        private float _evaluateTimer;
        private Transform _exitPoint;
        private Transform _playerTransform;
        private Animator _animator;

        // Seated table variables
        private CustomerTable _assignedTable;
        private Transform _assignedChair;
        private bool _isSitting;

        // Bar queue variables
        private bool _isInBarQueue = false;
        private float _queuePatienceTimer = 45f;
        private float _maxQueuePatience = 45f;
        private bool _patienceInitialized = false;
        private BarCustomerState _barState = BarCustomerState.None;
        private LemonEmpire.UI.NPCPatienceUI _patienceUI;

        public NPCState State => _state;
        public ServiceCounter TargetCounter => _targetCounter;
        public BarCustomerState BarState => _barState;
        public bool IsInBarQueue => _isInBarQueue;
        public float QueuePatienceTimer => _queuePatienceTimer;
        public float MaxQueuePatience => _maxQueuePatience;

        // Archetype / Beverage specifications
        public NPCArchetype Archetype { get; private set; }
        public float MinSugar { get; private set; }
        public float MaxSugar { get; private set; }
        public float MinCarbonation { get; private set; }
        public float MaxCarbonation { get; private set; }
        public float MinAlcohol { get; private set; }
        public float MaxAlcohol { get; private set; }
        public PackagingType? PreferredPackaging { get; private set; }
        public bool RequiresPackaging { get; private set; }
        public float MaxTemperature { get; private set; } = 100f;

        public NpcResponse CachedDialogResponse { get; set; }
        public float[] ReplyReactionBonuses { get; set; }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = 2.5f;
            _agent.stoppingDistance = 1.5f;

            // Add CapsuleCollider for player interaction (raycast)
            var col = GetComponent<CapsuleCollider>();
            if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.35f;
            col.height = 1.8f;

            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator != null)
            {
                _animator.SetBool("IsGrounded", true);
            }

            // Generate archetype based on active trend
            Archetype = ChooseArchetype();
            SetupArchetypeStats();
        }

        private void SetupArchetypeStats()
        {
            switch (Archetype)
            {
                case NPCArchetype.Kids:
                    MinSugar = 70f; MaxSugar = 100f;
                    MinAlcohol = 0f; MaxAlcohol = 0f;
                    MinCarbonation = 40f; MaxCarbonation = 100f;
                    PreferredPackaging = PackagingType.Plastic;
                    RequiresPackaging = false;
                    break;
                case NPCArchetype.Athletes:
                    MinSugar = 0f; MaxSugar = 15f;
                    MinAlcohol = 0f; MaxAlcohol = 0f;
                    MinCarbonation = 0f; MaxCarbonation = 30f;
                    PreferredPackaging = PackagingType.Can;
                    RequiresPackaging = false;
                    break;
                case NPCArchetype.Hipsters:
                    MinSugar = 20f; MaxSugar = 40f;
                    MinAlcohol = 0f; MaxAlcohol = 5f;
                    MinCarbonation = 0f; MaxCarbonation = 100f;
                    PreferredPackaging = PackagingType.Glass;
                    RequiresPackaging = true;
                    break;
                case NPCArchetype.PartyAnimals:
                    MinSugar = 0f; MaxSugar = 100f;
                    MinAlcohol = 12f; MaxAlcohol = 100f;
                    MinCarbonation = 60f; MaxCarbonation = 100f;
                    PreferredPackaging = null;
                    RequiresPackaging = false;
                    break;
            }

            MaxTemperature = 100f;

            // Apply Daily Trend overrides
            switch (GameEventManager.CurrentTrend)
            {
                case DailyTrend.HeatWave:
                    if (MinCarbonation < 50f) MinCarbonation = 50f;
                    if (MaxCarbonation < 50f) MaxCarbonation = 100f;
                    MaxTemperature = 10f;
                    break;
                case DailyTrend.PartyNight:
                    if (MinAlcohol < 12f) MinAlcohol = 12f;
                    if (MaxAlcohol < 12f) MaxAlcohol = 100f;
                    break;
                case DailyTrend.KidDay:
                    if (MinSugar < 70f) MinSugar = 70f;
                    if (MaxSugar < 70f) MaxSugar = 100f;
                    MinAlcohol = 0f;
                    MaxAlcohol = 0f;
                    break;
                case DailyTrend.Marathon:
                    MinSugar = 0f;
                    MaxSugar = 15f;
                    MinAlcohol = 0f;
                    MaxAlcohol = 0f;
                    break;
            }
        }

        private NPCArchetype ChooseArchetype()
        {
            float rand = Random.value;
            bool hasNeon = UpgradeManager.HasNeonSigns;

            switch (GameEventManager.CurrentTrend)
            {
                case DailyTrend.HeatWave:
                    if (hasNeon)
                    {
                        if (rand < 0.40f) return NPCArchetype.Hipsters;
                        else if (rand < 0.55f) return NPCArchetype.Athletes;
                        else if (rand < 0.70f) return NPCArchetype.Kids;
                        else return NPCArchetype.PartyAnimals;
                    }
                    else
                    {
                        if (rand < 0.30f) return NPCArchetype.Hipsters;
                        else if (rand < 0.60f) return NPCArchetype.Athletes;
                        else if (rand < 0.80f) return NPCArchetype.Kids;
                        else return NPCArchetype.PartyAnimals;
                    }

                case DailyTrend.PartyNight:
                    if (hasNeon)
                    {
                        if (rand < 0.75f) return NPCArchetype.PartyAnimals;
                        else if (rand < 0.90f) return NPCArchetype.Hipsters;
                        else if (rand < 0.95f) return NPCArchetype.Kids;
                        else return NPCArchetype.Athletes;
                    }
                    else
                    {
                        if (rand < 0.80f) return NPCArchetype.PartyAnimals;
                        else if (rand < 0.8667f) return NPCArchetype.Kids;
                        else if (rand < 0.9334f) return NPCArchetype.Athletes;
                        else return NPCArchetype.Hipsters;
                    }

                case DailyTrend.KidDay:
                    if (hasNeon)
                    {
                        if (rand < 0.75f) return NPCArchetype.Kids;
                        else if (rand < 0.85f) return NPCArchetype.Hipsters;
                        else if (rand < 0.95f) return NPCArchetype.PartyAnimals;
                        else return NPCArchetype.Athletes;
                    }
                    else
                    {
                        if (rand < 0.80f) return NPCArchetype.Kids;
                        else if (rand < 0.8667f) return NPCArchetype.Athletes;
                        else if (rand < 0.9334f) return NPCArchetype.Hipsters;
                        else return NPCArchetype.PartyAnimals;
                    }

                case DailyTrend.Marathon:
                    if (hasNeon)
                    {
                        if (rand < 0.75f) return NPCArchetype.Athletes;
                        else if (rand < 0.875f) return NPCArchetype.Hipsters;
                        else if (rand < 1.0f) return NPCArchetype.PartyAnimals;
                        else return NPCArchetype.Kids;
                    }
                    else
                    {
                        if (rand < 0.80f) return NPCArchetype.Athletes;
                        else if (rand < 0.8667f) return NPCArchetype.Kids;
                        else if (rand < 0.9334f) return NPCArchetype.Hipsters;
                        else return NPCArchetype.PartyAnimals;
                    }

                case DailyTrend.Normal:
                default:
                    if (hasNeon)
                    {
                        if (rand < 0.35f) return NPCArchetype.Hipsters;
                        else if (rand < 0.70f) return NPCArchetype.PartyAnimals;
                        else if (rand < 0.85f) return NPCArchetype.Kids;
                        else return NPCArchetype.Athletes;
                    }
                    else
                    {
                        return (NPCArchetype)Random.Range(0, 4);
                    }
            }
        }

        public void Initialize(ServiceCounter counter, Transform exitPoint, Transform player)
        {
            _targetCounter = counter;
            _exitPoint = exitPoint;
            _playerTransform = player;
            _state = NPCState.Walking;

            bool wentToTable = false;
            if (UpgradeManager.IsLeftHallUnlocked)
            {
                if (Random.value < 0.40f)
                {
                    CustomerTable[] tables = FindObjectsByType<CustomerTable>(FindObjectsSortMode.None);
                    List<CustomerTable> emptyTables = new List<CustomerTable>();
                    foreach (var t in tables)
                    {
                        if (t != null && t.IsEmpty)
                        {
                            emptyTables.Add(t);
                        }
                    }

                    if (emptyTables.Count > 0)
                    {
                        CustomerTable chosenTable = emptyTables[Random.Range(0, emptyTables.Count)];
                        if (chosenTable.AssignNPC(this))
                        {
                            wentToTable = true;
                            if (chosenTable.SeatedCount == 1 && Random.value < 0.50f)
                            {
                                SpawnCompanionNPC(chosenTable);
                            }
                        }
                    }
                }
            }

            if (!wentToTable)
            {
                if (_targetCounter != null)
                {
                    _isInBarQueue = true;
                    _barState = BarCustomerState.WalkingToQueue;
                    if (_agent == null) _agent = GetComponent<NavMeshAgent>();
                    if (_agent != null) _agent.stoppingDistance = 0.1f;
                    _targetCounter.RegisterCustomer(this);

                    // Add Patience UI
                    var patienceUIGo = new GameObject("PatienceUIContainer");
                    patienceUIGo.transform.SetParent(transform, false);
                    _patienceUI = patienceUIGo.AddComponent<LemonEmpire.UI.NPCPatienceUI>();
                    _patienceUI.Initialize(this);
                }

                float precalcReaction = CalculateReactionScore();
                if (DialogueUI.Instance != null)
                    DialogueUI.Instance.PrefetchDialogue(this, precalcReaction);
            }
        }

        public void InitializeWithTable(CustomerTable table, Transform exitPoint, Transform player)
        {
            _exitPoint = exitPoint;
            _playerTransform = player;
            _state = NPCState.Walking;
            table.AssignNPC(this);
        }

        private void SpawnCompanionNPC(CustomerTable table)
        {
            var spawner = FindFirstObjectByType<NPCSpawner>();
            if (spawner != null)
            {
                GameObject companionGo = Instantiate(gameObject, spawner.transform.position, Quaternion.identity);
                companionGo.name = "NPC_Customer_Companion";
                var companionBuyer = companionGo.GetComponent<NPCBuyer>();
                if (companionBuyer != null)
                {
                    companionBuyer.InitializeWithTable(table, _exitPoint, _playerTransform);
                }
            }
        }

        public void AssignTable(CustomerTable table, Transform chair)
        {
            _assignedTable = table;
            _assignedChair = chair;
            _isSitting = false;

            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (!_agent.enabled) _agent.enabled = true;

            if (chair != null)
            {
                SetDestinationSafe(chair.position);
            }
            else
            {
                SetDestinationSafe(table.transform.position);
            }
        }

        private float CalculateReactionScore()
        {
            return 50f + Random.Range(-20f, 20f);
        }

        private void Update()
        {
            if (_isInBarQueue)
            {
                UpdateBarQueueCustomer();
                return;
            }

            if (_animator != null)
            {
                float currentSpeed = (_agent != null && _agent.enabled && _agent.isOnNavMesh) ? _agent.velocity.magnitude : 0f;
                _animator.SetFloat("Speed", currentSpeed);
            }

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            if (_assignedTable != null)
            {
                UpdateTableGuest();
                return;
            }

            switch (_state)
            {
                case NPCState.Walking:
                    UpdateWalking();
                    break;
                case NPCState.Evaluating:
                    UpdateEvaluating();
                    break;
                case NPCState.Buying:
                    _state = NPCState.Leaving;
                    GoToExit();
                    break;
                case NPCState.Leaving:
                    UpdateLeaving();
                    break;
            }
        }

        private void UpdateTableGuest()
        {
            if (_state == NPCState.Leaving)
            {
                UpdateLeaving();
                return;
            }

            if (!_isSitting)
            {
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    _isSitting = true;
                    _agent.enabled = false;
                    if (_assignedChair != null)
                    {
                        transform.position = _assignedChair.position;
                        transform.rotation = _assignedChair.rotation;
                    }
                    else
                    {
                        transform.position = _assignedTable.transform.position;
                    }
                }
            }
        }

        private void UpdateWalking()
        {
            if (_targetCounter == null)
            {
                _state = NPCState.Leaving;
                GoToExit();
                return;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                if (!IsPlayerNearby())
                {
                    _state = NPCState.Leaving;
                    GoToExit();
                    return;
                }

                _state = NPCState.Evaluating;
                float baseWait = evaluateTime > 5f ? evaluateTime : 15f;
                float cleanliness = 100f;
                if (LemonEmpire.Core.ShopCleanlinessManager.Instance != null)
                {
                    cleanliness = LemonEmpire.Core.ShopCleanlinessManager.Instance.Cleanliness;
                }
                float patienceMultiplier = Mathf.Clamp(cleanliness / 100f, 0.1f, 1f);

                bool isJukeboxPlaying = false;
                if (UpgradeManager.HasJukebox)
                {
                    var jb = FindFirstObjectByType<Jukebox>();
                    if (jb != null && jb.IsPlaying)
                    {
                        isJukeboxPlaying = true;
                    }
                }

                if (isJukeboxPlaying)
                {
                    _evaluateTimer = baseWait * patienceMultiplier * 2.0f; // doubled patience!
                }
                else
                {
                    _evaluateTimer = baseWait * patienceMultiplier;
                }
            }
        }

        private bool _dialogueStarted;

        private void UpdateEvaluating()
        {
            if (_dialogueStarted) return;

            if (_targetCounter == null)
            {
                _state = NPCState.Leaving;
                GoToExit();
                return;
            }

            // Check if there is a drink placed on the service counter
            if (_targetCounter.PlacedDrink != null)
            {
                // Check cleanliness before starting transaction
                float cleanliness = 100f;
                if (LemonEmpire.Core.ShopCleanlinessManager.Instance != null)
                {
                    cleanliness = LemonEmpire.Core.ShopCleanlinessManager.Instance.Cleanliness;
                }

                if (Random.value > (cleanliness / 100f))
                {
                    Debug.Log($"{gameObject.name} walked out in disgust due to dirty shop ({cleanliness:F1}% cleanliness).");
                    _state = NPCState.Leaving;
                    GoToExit();
                    return;
                }

                float quality = _targetCounter.PlacedDrink.Quality;
                float reaction = (quality * 0.5f) + Random.Range(-20f, 20f);

                var dialogueUI = UI.DialogueUI.Instance;
                if (dialogueUI != null && !dialogueUI.IsActive)
                {
                    _dialogueStarted = true;
                    dialogueUI.StartDialogue(this, reaction, (success) =>
                    {
                        if (success)
                        {
                            _state = NPCState.Buying;
                        }
                        else
                        {
                            _state = NPCState.Leaving;
                            GoToExit();
                        }
                        _dialogueStarted = false;
                    });
                }
            }
            else
            {
                // Wait for the player to place a drink
                _evaluateTimer -= Time.deltaTime;
                if (_evaluateTimer <= 0f)
                {
                    Debug.Log($"{gameObject.name} left the counter due to waiting too long for a drink.");
                    _state = NPCState.Leaving;
                    GoToExit();
                }
            }
        }

        private void UpdateLeaving()
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            if (!_agent.pathPending)
            {
                // Verify that we are close to the destination (or remaining distance is small on a valid path)
                float distToDest = Vector3.Distance(transform.position, _agent.destination);
                if (distToDest <= _agent.stoppingDistance + 0.5f || (_agent.remainingDistance <= _agent.stoppingDistance && _agent.hasPath && _agent.velocity.sqrMagnitude < 0.01f))
                {
                    Destroy(gameObject);
                }
            }
        }

        public void Flee()
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.speed = 6.0f; // Fast fleeing speed
            }
        }

        private bool IsPlayerNearby()
        {
            if (_playerTransform == null) return true;
            return Vector3.Distance(_playerTransform.position, transform.position) <= playerRequiredDistance;
        }

        private void GoToExit()
        {
            if (_exitPoint != null)
                SetDestinationSafe(_exitPoint.position);
            else
                SetDestinationSafe(transform.position + Vector3.forward * 20f);
        }

        private void SetDestinationSafe(Vector3 target)
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (!_agent.enabled) return;

            if (_agent.isOnNavMesh)
            {
                _agent.SetDestination(target);
            }
            else
            {
                StartCoroutine(SetDestinationWhenOnNavMesh(target));
            }
        }

        private IEnumerator SetDestinationWhenOnNavMesh(Vector3 target)
        {
            while (_agent != null && _agent.enabled && !_agent.isOnNavMesh)
            {
                yield return null;
            }
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(target);
            }
        }

        public void ForceLeave()
        {
            _isSitting = false;
            _assignedTable = null;
            _assignedChair = null;
            _state = NPCState.Leaving;

            if (!_agent.enabled)
            {
                _agent.enabled = true;
            }
            GoToExit();
        }

        public void InitializeQueuePatience(bool isForOrder)
        {
            float basePatience = isForOrder ? 45f : 60f;
            float cleanliness = 100f;
            if (LemonEmpire.Core.ShopCleanlinessManager.Instance != null)
            {
                cleanliness = LemonEmpire.Core.ShopCleanlinessManager.Instance.Cleanliness;
            }
            float patienceMultiplier = Mathf.Clamp(cleanliness / 100f, 0.1f, 1f);
            
            bool isJukeboxPlaying = false;
            if (UpgradeManager.HasJukebox)
            {
                var jb = FindFirstObjectByType<Jukebox>();
                if (jb != null && jb.IsPlaying)
                {
                    isJukeboxPlaying = true;
                }
            }

            if (isJukeboxPlaying)
            {
                _queuePatienceTimer = basePatience * patienceMultiplier * 2.0f; // doubled patience!
            }
            else
            {
                _queuePatienceTimer = basePatience * patienceMultiplier;
            }

            _maxQueuePatience = _queuePatienceTimer;
            _patienceInitialized = true;
        }

        public void SetQueueDestination(Vector3 destination, bool isFront)
        {
            SetDestinationSafe(destination);
            if (_barState == BarCustomerState.WalkingToQueue)
            {
                StartCoroutine(WaitUntilReachedFirstSpot(destination, isFront));
            }
            else
            {
                if (isFront && _barState == BarCustomerState.WaitingInLine)
                {
                    _barState = BarCustomerState.WaitingForOrder;
                    if (_targetCounter != null)
                    {
                        transform.rotation = Quaternion.LookRotation(_targetCounter.transform.position - transform.position);
                    }
                }
            }
        }

        private IEnumerator WaitUntilReachedFirstSpot(Vector3 destination, bool isFront)
        {
            while (Vector3.Distance(transform.position, destination) > 0.35f)
            {
                yield return null;
            }

            if (_barState == BarCustomerState.WalkingToQueue)
            {
                if (isFront)
                {
                    _barState = BarCustomerState.WaitingForOrder;
                    if (_targetCounter != null)
                    {
                        transform.rotation = Quaternion.LookRotation(_targetCounter.transform.position - transform.position);
                    }
                }
                else
                {
                    _barState = BarCustomerState.WaitingInLine;
                }
                InitializeQueuePatience(true);
            }
        }

        private void UpdateBarQueueCustomer()
        {
            if (_animator != null)
            {
                float currentSpeed = (_agent != null && _agent.enabled && _agent.isOnNavMesh) ? _agent.velocity.magnitude : 0f;
                _animator.SetFloat("Speed", currentSpeed);
            }

            if (_barState == BarCustomerState.Leaving)
            {
                UpdateLeaving();
                return;
            }

            if (_barState == BarCustomerState.Drinking)
            {
                return;
            }

            // Patience tick
            if (_barState == BarCustomerState.WaitingInLine || 
                _barState == BarCustomerState.WaitingForOrder || 
                _barState == BarCustomerState.WaitingForDrink)
            {
                _queuePatienceTimer -= Time.deltaTime;
                if (_queuePatienceTimer <= 0f)
                {
                    HandleQueueTimeout();
                }
            }
        }

        private void HandleQueueTimeout()
        {
            Debug.Log($"[NPCBuyer] Customer {gameObject.name} timed out in bar queue!");
            
            if (PlayerVitals.Instance != null)
            {
                PlayerVitals.Instance.OnDealFailed();
            }
            
            if (_targetCounter != null)
            {
                _targetCounter.UnregisterCustomer(this);
            }

            if (_patienceUI != null)
            {
                Destroy(_patienceUI.gameObject);
                _patienceUI = null;
            }

            _barState = BarCustomerState.Leaving;
            _isInBarQueue = false;
            _state = NPCState.Leaving;
            
            if (_agent != null)
            {
                _agent.stoppingDistance = 1.5f; // Restore default
                if (!_agent.enabled) _agent.enabled = true;
            }

            GoToExit();
        }

        public void SetDrinkingState(ItemBase drink)
        {
            _barState = BarCustomerState.Drinking;
            
            if (_agent != null)
            {
                _agent.enabled = false;
            }

            if (_animator != null)
            {
                _animator.SetFloat("Speed", 0f);
            }

            if (_patienceUI != null)
            {
                Destroy(_patienceUI.gameObject);
                _patienceUI = null;
            }

            StartCoroutine(DrinkingRoutine(drink));
        }

        private IEnumerator DrinkingRoutine(ItemBase drink)
        {
            yield return new WaitForSeconds(3.0f);

            if (_targetCounter != null)
            {
                _targetCounter.ClearPlacedDrink();
                _targetCounter.UnregisterCustomer(this);
            }

            _barState = BarCustomerState.Leaving;
            _isInBarQueue = false;
            _state = NPCState.Leaving;

            if (_agent != null)
            {
                _agent.stoppingDistance = 1.5f; // Restore default
                _agent.enabled = true;
            }

            GoToExit();
        }

        public void TransitionToWaitingForDrink()
        {
            _barState = BarCustomerState.WaitingForDrink;
            InitializeQueuePatience(false); // drink patience
        }

        #region IInteractable Implementation
        public bool CanInteract
        {
            get
            {
                if (!_isInBarQueue || _targetCounter == null) return false;
                
                // Only allow interaction if this NPC is the first one in the queue
                if (_targetCounter.ActiveCustomer != this) return false;

                if (!_targetCounter.IsPlayerBehindCounter()) return false;

                return _barState == BarCustomerState.WaitingForOrder || _barState == BarCustomerState.WaitingForDrink;
            }
        }

        public string InteractionPrompt
        {
            get
            {
                bool paranoia = PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.IsLemonParanoiaActive;
                string nameStr = paranoia ? "Шпион конкурентов" : TranslateArchetype(Archetype);

                if (_barState == BarCustomerState.WaitingForOrder)
                {
                    return paranoia 
                        ? $"[E] Допросить шпиона ({nameStr})"
                        : $"[E] Принять заказ у {nameStr}";
                }
                else if (_barState == BarCustomerState.WaitingForDrink)
                {
                    var carry = FindFirstObjectByType<PlayerCarry>();
                    if (carry != null && carry.IsCarrying && carry.CarriedItem.ItemType == ItemType.BottledLemonade)
                    {
                        return paranoia
                            ? $"[E] Швырнуть {carry.CarriedItem.DisplayName} в лицо шпиону"
                            : $"[E] Подать {carry.CarriedItem.DisplayName} клиенту";
                    }
                    else
                    {
                        return $"{nameStr} ожидает напиток";
                    }
                }
                return "";
            }
        }

        public void Interact(PlayerInteractionContext context)
        {
            if (!CanInteract) return;

            bool paranoia = PlayerStatusEffects.Instance != null && PlayerStatusEffects.Instance.IsLemonParanoiaActive;
            if (paranoia)
            {
                var dialogueUI = UI.DialogueUI.Instance;
                if (dialogueUI != null && !dialogueUI.IsActive)
                {
                    float quality = 100f;
                    if (context.PlayerCarry != null && context.PlayerCarry.IsCarrying && context.PlayerCarry.CarriedItem != null)
                    {
                        quality = context.PlayerCarry.CarriedItem.Quality;
                    }
                    float reaction = (quality * 0.5f) + Random.Range(-20f, 20f);

                    dialogueUI.StartDialogue(this, reaction, (success) => {
                        if (success)
                        {
                            if (context.PlayerCarry != null && context.PlayerCarry.IsCarrying)
                            {
                                var drink = context.PlayerCarry.TakeItem();
                                if (drink != null)
                                {
                                    _targetCounter.DeliverDrink(drink, this);
                                }
                                else
                                {
                                    _barState = BarCustomerState.Leaving;
                                    _isInBarQueue = false;
                                    _state = NPCState.Leaving;
                                    
                                    if (_targetCounter != null)
                                    {
                                        _targetCounter.UnregisterCustomer(this);
                                    }

                                    if (_patienceUI != null)
                                    {
                                        Destroy(_patienceUI.gameObject);
                                        _patienceUI = null;
                                    }

                                    if (_agent != null)
                                    {
                                        _agent.stoppingDistance = 1.5f;
                                        _agent.enabled = true;
                                    }

                                    GoToExit();
                                }
                            }
                            else
                            {
                                _barState = BarCustomerState.Leaving;
                                _isInBarQueue = false;
                                _state = NPCState.Leaving;

                                if (_targetCounter != null)
                                {
                                    _targetCounter.UnregisterCustomer(this);
                                }

                                if (_patienceUI != null)
                                {
                                    Destroy(_patienceUI.gameObject);
                                    _patienceUI = null;
                                }

                                if (_agent != null)
                                {
                                    _agent.stoppingDistance = 1.5f;
                                    _agent.enabled = true;
                                }

                                GoToExit();
                            }
                        }
                        else
                        {
                            _barState = BarCustomerState.Leaving;
                            _isInBarQueue = false;
                            _state = NPCState.Leaving;

                            if (_targetCounter != null)
                            {
                                _targetCounter.UnregisterCustomer(this);
                            }

                            if (_patienceUI != null)
                            {
                                Destroy(_patienceUI.gameObject);
                                _patienceUI = null;
                            }

                            if (_agent != null)
                            {
                                _agent.stoppingDistance = 1.5f;
                                _agent.enabled = true;
                            }

                            GoToExit();
                        }
                    });
                }
                return;
            }

            if (_barState == BarCustomerState.WaitingForOrder)
            {
                _targetCounter.TakeOrder(this);
            }
            else if (_barState == BarCustomerState.WaitingForDrink)
            {
                if (context.PlayerCarry != null && context.PlayerCarry.IsCarrying)
                {
                    var item = context.PlayerCarry.CarriedItem;
                    if (item != null && item.ItemType == ItemType.BottledLemonade)
                    {
                        ItemBase drink = context.PlayerCarry.TakeItem();
                        if (drink != null)
                        {
                            _targetCounter.DeliverDrink(drink, this);
                        }
                    }
                }
            }
        }

        private string TranslateArchetype(NPCArchetype archetype)
        {
            switch (archetype)
            {
                case NPCArchetype.Kids: return "Ребенок";
                case NPCArchetype.Athletes: return "Спортсмен";
                case NPCArchetype.Hipsters: return "Хипстер";
                case NPCArchetype.PartyAnimals: return "Тусовщик";
                default: return "Покупатель";
            }
        }
        #endregion
    }
}

