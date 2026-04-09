using UnityEngine;
using UnityEngine.AI;
using LemonEmpire.Core;

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

        [Header("Visuals")]
        [SerializeField] private Color npcColor = new Color(0.4f, 0.6f, 0.9f);

        private NavMeshAgent _agent;
        private NPCState _state = NPCState.Walking;
        private TradeStand _targetStand;
        private float _evaluateTimer;
        private Transform _exitPoint;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = 2.5f;
            _agent.stoppingDistance = 1.5f;

            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = npcColor;
                rend.material = mat;
            }
        }

        public void Initialize(TradeStand stand, Transform exitPoint)
        {
            _targetStand = stand;
            _exitPoint = exitPoint;
            _state = NPCState.Walking;

            if (_targetStand != null)
                _agent.SetDestination(_targetStand.transform.position);
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

                _state = NPCState.Evaluating;
                _evaluateTimer = evaluateTime;
            }
        }

        private void UpdateEvaluating()
        {
            _evaluateTimer -= Time.deltaTime;
            if (_evaluateTimer > 0f) return;

            if (_targetStand == null || !_targetStand.HasStock)
            {

                _state = NPCState.Leaving;
                GoToExit();
                return;
            }

            float quality = _targetStand.AverageQuality;
            float price = _targetStand.Price;

            float fairPrice = quality * priceToleranceBase;

            fairPrice *= Random.Range(0.8f, 1.4f);

            if (price <= fairPrice)
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

        private void UpdateLeaving()
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                Destroy(gameObject);
            }
        }

        private void GoToExit()
        {
            if (_exitPoint != null)
                _agent.SetDestination(_exitPoint.position);
            else
                _agent.SetDestination(transform.position + Vector3.forward * 20f);
        }
    }
}
