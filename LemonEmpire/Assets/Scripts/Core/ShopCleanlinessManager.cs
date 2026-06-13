using UnityEngine;
using LemonEmpire.Production;
using LemonEmpire.Trading;

namespace LemonEmpire.Core
{
    public class ShopCleanlinessManager : MonoBehaviour
    {
        public static ShopCleanlinessManager Instance { get; private set; }

        [Header("Cleanliness Settings")]
        [SerializeField] private float startCleanliness = 100f;
        [SerializeField] private float decayRatePerMess = 0.1f;    // Cleanliness loss per second per footprint
        [SerializeField] private float decayRatePerCrate = 0.05f;  // Cleanliness loss per second per empty crate

        [Header("Mess Spawning")]
        [SerializeField] private GameObject messSpotPrefab;
        [SerializeField] private float spawnInterval = 30f;
        [SerializeField] private Transform entrance1;
        [SerializeField] private Transform entrance2;

        private float _cleanliness = 100f;
        private float _spawnTimer;

        public float Cleanliness
        {
            get => _cleanliness;
            private set => _cleanliness = Mathf.Clamp(value, 0f, 100f);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _cleanliness = startCleanliness;
        }

        private void Start()
        {
            _spawnTimer = spawnInterval;

            // Attempt to auto-locate entrances if not assigned in editor
            FindEntrances();

            // Spawn broom if none exists in the scene
            var existingBrooms = FindObjectsByType<BroomTool>(FindObjectsSortMode.None);
            if (existingBrooms.Length == 0)
            {
                Vector3 spawnPos = new Vector3(-2f, -1.1f, 2f);
                var stand = FindFirstObjectByType<LemonEmpire.Trading.TradeStand>();
                if (stand != null)
                {
                    spawnPos = stand.transform.position - stand.transform.right * 2.5f + Vector3.up * 0.2f;
                }
                BroomTool.SpawnBroom(spawnPos);
            }
        }

        private void Update()
        {
            // Update cleanliness decay
            UpdateDecay();

            // Periodic footprint spawning
            UpdateMessSpawning();
        }

        private void FindEntrances()
        {
            if (entrance1 == null)
            {
                var go = GameObject.Find("Entrance 1");
                if (go == null) go = GameObject.Find("Entrance1");
                if (go != null) entrance1 = go.transform;
            }

            if (entrance2 == null)
            {
                var go = GameObject.Find("Entrance 2");
                if (go == null) go = GameObject.Find("Entrance2");
                if (go != null) entrance2 = go.transform;
            }
        }

        private void UpdateDecay()
        {
            int messCount = FindObjectsByType<MessSpot>(FindObjectsSortMode.None).Length;
            int crateCount = FindObjectsByType<EmptyCrate>(FindObjectsSortMode.None).Length;

            float decayAmount = (messCount * decayRatePerMess) + (crateCount * decayRatePerCrate);
            Cleanliness -= decayAmount * Time.deltaTime;
        }

        private void UpdateMessSpawning()
        {
            // Only spawn if active shift in TimeManager
            if (TimeManager.Instance != null && !TimeManager.Instance.IsShiftActive)
                return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = spawnInterval;
                SpawnMessSpot();
            }
        }

        public void SpawnMessSpot()
        {
            Transform targetEntrance = null;
            if (entrance1 != null && entrance2 != null)
            {
                targetEntrance = Random.value < 0.5f ? entrance1 : entrance2;
            }
            else if (entrance1 != null)
            {
                targetEntrance = entrance1;
            }
            else if (entrance2 != null)
            {
                targetEntrance = entrance2;
            }

            Vector3 spawnPos = Vector3.zero;
            bool foundPos = false;

            if (targetEntrance != null)
            {
                Vector2 circleOffset = Random.insideUnitCircle * 1.5f;
                spawnPos = targetEntrance.position + new Vector3(circleOffset.x, 0f, circleOffset.y);
                foundPos = true;
            }
            else
            {
                // Try to find active customer (NPCBuyer)
                var buyers = FindObjectsByType<NPCBuyer>(FindObjectsSortMode.None);
                if (buyers.Length > 0)
                {
                    var randomBuyer = buyers[Random.Range(0, buyers.Length)];
                    Vector2 circleOffset = Random.insideUnitCircle * 1.0f;
                    Vector3 testPos = randomBuyer.transform.position + new Vector3(circleOffset.x, 0f, circleOffset.y);
                    
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(testPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        spawnPos = hit.position;
                        foundPos = true;
                    }
                }

                // Try near ServiceCounter
                if (!foundPos)
                {
                    var counter = FindFirstObjectByType<ServiceCounter>();
                    if (counter != null)
                    {
                        Vector2 circleOffset = Random.insideUnitCircle * 4.0f;
                        Vector3 testPos = counter.transform.position + new Vector3(circleOffset.x, 0f, circleOffset.y);
                        
                        UnityEngine.AI.NavMeshHit hit;
                        if (UnityEngine.AI.NavMesh.SamplePosition(testPos, out hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
                        {
                            spawnPos = hit.position;
                            foundPos = true;
                        }
                    }
                }

                // Try random position inside the main shop floor (walkable)
                if (!foundPos)
                {
                    Vector3 randomPt = new Vector3(Random.Range(-9f, 9f), -1.30f, Random.Range(-9f, 9f));
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(randomPt, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        spawnPos = hit.position;
                        foundPos = true;
                    }
                }

                // Try near player
                if (!foundPos)
                {
                    var player = FindFirstObjectByType<Player.PlayerController>();
                    if (player != null)
                    {
                        Vector2 circleOffset = Random.insideUnitCircle * 3f;
                        spawnPos = player.transform.position + new Vector3(circleOffset.x, 0f, circleOffset.y);
                        foundPos = true;
                    }
                }
            }

            if (!foundPos)
            {
                spawnPos = new Vector3(Random.Range(-5f, 5f), -1.29f, Random.Range(-5f, 5f));
            }
            else
            {
                spawnPos.y = -1.29f; // prevent z-fighting with the floor
            }

            GameObject go;
            if (messSpotPrefab != null)
            {
                go = Instantiate(messSpotPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Create root object
                go = new GameObject("MessSpot_Footprint");
                go.transform.position = spawnPos;
                go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                // Load wet mud material
                Material mudMat = null;
#if UNITY_EDITOR
                mudMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WetMudMaterial.mat");
#endif
                if (mudMat == null)
                {
                    mudMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mudMat.color = new Color(0.28f, 0.18f, 0.10f);
                    mudMat.SetFloat("_Smoothness", 0.8f);
                }

                // Helper to spawn splash details
                System.Action<string, Vector3, Vector3> spawnSplash = (name, localPos, scale) =>
                {
                    var spl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    spl.name = name;
                    spl.transform.SetParent(go.transform, false);
                    spl.transform.localPosition = localPos;
                    spl.transform.localScale = scale;
                    var col = spl.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    var rend = spl.GetComponent<Renderer>();
                    if (rend != null) rend.sharedMaterial = mudMat;
                };

                // Spawn splash elements
                spawnSplash("MainSpot", Vector3.zero, new Vector3(0.35f, 0.001f, 0.2f));
                spawnSplash("Splash_1", new Vector3(0.22f, 0f, 0.08f), new Vector3(0.07f, 0.001f, 0.06f));
                spawnSplash("Splash_2", new Vector3(-0.18f, 0f, -0.10f), new Vector3(0.05f, 0.001f, 0.05f));
                spawnSplash("Splash_3", new Vector3(0.06f, 0f, -0.16f), new Vector3(0.06f, 0.001f, 0.04f));

                // Add trigger collider for player interaction
                var boxCol = go.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                boxCol.size = new Vector3(1.5f, 4f, 1.5f);

                go.AddComponent<MessSpot>();
            }

            Debug.Log($"[ShopCleanlinessManager] Footprint mess spawned at {spawnPos}");
        }

        public void IncreaseCleanliness(float amount)
        {
            Cleanliness += amount;
            Debug.Log($"[ShopCleanlinessManager] Cleanliness increased to {Cleanliness:F1}");
        }
    }
}
