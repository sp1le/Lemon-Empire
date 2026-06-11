using UnityEngine;
using UnityEngine.AI;
using LemonEmpire.Core;
using LemonEmpire.Production;

namespace LemonEmpire.Trading
{

    public class NPCSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private float spawnInterval = 15f;
        [SerializeField] private float spawnIntervalVariance = 5f;
        [SerializeField] private int maxNPCs = 5;

        [Header("Player Proximity")]
        [SerializeField] private float playerProximityRadius = 15f;

        [Header("References")]
        [SerializeField] private ServiceCounter targetCounter;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private GameObject npcPrefab;
        [SerializeField] private Material[] customerSkins;

        private float _spawnTimer;
        private int _currentNPCCount;
        private Transform _playerTransform;

        private void Start()
        {
            _spawnTimer = spawnInterval * 0.5f;

            if (targetCounter == null)
                targetCounter = FindFirstObjectByType<ServiceCounter>();

            var player = FindFirstObjectByType<Player.PlayerController>();
            if (player != null)
                _playerTransform = player.transform;
        }

        private void Update()
        {
            if (TimeManager.Instance != null && !TimeManager.Instance.IsShiftActive)
                return;

            if (targetCounter == null)
                return;

            if (!IsPlayerNearCounter())
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

        private bool IsPlayerNearCounter()
        {
            if (_playerTransform == null) return false;
            float dist = Vector3.Distance(_playerTransform.position, targetCounter.transform.position);
            return dist <= playerProximityRadius;
        }

        private void SpawnNPC()
        {
            GameObject go;
            if (npcPrefab != null)
            {
                go = Instantiate(npcPrefab);
                if (customerSkins != null && customerSkins.Length > 0)
                {
                    var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null)
                    {
                        smr.sharedMaterial = customerSkins[Random.Range(0, customerSkins.Length)];
                    }
                }
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);

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
            }

            go.name = "NPC_Customer";
            go.transform.position = transform.position + Random.insideUnitSphere * 1f;
            go.transform.position = new Vector3(
                go.transform.position.x,
                transform.position.y,
                go.transform.position.z);

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null) agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 2.5f;
            agent.stoppingDistance = 1.5f;
            agent.radius = 0.3f;
            agent.height = 2f;

            var buyer = go.GetComponent<NPCBuyer>();
            if (buyer == null) buyer = go.AddComponent<NPCBuyer>();
            buyer.Initialize(targetCounter, exitPoint != null ? exitPoint : transform, _playerTransform);
        }
    }
}
