using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace LemonEmpire.Editor
{
    public class CityGenerator : EditorWindow
    {
        [MenuItem("Lemon Empire/Generate City Environment")]
        public static void GenerateCity()
        {
            // 1. Find and destroy existing CityEnvironment
            GameObject oldEnv = GameObject.Find("CityEnvironment");
            if (oldEnv != null)
            {
                Undo.DestroyObjectImmediate(oldEnv);
                Debug.Log("Destroyed existing CityEnvironment.");
            }

            // 2. Create new root
            GameObject root = new GameObject("CityEnvironment");
            Undo.RegisterCreatedObjectUndo(root, "Generate City Environment");

            // Setup shaders and materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material grassMat = new Material(litShader);
            grassMat.color = new Color(0.18f, 0.45f, 0.25f, 1f); // Dark green grass

            Material asphaltMat = new Material(litShader);
            asphaltMat.color = new Color(0.15f, 0.15f, 0.16f, 1f); // Dark gray asphalt

            Material sidewalkMat = new Material(litShader);
            sidewalkMat.color = new Color(0.62f, 0.62f, 0.65f, 1f); // Light gray sidewalk

            Material stripeMat = new Material(litShader);
            stripeMat.color = Color.white;

            Material trunkMat = new Material(litShader);
            trunkMat.color = new Color(0.42f, 0.28f, 0.14f, 1f); // Brown tree trunk

            Material leavesMat = new Material(litShader);
            leavesMat.color = new Color(0.22f, 0.58f, 0.24f, 1f); // Green tree leaves

            Material cabinMat = new Material(litShader);
            cabinMat.color = new Color(0.2f, 0.2f, 0.22f, 1f); // Dark windows

            Material wheelMat = new Material(litShader);
            wheelMat.color = Color.black;

            Color[] carColors = new Color[] {
                new Color(0.85f, 0.2f, 0.2f),   // Red
                new Color(0.2f, 0.4f, 0.85f),   // Blue
                new Color(0.85f, 0.65f, 0.15f), // Gold/Yellow
                new Color(0.25f, 0.7f, 0.3f),   // Green
                new Color(0.85f, 0.45f, 0.1f),  // Orange
                new Color(0.7f, 0.7f, 0.75f),   // Silver
                new Color(0.45f, 0.2f, 0.75f)   // Purple
            };

            // 3. Create Grass Base Plane (large green area)
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grass.name = "GrassBase";
            grass.transform.SetParent(root.transform);
            grass.transform.position = new Vector3(0f, -1.45f, 0f);
            grass.transform.localScale = new Vector3(200f, 0.1f, 200f);
            grass.GetComponent<Renderer>().sharedMaterial = grassMat;

            // 4. Create Ring Roads
            // North Road
            CreateRoad(root.transform, "NorthRoad", new Vector3(0f, -1.41f, 22f), new Vector3(80f, 0.05f, 8f), asphaltMat);
            // South Road
            CreateRoad(root.transform, "SouthRoad", new Vector3(0f, -1.41f, -22f), new Vector3(80f, 0.05f, 8f), asphaltMat);
            // East Road
            CreateRoad(root.transform, "EastRoad", new Vector3(22f, -1.41f, 0f), new Vector3(8f, 0.05f, 80f), asphaltMat);
            // West Road
            CreateRoad(root.transform, "WestRoad", new Vector3(-22f, -1.41f, 0f), new Vector3(8f, 0.05f, 80f), asphaltMat);

            // 5. Place Road markings (white center dashes)
            // North road dashes
            for (float x = -36f; x <= 36f; x += 6f)
            {
                if (Mathf.Abs(x - 22f) < 4f || Mathf.Abs(x + 22f) < 4f) continue; // Skip intersection
                CreateStripe(root.transform, new Vector3(x, -1.39f, 22f), new Vector3(1.5f, 0.01f, 0.15f), stripeMat);
            }
            // South road dashes
            for (float x = -36f; x <= 36f; x += 6f)
            {
                if (Mathf.Abs(x - 22f) < 4f || Mathf.Abs(x + 22f) < 4f) continue; // Skip intersection
                CreateStripe(root.transform, new Vector3(x, -1.39f, -22f), new Vector3(1.5f, 0.01f, 0.15f), stripeMat);
            }
            // East road dashes
            for (float z = -36f; z <= 36f; z += 6f)
            {
                if (Mathf.Abs(z - 22f) < 4f || Mathf.Abs(z + 22f) < 4f) continue; // Skip intersection
                CreateStripe(root.transform, new Vector3(22f, -1.39f, z), new Vector3(0.15f, 0.01f, 1.5f), stripeMat);
            }
            // West road dashes
            for (float z = -36f; z <= 36f; z += 6f)
            {
                if (Mathf.Abs(z - 22f) < 4f || Mathf.Abs(z + 22f) < 4f) continue; // Skip intersection
                CreateStripe(root.transform, new Vector3(-22f, -1.39f, z), new Vector3(0.15f, 0.01f, 1.5f), stripeMat);
            }

            // 6. Create Sidewalks around the building
            CreateRoad(root.transform, "NorthSidewalk", new Vector3(0f, -1.38f, 17f), new Vector3(34f, 0.06f, 2f), sidewalkMat);
            CreateRoad(root.transform, "SouthSidewalk", new Vector3(0f, -1.38f, -17f), new Vector3(34f, 0.06f, 2f), sidewalkMat);
            CreateRoad(root.transform, "EastSidewalk", new Vector3(17f, -1.38f, 0f), new Vector3(2f, 0.06f, 34f), sidewalkMat);
            CreateRoad(root.transform, "WestSidewalk", new Vector3(-17f, -1.38f, 0f), new Vector3(2f, 0.06f, 34f), sidewalkMat);

            // 7. Parking Lot (North side)
            CreateRoad(root.transform, "ParkingLot", new Vector3(0f, -1.40f, 14f), new Vector3(20f, 0.02f, 4f), asphaltMat);
            // Parking slot lines
            for (float x = -9f; x <= 9f; x += 3f)
            {
                CreateStripe(root.transform, new Vector3(x, -1.385f, 14f), new Vector3(0.1f, 0.01f, 3.5f), stripeMat);
            }

            // 8. Place Procedural Cars
            // In Parking Lot
            CreateProceduralCar(new Vector3(-6f, -1.38f, 14f), Quaternion.Euler(0f, 180f, 0f), root.transform, carColors[0], cabinMat, wheelMat);
            CreateProceduralCar(new Vector3(-3f, -1.38f, 14f), Quaternion.Euler(0f, 180f, 0f), root.transform, carColors[1], cabinMat, wheelMat);
            CreateProceduralCar(new Vector3(3f, -1.38f, 14f), Quaternion.Euler(0f, 180f, 0f), root.transform, carColors[2], cabinMat, wheelMat);
            CreateProceduralCar(new Vector3(6f, -1.38f, 14f), Quaternion.Euler(0f, 180f, 0f), root.transform, carColors[3], cabinMat, wheelMat);
            // Parked along side of East and West roads
            CreateProceduralCar(new Vector3(25.5f, -1.38f, -10f), Quaternion.Euler(0f, 0f, 0f), root.transform, carColors[4], cabinMat, wheelMat);
            CreateProceduralCar(new Vector3(25.5f, -1.38f, 5f), Quaternion.Euler(0f, 0f, 0f), root.transform, carColors[5], cabinMat, wheelMat);
            CreateProceduralCar(new Vector3(-25.5f, -1.38f, -8f), Quaternion.Euler(0f, 180f, 0f), root.transform, carColors[6], cabinMat, wheelMat);
            CreateProceduralCar(new Vector3(-25.5f, -1.38f, 8f), Quaternion.Euler(0f, 180f, 0f), root.transform, carColors[0], cabinMat, wheelMat);

            // 9. Place Procedural Sidewalk Trees
            // North sidewalk trees
            for (float x = -15f; x <= 15f; x += 6f)
            {
                if (Mathf.Abs(x) < 2f) continue; // Keep storefront clear
                CreateProceduralTree(new Vector3(x, -1.38f, 17f), root.transform, trunkMat, leavesMat);
            }
            // South sidewalk trees
            for (float x = -15f; x <= 15f; x += 6f)
            {
                CreateProceduralTree(new Vector3(x, -1.38f, -17f), root.transform, trunkMat, leavesMat);
            }
            // East sidewalk trees
            for (float z = -12f; z <= 12f; z += 6f)
            {
                CreateProceduralTree(new Vector3(17f, -1.38f, z), root.transform, trunkMat, leavesMat);
            }
            // West sidewalk trees
            for (float z = -12f; z <= 12f; z += 6f)
            {
                CreateProceduralTree(new Vector3(-17f, -1.38f, z), root.transform, trunkMat, leavesMat);
            }

            // 10. Instantiate CityKit Buildings
            string[] buildingPrefabs = new string[] {
                "building-a", "building-b", "building-c", "building-d", "building-e",
                "building-f", "building-g", "building-h", "building-i", "building-j",
                "building-k", "building-l", "building-m", "building-n",
                "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c",
                "building-skyscraper-d", "building-skyscraper-e"
            };

            int buildingIndex = 0;

            // North Row (Z = 32f)
            for (float x = -45f; x <= 45f; x += 15f)
            {
                string name = buildingPrefabs[buildingIndex % buildingPrefabs.Length];
                buildingIndex++;
                SpawnCityBuilding(name, new Vector3(x, -1.38f, 32f), Quaternion.Euler(0f, 180f, 0f), root.transform);
            }

            // South Row (Z = -32f)
            for (float x = -45f; x <= 45f; x += 15f)
            {
                string name = buildingPrefabs[buildingIndex % buildingPrefabs.Length];
                buildingIndex++;
                SpawnCityBuilding(name, new Vector3(x, -1.38f, -32f), Quaternion.Euler(0f, 0f, 0f), root.transform);
            }

            // East Row (X = 32f)
            for (float z = -15f; z <= 15f; z += 15f)
            {
                string name = buildingPrefabs[buildingIndex % buildingPrefabs.Length];
                buildingIndex++;
                SpawnCityBuilding(name, new Vector3(32f, -1.38f, z), Quaternion.Euler(0f, 270f, 0f), root.transform);
            }

            // West Row (X = -32f)
            for (float z = -15f; z <= 15f; z += 15f)
            {
                string name = buildingPrefabs[buildingIndex % buildingPrefabs.Length];
                buildingIndex++;
                SpawnCityBuilding(name, new Vector3(-32f, -1.38f, z), Quaternion.Euler(0f, 90f, 0f), root.transform);
            }

            // Save scene changes
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("City Environment successfully generated!");
        }

        private static void CreateRoad(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject rd = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rd.name = name;
            rd.transform.SetParent(parent);
            rd.transform.position = pos;
            rd.transform.localScale = scale;
            rd.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateStripe(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject st = GameObject.CreatePrimitive(PrimitiveType.Quad);
            st.name = "RoadStripe";
            st.transform.SetParent(parent);
            st.transform.position = pos;
            // Orient quad horizontally
            st.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            st.transform.localScale = new Vector3(scale.x, scale.z, 1f); // Quad size uses x and y
            st.GetComponent<Renderer>().sharedMaterial = mat;
            
            // Remove MeshCollider to prevent physics collisions with stripes
            var col = st.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        private static void CreateProceduralCar(Vector3 pos, Quaternion rot, Transform parent, Color bodyColor, Material cabinMat, Material wheelMat)
        {
            GameObject car = new GameObject("ProceduralCar");
            car.transform.SetParent(parent);
            car.transform.position = pos;
            car.transform.rotation = rot;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            Material bodyMat = new Material(litShader);
            bodyMat.color = bodyColor;

            // Chassis
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(car.transform, false);
            chassis.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            chassis.transform.localScale = new Vector3(1.4f, 0.5f, 2.8f);
            chassis.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Cabin
            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(car.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 0.8f, -0.2f);
            cabin.transform.localScale = new Vector3(1.2f, 0.45f, 1.5f);
            cabin.GetComponent<Renderer>().sharedMaterial = cabinMat;

            // Wheels (Cylinders rotated on Z axis)
            float wheelRadius = 0.28f;
            float wheelWidth = 0.2f;
            Vector3[] wheelOffsets = new Vector3[] {
                new Vector3(-0.75f, 0.28f, 0.8f),
                new Vector3(0.75f, 0.28f, 0.8f),
                new Vector3(-0.75f, 0.28f, -0.8f),
                new Vector3(0.75f, 0.28f, -0.8f)
            };

            for (int i = 0; i < wheelOffsets.Length; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel_" + i;
                wheel.transform.SetParent(car.transform, false);
                wheel.transform.localPosition = wheelOffsets[i];
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(wheelRadius * 2f, wheelWidth, wheelRadius * 2f);
                wheel.GetComponent<Renderer>().sharedMaterial = wheelMat;
            }
        }

        private static void CreateProceduralTree(Vector3 pos, Transform parent, Material trunkMat, Material leavesMat)
        {
            GameObject tree = new GameObject("ProceduralTree");
            tree.transform.SetParent(parent);
            tree.transform.position = pos;

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            trunk.transform.localScale = new Vector3(0.2f, 1.0f, 0.2f); // height is localScale.y * 2
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

            // Leaves
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Leaves";
            leaves.transform.SetParent(tree.transform, false);
            leaves.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            leaves.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            leaves.GetComponent<Renderer>().sharedMaterial = leavesMat;
        }

        private static void SpawnCityBuilding(string assetName, Vector3 pos, Quaternion rot, Transform parent)
        {
            string path = $"Assets/Models/CityKit/{assetName}.fbx";
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (fbxPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
                instance.transform.SetParent(parent);
                instance.transform.position = pos;
                instance.transform.rotation = rot;
                
                // Add static flags for lightmapping if desired
                GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            }
            else
            {
                Debug.LogWarning($"Could not load building prefab at path: {path}");
            }
        }
    }
}
