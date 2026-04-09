using UnityEngine;
using UnityEngine.AI;
using LemonEmpire.Core;

namespace LemonEmpire.Trading
{

    public class NPCSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private float spawnInterval = 15f;
        [SerializeField] private float spawnIntervalVariance = 5f;
        [SerializeField] private int maxNPCs = 5;

        [Header("References")]
        [SerializeField] private TradeStand targetStand;
        [SerializeField] private Transform exitPoint;

        private float _spawnTimer;
        private int _currentNPCCount;

        private void Start()
        {
            _spawnTimer = spawnInterval * 0.5f;

            if (targetStand == null)
                targetStand = FindFirstObjectByType<TradeStand>();
        }

        private void Update()
        {
            if (TimeManager.Instance != null && !TimeManager.Instance.IsShiftActive)
                return;

            if (targetStand == null || !targetStand.HasStock)
                return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = spawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance);

                _currentNPCCount = FindObjectsByType<NPCBuyer>(FindObjectsSortMode.None).Length;
                if (_currentNPCCount >= maxNPCs) return;

                SpawnNPC();
            }
        }

        private void SpawnNPC()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "NPC_Customer";
            go.transform.position = transform.position + Random.insideUnitSphere * 1f;
            go.transform.position = new Vector3(
                go.transform.position.x,
                transform.position.y,
                go.transform.position.z);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 2.5f;
            agent.stoppingDistance = 1.5f;
            agent.radius = 0.3f;
            agent.height = 2f;

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(
                    Random.Range(0.3f, 0.8f),
                    Random.Range(0.3f, 0.8f),
                    Random.Range(0.3f, 0.8f));
                rend.material = mat;
            }

            var buyer = go.AddComponent<NPCBuyer>();
            buyer.Initialize(targetStand, exitPoint != null ? exitPoint : transform);
        }
    }
}
