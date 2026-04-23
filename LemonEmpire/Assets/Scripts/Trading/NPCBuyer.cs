using UnityEngine;
using UnityEngine.AI;
using LemonEmpire.Core;
using LemonEmpire.UI;
using LemonEmpire.Network;

namespace LemonEmpire.Trading
{
    public enum NPCState
    {
        Walking,
        Evaluating,
        Buying,
        Leaving
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCBuyer : MonoBehaviour
    {
        [Header("NPC Settings")]
        [SerializeField] private float evaluateTime = 2f;
        [SerializeField] private float maxAcceptablePrice = 200f;
        [SerializeField] private float priceToleranceBase = 1.0f;
        [SerializeField] private float playerRequiredDistance = 8f;

        private NavMeshAgent _agent;
        private NPCState _state = NPCState.Walking;
        private TradeStand _targetStand;
        private float _evaluateTimer;
        private Transform _exitPoint;
        private Transform _playerTransform;

        public NPCState State => _state;
        public TradeStand TargetStand => _targetStand;

        public NpcResponse CachedDialogResponse { get; set; }
        public float[] ReplyReactionBonuses { get; set; }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = 2.5f;
            _agent.stoppingDistance = 1.5f;
        }

        public void Initialize(TradeStand stand, Transform exitPoint, Transform player)
        {
            _targetStand = stand;
            _exitPoint = exitPoint;
            _playerTransform = player;
            _state = NPCState.Walking;

            if (_targetStand != null)
                _agent.SetDestination(_targetStand.transform.position);

            float precalcReaction = CalculateReactionScore();
            if (DialogueUI.Instance != null)
                DialogueUI.Instance.PrefetchDialogue(this, precalcReaction);
        }

        private float CalculateReactionScore()
        {
            float quality = _targetStand != null ? _targetStand.AverageQuality : 50f;
            float playerLook = PlayerVitals.Instance != null ? PlayerVitals.Instance.LookScore : 0f;
            return (quality * 0.5f) + (playerLook * 0.3f) + Random.Range(-20f, 20f);
        }

        private void Update()
        {
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

        private void UpdateWalking()
        {
            if (_targetStand == null)
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
                _evaluateTimer = evaluateTime;
            }
        }

        private bool _dialogueStarted;

        private void UpdateEvaluating()
        {
            if (_dialogueStarted) return;

            _evaluateTimer -= Time.deltaTime;
            if (_evaluateTimer > 0f) return;

            if (_targetStand == null || !_targetStand.HasStock)
            {
                _state = NPCState.Leaving;
                GoToExit();
                return;
            }

            float quality = _targetStand.AverageQuality;

            // Optional: Recalculate dynamic values here if you want final price fairness to be exact.
            // But we will just use it to see if they buy or not. The reaction score was mostly precalculated.
            
            float playerLook = PlayerVitals.Instance != null ? PlayerVitals.Instance.LookScore : 0f;
            float reaction = (quality * 0.5f) + (playerLook * 0.3f) + Random.Range(-20f, 20f);

            var dialogueUI = UI.DialogueUI.Instance;
            if (dialogueUI != null && !dialogueUI.IsActive)
            {
                _dialogueStarted = true;
                dialogueUI.StartDialogue(this, reaction, (success) =>
                {
                    if (success)
                    {
                        // Sale logic is now handled internally inside DialogueUI (like SellMultiple handles 2-for-1 bargains)
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
            else
            {
                float fairPrice = quality * priceToleranceBase;
                fairPrice *= (1f + reaction * 0.01f);

                if (_targetStand.Price <= fairPrice)
                {
                    _targetStand.SellOne();
                    _state = NPCState.Buying;
                }
                else
                {
                    _state = NPCState.Leaving;
                    GoToExit();
                }
            }
        }

        private void UpdateLeaving()
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                Destroy(gameObject);
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
                _agent.SetDestination(_exitPoint.position);
            else
                _agent.SetDestination(transform.position + Vector3.forward * 20f);
        }

        public void ForceLeave()
        {
            _state = NPCState.Leaving;
            GoToExit();
        }
    }
}
