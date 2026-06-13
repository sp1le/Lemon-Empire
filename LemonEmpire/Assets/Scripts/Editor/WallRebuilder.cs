using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using LemonEmpire.Production;

namespace LemonEmpire.Editor
{
    public class WallRebuilder
    {
        [MenuItem("Lemon Empire/Rebuild Modular Walls")]
        public static void RebuildWallsAndInterior()
        {
            Debug.Log("Rebuilding modular walls and modern loft interior...");

            // 1. Load materials
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WallMaterial.mat");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WoodSolidMaterial.mat");
            Material glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/GlassMaterial.mat");
            Material chromeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/ChromeMaterial.mat");
            Material plasticMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/BlackPlasticMaterial.mat");

            if (wallMat == null || woodMat == null || glassMat == null || chromeMat == null)
            {
                Debug.LogError("Required materials (WallMaterial, WoodSolidMaterial, GlassMaterial, ChromeMaterial) not found!");
                return;
            }

            // Create prefabs folder
            string folderPath = "Assets/Prefabs/Building";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            // 2. Build/Save Prefabs
            GameObject wallPrefab = CreateWallSegmentPrefab(folderPath, wallMat, woodMat);
            GameObject pillarPrefab = CreatePillarPrefab(folderPath, wallMat, woodMat);
            GameObject windowPrefab = CreateWindowPrefab(folderPath, wallMat, woodMat, glassMat);
            GameObject doorwayPrefab = CreateDoorwayPrefab(folderPath, wallMat, woodMat, chromeMat);
            GameObject ventPrefab = CreateVentPrefab(folderPath, chromeMat);
            GameObject lampPrefab = CreateLampPrefab(folderPath, plasticMat, chromeMat);

            // 3. Clear scene walls
            GameObject wallsGo = GameObject.Find("Walls");
            if (wallsGo == null)
            {
                wallsGo = new GameObject("Walls");
            }
            else
            {
                // Remove all existing children under Walls
                for (int i = wallsGo.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(wallsGo.transform.GetChild(i).gameObject);
                }
            }

            // Clear old ceiling decorations or hanging lamps if any exist
            GameObject oldInterior = GameObject.Find("LoftInteriorDecor");
            if (oldInterior != null)
            {
                Object.DestroyImmediate(oldInterior);
            }

            // Create new interior decor parent
            GameObject decorGo = new GameObject("LoftInteriorDecor");

            // 4. Place External Walls, Corner Pillars and Windows
            // Pillars at 4 corners
            SpawnPrefab(pillarPrefab, new Vector3(-12f, -1.35f, -12f), Quaternion.identity, wallsGo.transform);
            SpawnPrefab(pillarPrefab, new Vector3(12f, -1.35f, -12f), Quaternion.identity, wallsGo.transform);
            SpawnPrefab(pillarPrefab, new Vector3(-12f, -1.35f, 12f), Quaternion.identity, wallsGo.transform);
            SpawnPrefab(pillarPrefab, new Vector3(12f, -1.35f, 12f), Quaternion.identity, wallsGo.transform);

            // Left Wall (X = -12): Z goes from -11 to 11
            // Placing 12 segments of 2m, rotated 90 degrees (facing +X)
            for (float z = -11f; z <= 11f; z += 2f)
            {
                SpawnPrefab(wallPrefab, new Vector3(-12f, -1.35f, z), Quaternion.Euler(0f, 90f, 0f), wallsGo.transform);
            }

            // Right Wall (X = 12): Z goes from -11 to 11
            // Placing 12 segments of 2m, rotated -90 degrees (facing -X)
            // Z = 9 is the Warehouse black door (back entrance door) leading to delivery zone
            for (float z = -11f; z <= 11f; z += 2f)
            {
                if (z == 9f)
                {
                    // Spawn Black Entrance Door (single doorway prefab)
                    GameObject blackDoor = SpawnPrefab(doorwayPrefab, new Vector3(12f, -1.35f, 9f), Quaternion.Euler(0f, -90f, 0f), wallsGo.transform);
                    if (blackDoor != null)
                    {
                        var door = blackDoor.GetComponent<InteractiveDoor>();
                        if (door != null)
                        {
                            typeof(InteractiveDoor).GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, "Черный вход");
                            typeof(InteractiveDoor).GetField("openAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, 95f);
                        }
                    }
                }
                else
                {
                    SpawnPrefab(wallPrefab, new Vector3(12f, -1.35f, z), Quaternion.Euler(0f, -90f, 0f), wallsGo.transform);
                }
            }

            // Back Wall (Z = 12): X goes from -11 to 11
            // Placing 12 segments of 2m, rotated 180 degrees (facing -Z)
            for (float x = -11f; x <= 11f; x += 2f)
            {
                SpawnPrefab(wallPrefab, new Vector3(x, -1.35f, 12f), Quaternion.Euler(0f, 180f, 0f), wallsGo.transform);
            }

            // Front Wall (Z = -12): X goes from -11 to 11 (12 segments of 2m)
            // Balanced design with symmetrical entrance doors, central showcase window and side windows
            for (float x = -11f; x <= 11f; x += 2f)
            {
                if (x == -11f || x == -7f || x == -3f || x == 3f || x == 7f || x == 11f)
                {
                    SpawnPrefab(wallPrefab, new Vector3(x, -1.35f, -12f), Quaternion.identity, wallsGo.transform);
                }
                else if (x == -9f || x == -1f || x == 1f || x == 9f)
                {
                    SpawnPrefab(windowPrefab, new Vector3(x, -1.35f, -12f), Quaternion.identity, wallsGo.transform);
                }
                else if (x == -5f)
                {
                    // Spawn Left Entrance (Double Door) at X = -5
                    SpawnEntranceDoubleDoor(doorwayPrefab, new Vector3(-5f, -1.35f, -12f), Quaternion.identity, wallsGo.transform, "LeftEntranceDoubleDoor", true);
                }
                else if (x == 5f)
                {
                    // Spawn Main Entrance (Double Door) at X = 5
                    SpawnEntranceDoubleDoor(doorwayPrefab, new Vector3(5f, -1.35f, -12f), Quaternion.identity, wallsGo.transform, "MainEntranceDoubleDoor", false);
                }
            }


            // 5. Place Internal Walls & Dividers
            // Warehouse wall left: Z = 6, X from -12 to 0
            for (float x = -11f; x <= -1f; x += 2f)
            {
                SpawnPrefab(wallPrefab, new Vector3(x, -1.35f, 6f), Quaternion.identity, wallsGo.transform);
            }

            // Warehouse wall right: Z = 6, X from 0 to 10 (fully closed)
            for (float x = 1f; x <= 9f; x += 2f)
            {
                SpawnPrefab(wallPrefab, new Vector3(x, -1.35f, 6f), Quaternion.identity, wallsGo.transform);
            }

            // Shop-to-corridor entrance door at Z = 6, X = 11f (in corridor area)
            GameObject shopToCorridorDoor = SpawnPrefab(doorwayPrefab, new Vector3(11f, -1.35f, 6f), Quaternion.identity, wallsGo.transform);
            if (shopToCorridorDoor != null)
            {
                var door = shopToCorridorDoor.GetComponent<InteractiveDoor>();
                if (door != null)
                {
                    typeof(InteractiveDoor).GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, "Дверь в коридор");
                }
            }

            // Center shop divider LeftHallDividerBlocker: X = 0, Z from -12 to 6
            GameObject dividerParent = new GameObject("LeftHallDividerBlocker");
            dividerParent.transform.SetParent(wallsGo.transform);
            for (float z = -11f; z <= 5f; z += 2f)
            {
                GameObject wall = SpawnPrefab(wallPrefab, new Vector3(0f, -1.35f, z), Quaternion.Euler(0f, 90f, 0f), dividerParent.transform);
                if (wall != null)
                {
                    var obstacle = wall.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    obstacle.carving = true;
                    obstacle.size = new Vector3(2.0f, 3.5f, 0.3f);
                }
            }

            // Warehouse dividers (Z = 7, 9, 11 to fully close compartments)
            System.Action<GameObject> addObstacle = (goObj) => {
                if (goObj != null) {
                    var obs = goObj.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    obs.carving = true;
                    obs.size = new Vector3(2.0f, 3.5f, 0.3f);
                }
            };

            GameObject divL1 = new GameObject("WarehouseDividers_L1");
            divL1.transform.SetParent(wallsGo.transform);
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(0f, -1.35f, 7f), Quaternion.Euler(0f, 90f, 0f), divL1.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(0f, -1.35f, 9f), Quaternion.Euler(0f, 90f, 0f), divL1.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(0f, -1.35f, 11f), Quaternion.Euler(0f, 90f, 0f), divL1.transform));

            GameObject divL2 = new GameObject("WarehouseDividers_L2");
            divL2.transform.SetParent(wallsGo.transform);
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-3f, -1.35f, 7f), Quaternion.Euler(0f, 90f, 0f), divL2.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-3f, -1.35f, 9f), Quaternion.Euler(0f, 90f, 0f), divL2.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-3f, -1.35f, 11f), Quaternion.Euler(0f, 90f, 0f), divL2.transform));

            GameObject divL3 = new GameObject("WarehouseDividers_L3");
            divL3.transform.SetParent(wallsGo.transform);
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-6f, -1.35f, 7f), Quaternion.Euler(0f, 90f, 0f), divL3.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-6f, -1.35f, 9f), Quaternion.Euler(0f, 90f, 0f), divL3.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-6f, -1.35f, 11f), Quaternion.Euler(0f, 90f, 0f), divL3.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-9f, -1.35f, 7f), Quaternion.Euler(0f, 90f, 0f), divL3.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-9f, -1.35f, 9f), Quaternion.Euler(0f, 90f, 0f), divL3.transform));
            addObstacle(SpawnPrefab(wallPrefab, new Vector3(-9f, -1.35f, 11f), Quaternion.Euler(0f, 90f, 0f), divL3.transform));

            // Setup Warehouse Upgrade Unlocker Manager in scene
            GameObject warehouseUnlockerGo = GameObject.Find("WarehouseUpgradeUnlockerManager");
            if (warehouseUnlockerGo == null)
            {
                warehouseUnlockerGo = new GameObject("WarehouseUpgradeUnlockerManager");
            }
            var wUnlocker = warehouseUnlockerGo.GetComponent<WarehouseUpgradeUnlocker>();
            if (wUnlocker == null)
            {
                wUnlocker = warehouseUnlockerGo.AddComponent<WarehouseUpgradeUnlocker>();
            }

            var blocker1Field = typeof(WarehouseUpgradeUnlocker).GetField("blocker1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (blocker1Field != null) blocker1Field.SetValue(wUnlocker, divL1);

            var blocker2Field = typeof(WarehouseUpgradeUnlocker).GetField("blocker2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (blocker2Field != null) blocker2Field.SetValue(wUnlocker, divL2);

            var blocker3Field = typeof(WarehouseUpgradeUnlocker).GetField("blocker3", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (blocker3Field != null) blocker3Field.SetValue(wUnlocker, divL3);

            // Corridor wall: X = 10, Z from 6 to 12
            // Z = 7: Wall, Z = 9: Doorway (warehouse door), Z = 11: Wall
            SpawnPrefab(wallPrefab, new Vector3(10f, -1.35f, 7f), Quaternion.Euler(0f, 90f, 0f), wallsGo.transform);
            
            GameObject warehouseDoor = SpawnPrefab(doorwayPrefab, new Vector3(10f, -1.35f, 9f), Quaternion.Euler(0f, 90f, 0f), wallsGo.transform);
            if (warehouseDoor != null)
            {
                var door = warehouseDoor.GetComponent<InteractiveDoor>();
                if (door != null)
                {
                    typeof(InteractiveDoor).GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, "Дверь на склад");
                    typeof(InteractiveDoor).GetField("openAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, 90f);
                }
            }

            SpawnPrefab(wallPrefab, new Vector3(10f, -1.35f, 11f), Quaternion.Euler(0f, 90f, 0f), wallsGo.transform);


            // 6. Spawn Interior Decor (Vents, Lights, Plants, Art)
            // Ceiling is at Y = 2.15f
            float ceilingY = 2.1f;

            // 6.1 Ventilation Pipes (Vents) running along the ceiling
            // Pipe line from X = -10 to 10 at Z = 0
            for (float x = -10f; x <= 10f; x += 3f)
            {
                SpawnPrefab(ventPrefab, new Vector3(x, ceilingY, 0f), Quaternion.Euler(0f, 90f, 0f), decorGo.transform);
            }
            // Pipe line from X = -10 to 10 at Z = -6
            for (float x = -10f; x <= 10f; x += 3f)
            {
                SpawnPrefab(ventPrefab, new Vector3(x, ceilingY, -6f), Quaternion.Euler(0f, 90f, 0f), decorGo.transform);
            }

            // 6.2 Hanging Loft Lamps (Warm Spotlights)
            // Lamp above DisplayStand (approx position X=11.6, Z=1.0)
            SpawnHangingLamp(lampPrefab, new Vector3(10.5f, ceilingY, 1.0f), new Color(1f, 0.75f, 0.4f), 1.8f, decorGo.transform);
            // Lamp above ServiceCounter (approx position X=5.3, Z=4.2)
            SpawnHangingLamp(lampPrefab, new Vector3(5.3f, ceilingY, 3.5f), new Color(1f, 0.75f, 0.4f), 1.8f, decorGo.transform);
            // Lamp in the middle of shop floor
            SpawnHangingLamp(lampPrefab, new Vector3(-5.0f, ceilingY, -3.0f), new Color(1f, 0.75f, 0.4f), 1.8f, decorGo.transform);
            SpawnHangingLamp(lampPrefab, new Vector3(5.0f, ceilingY, -3.0f), new Color(1f, 0.75f, 0.4f), 1.8f, decorGo.transform);

            // 7. Configure Player starting position and LeftHallUnlocker manager
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo != null && playerGo.transform.position.x < 0f)
            {
                playerGo.transform.position = new Vector3(3.0f, playerGo.transform.position.y, playerGo.transform.position.z);
                Debug.Log("Adjusted Player start position to X = 3.0f (inside starting shop room).");
            }

            GameObject unlockerGo = GameObject.Find("LeftHallUnlockerManager");
            if (unlockerGo == null)
            {
                unlockerGo = new GameObject("LeftHallUnlockerManager");
            }
            var unlocker = unlockerGo.GetComponent<LeftHallUnlocker>();
            if (unlocker == null)
            {
                unlocker = unlockerGo.AddComponent<LeftHallUnlocker>();
            }

            var blockerFields = typeof(LeftHallUnlocker).GetField("blockerObjects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (blockerFields != null)
            {
                blockerFields.SetValue(unlocker, new GameObject[] { dividerParent });
            }

            // 8. Adjust positions of other objects in the scene to resolve layout issues
            GameObject trashBin = GameObject.Find("TrashBin");
            if (trashBin != null)
            {
                trashBin.transform.position = new Vector3(13.5f, -1.30f, 9.5f);
                trashBin.transform.rotation = Quaternion.identity;
                var visual = trashBin.transform.Find("DumpsterVisual");
                if (visual != null)
                {
                    visual.localPosition = Vector3.zero;
                    visual.localRotation = Quaternion.identity;
                }
                Debug.Log("[WallRebuilder] Moved TrashBin to delivery zone and centered DumpsterVisual.");
            }

            GameObject rack1 = GameObject.Find("BroomToolRack");
            if (rack1 != null)
            {
                rack1.transform.position = new Vector3(11.62f, -1.30f, 6.8f);
                Debug.Log("[WallRebuilder] Moved BroomToolRack 1 to (11.62, -1.3, 6.8).");
            }
            GameObject rack2 = GameObject.Find("BroomToolRack (1)");
            if (rack2 != null)
            {
                rack2.transform.position = new Vector3(11.62f, -1.30f, 7.4f);
                Debug.Log("[WallRebuilder] Moved BroomToolRack 2 to (11.62, -1.3, 7.4).");
            }

            // Fix Warehouse Shelf Prefab material
            FixWarehouseShelfPrefab(plasticMat);

            // Save Scene
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("Scene modular walls and interior rebuild finished successfully!");
        }

        private static GameObject SpawnPrefab(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            GameObject inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (inst != null)
            {
                inst.transform.position = pos;
                inst.transform.rotation = rot;
                inst.transform.SetParent(parent);
            }
            return inst;
        }

        private static void SpawnHangingLamp(GameObject lampPrefab, Vector3 pos, Color lightColor, float intensity, Transform parent)
        {
            GameObject lamp = SpawnPrefab(lampPrefab, pos, Quaternion.identity, parent);
            if (lamp != null)
            {
                GameObject lightGo = new GameObject("WarmSpotlight");
                lightGo.transform.SetParent(lamp.transform);
                lightGo.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Face down

                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = lightColor;
                light.intensity = intensity;
                light.range = 7f;
                light.spotAngle = 70f;
                light.innerSpotAngle = 30f;
                light.shadows = LightShadows.Soft;
            }
        }

        private static void SpawnEntranceDoubleDoor(GameObject doorwayPrefab, Vector3 pos, Quaternion rot, Transform parent, string doorName, bool requiresUpgrade)
        {
            // The entrance door is a 2.0m wide double swinging door
            // It has two doorways flipped or placed together
            // We can place the doorway segment, but make it a double glass door by customizing it in the scene!
            GameObject doubleDoor = SpawnPrefab(doorwayPrefab, pos, rot, parent);
            doubleDoor.name = doorName;

            // Unpack prefab completely to modify it safely in the scene and prevent revert bugs
            PrefabUtility.UnpackPrefabInstance(doubleDoor, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            Material chromeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/ChromeMaterial.mat");

            // Modify the door inside EntranceDoubleDoor to be glass doors!
            // Double door has 2 panels swinging left and right
            Transform hingeLeft = doubleDoor.transform.Find("Hinge");
            if (hingeLeft != null)
            {
                hingeLeft.name = "HingeLeft";
                var door = doubleDoor.GetComponent<InteractiveDoor>();
                if (door == null) door = doubleDoor.AddComponent<InteractiveDoor>();
                
                var type = door.GetType();
                // Assign left hinge reference explicitly to prevent it searching for "Hinge" and failing
                type.GetField("hinge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, hingeLeft);
                // Configure left door: swings -90 degrees
                type.GetField("openAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, -95f);
                type.GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, requiresUpgrade ? "Входная дверь (левая)" : "Парадная дверь (левая)");
                type.GetField("requiresUpgrade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(door, requiresUpgrade);
                
                // Change panel material to GlassMaterial and resize for double door!
                Transform panel = hingeLeft.Find("Panel");
                if (panel != null)
                {
                    panel.localScale = new Vector3(0.675f, 2.5f, 0.05f);
                    panel.localPosition = new Vector3(0.3375f, 1.25f, 0f);

                    Material glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/GlassMaterial.mat");
                    if (glassMat != null)
                    {
                        panel.GetComponent<MeshRenderer>().sharedMaterial = glassMat;
                    }
                }
            }

            // Create Right Hinge and Panel for the double door
            GameObject hingeRightGo = new GameObject("HingeRight");
            hingeRightGo.transform.SetParent(doubleDoor.transform);
            // HingeRight is placed at X = +0.7
            hingeRightGo.transform.localPosition = new Vector3(0.675f, 0f, 0f);
            hingeRightGo.transform.localRotation = Quaternion.identity;
            hingeRightGo.transform.localScale = Vector3.one;

            GameObject rightPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPanel.name = "Panel";
            rightPanel.transform.SetParent(hingeRightGo.transform);
            // Pivot is at right edge, panel extends left towards center (offset local X is -0.3375, wait, panel width is 0.675, so center is at -0.3375)
            rightPanel.transform.localPosition = new Vector3(-0.3375f, 1.25f, 0f);
            rightPanel.transform.localRotation = Quaternion.identity;
            rightPanel.transform.localScale = new Vector3(0.675f, 2.5f, 0.05f);

            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WoodSolidMaterial.mat");
            Material glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/GlassMaterial.mat");
            rightPanel.GetComponent<MeshRenderer>().sharedMaterial = glass != null ? glass : woodMat;

            // Add handle to the right door panel
            GameObject hBaseRight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hBaseRight.name = "HandleBase";
            hBaseRight.transform.SetParent(rightPanel.transform);
            hBaseRight.transform.localPosition = new Vector3(-0.4f, 0f, 0.6f);
            hBaseRight.transform.localScale = new Vector3(0.05f, 0.03f, 0.05f);
            hBaseRight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hBaseRight.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { chromeMat };

            GameObject hLeverRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hLeverRight.name = "HandleLever";
            hLeverRight.transform.SetParent(hBaseRight.transform);
            hLeverRight.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            hLeverRight.transform.localScale = new Vector3(0.4f, 1.5f, 0.2f);
            hLeverRight.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hLeverRight.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { chromeMat };

            Object.DestroyImmediate(hBaseRight.GetComponent<Collider>());
            Object.DestroyImmediate(hLeverRight.GetComponent<Collider>());

            // Add interactive door script for the right door!
            var rightDoorScript = doubleDoor.AddComponent<InteractiveDoor>();
            var rType = rightDoorScript.GetType();
            rType.GetField("hinge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(rightDoorScript, hingeRightGo.transform);
            rType.GetField("openAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(rightDoorScript, 95f); // swings opposite way
            rType.GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(rightDoorScript, requiresUpgrade ? "Входная дверь (правая)" : "Парадная дверь (правая)");
            rType.GetField("requiresUpgrade", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(rightDoorScript, requiresUpgrade);

            // Resize the NavMeshObstacle to cover the 2.0m wide double door doorway
            var doubleObstacle = doubleDoor.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (doubleObstacle != null)
            {
                doubleObstacle.size = new Vector3(2.0f, 2.5f, 0.1f);
                doubleObstacle.center = new Vector3(0f, 1.25f, 0f);
            }
        }

        private static void SetMaterialsRecursively(GameObject go, Material mat)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
            foreach (Transform c in go.transform)
            {
                SetMaterialsRecursively(c.gameObject, mat);
            }
        }

        // --- PREFAB GENERATION HELPER METHODS ---

        private static GameObject CreateWallSegmentPrefab(string folderPath, Material wallMat, Material woodMat)
        {
            string path = folderPath + "/Wall_Segment_2m.prefab";
            GameObject go = new GameObject("Wall_Segment_2m");

            // Main Wall Panel
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(go.transform);
            panel.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            panel.transform.localScale = new Vector3(2.0f, 3.5f, 0.3f);
            panel.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Baseboard Trim (Mahogany wood at bottom)
            GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseboard.name = "Baseboard";
            baseboard.transform.SetParent(go.transform);
            baseboard.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            baseboard.transform.localScale = new Vector3(2.0f, 0.15f, 0.32f);
            baseboard.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Crown Molding (Mahogany wood at top)
            GameObject molding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            molding.name = "CrownMolding";
            molding.transform.SetParent(go.transform);
            molding.transform.localPosition = new Vector3(0f, 3.425f, 0f);
            molding.transform.localScale = new Vector3(2.0f, 0.15f, 0.32f);
            molding.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreatePillarPrefab(string folderPath, Material wallMat, Material woodMat)
        {
            string path = folderPath + "/Pillar_Segment.prefab";
            GameObject go = new GameObject("Pillar_Segment");

            // Pillar body
            GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cube);
            col.name = "Column";
            col.transform.SetParent(go.transform);
            col.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            col.transform.localScale = new Vector3(0.4f, 3.5f, 0.4f);
            col.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Pillar Base
            GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseboard.name = "Base";
            baseboard.transform.SetParent(go.transform);
            baseboard.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            baseboard.transform.localScale = new Vector3(0.44f, 0.2f, 0.44f);
            baseboard.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Pillar Capital
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Capital";
            cap.transform.SetParent(go.transform);
            cap.transform.localPosition = new Vector3(0f, 3.4f, 0f);
            cap.transform.localScale = new Vector3(0.44f, 0.2f, 0.44f);
            cap.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateWindowPrefab(string folderPath, Material wallMat, Material woodMat, Material glassMat)
        {
            string path = folderPath + "/Window_Segment.prefab";
            GameObject go = new GameObject("Window_Segment");

            // Left side Wall (0.3m)
            GameObject wl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wl.name = "WallLeft";
            wl.transform.SetParent(go.transform);
            wl.transform.localPosition = new Vector3(-0.85f, 1.75f, 0f);
            wl.transform.localScale = new Vector3(0.3f, 3.5f, 0.3f);
            wl.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Right side Wall (0.3m)
            GameObject wr = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wr.name = "WallRight";
            wr.transform.SetParent(go.transform);
            wr.transform.localPosition = new Vector3(0.85f, 1.75f, 0f);
            wr.transform.localScale = new Vector3(0.3f, 3.5f, 0.3f);
            wr.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Bottom wall panel (drywall under window)
            GameObject wb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wb.name = "WallBottom";
            wb.transform.SetParent(go.transform);
            wb.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            wb.transform.localScale = new Vector3(1.4f, 1.0f, 0.3f);
            wb.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Top wall panel (drywall above window)
            GameObject wt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wt.name = "WallTop";
            wt.transform.SetParent(go.transform);
            wt.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            wt.transform.localScale = new Vector3(1.4f, 1.0f, 0.3f);
            wt.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Window Frame left
            GameObject fl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fl.name = "FrameLeft";
            fl.transform.SetParent(go.transform);
            fl.transform.localPosition = new Vector3(-0.7f, 2.0f, 0f);
            fl.transform.localScale = new Vector3(0.05f, 2.0f, 0.32f);
            fl.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Window Frame right
            GameObject fr = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fr.name = "FrameRight";
            fr.transform.SetParent(go.transform);
            fr.transform.localPosition = new Vector3(0.7f, 2.0f, 0f);
            fr.transform.localScale = new Vector3(0.05f, 2.0f, 0.32f);
            fr.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Window Frame bottom
            GameObject fb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fb.name = "FrameBottom";
            fb.transform.SetParent(go.transform);
            fb.transform.localPosition = new Vector3(0f, 1.025f, 0f);
            fb.transform.localScale = new Vector3(1.45f, 0.05f, 0.32f);
            fb.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Window Frame top
            GameObject ft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ft.name = "FrameTop";
            ft.transform.SetParent(go.transform);
            ft.transform.localPosition = new Vector3(0f, 2.975f, 0f);
            ft.transform.localScale = new Vector3(1.45f, 0.05f, 0.32f);
            ft.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Glass Panel
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Glass";
            glass.transform.SetParent(go.transform);
            glass.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            glass.transform.localScale = new Vector3(1.35f, 1.9f, 0.05f);
            glass.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { glassMat };
            
            // Remove collider from glass so player interacts with frame or wall colliders if needed (or keep it as trigger/glass collider)
            var glassCol = glass.GetComponent<BoxCollider>();
            if (glassCol != null) glassCol.isTrigger = false;

            // Baseboard segments
            GameObject bbl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bbl.name = "BaseboardLeft";
            bbl.transform.SetParent(go.transform);
            bbl.transform.localPosition = new Vector3(-0.85f, 0.075f, 0f);
            bbl.transform.localScale = new Vector3(0.3f, 0.15f, 0.32f);
            bbl.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            GameObject bbr = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bbr.name = "BaseboardRight";
            bbr.transform.SetParent(go.transform);
            bbr.transform.localPosition = new Vector3(0.85f, 0.075f, 0f);
            bbr.transform.localScale = new Vector3(0.3f, 0.15f, 0.32f);
            bbr.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            GameObject bbc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bbc.name = "BaseboardCenter";
            bbc.transform.SetParent(go.transform);
            bbc.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            bbc.transform.localScale = new Vector3(1.4f, 0.15f, 0.32f);
            bbc.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Crown Molding
            GameObject molding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            molding.name = "CrownMolding";
            molding.transform.SetParent(go.transform);
            molding.transform.localPosition = new Vector3(0f, 3.425f, 0f);
            molding.transform.localScale = new Vector3(2.0f, 0.15f, 0.32f);
            molding.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateDoorwayPrefab(string folderPath, Material wallMat, Material woodMat, Material chromeMat)
        {
            string path = folderPath + "/Doorway_Segment.prefab";
            GameObject go = new GameObject("Doorway_Segment");

            // Left side Wall (0.3m)
            GameObject wl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wl.name = "WallLeft";
            wl.transform.SetParent(go.transform);
            wl.transform.localPosition = new Vector3(-0.85f, 1.75f, 0f);
            wl.transform.localScale = new Vector3(0.3f, 3.5f, 0.3f);
            wl.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Right side Wall (0.3m)
            GameObject wr = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wr.name = "WallRight";
            wr.transform.SetParent(go.transform);
            wr.transform.localPosition = new Vector3(0.85f, 1.75f, 0f);
            wr.transform.localScale = new Vector3(0.3f, 3.5f, 0.3f);
            wr.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Top wall panel above doorway frame
            GameObject wt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wt.name = "WallTop";
            wt.transform.SetParent(go.transform);
            wt.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            wt.transform.localScale = new Vector3(1.4f, 1.0f, 0.3f);
            wt.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { wallMat };

            // Door Frame left
            GameObject jl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jl.name = "JambLeft";
            jl.transform.SetParent(go.transform);
            jl.transform.localPosition = new Vector3(-0.7f, 1.25f, 0f);
            jl.transform.localScale = new Vector3(0.05f, 2.5f, 0.32f);
            jl.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Door Frame right
            GameObject jr = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jr.name = "JambRight";
            jr.transform.SetParent(go.transform);
            jr.transform.localPosition = new Vector3(0.7f, 1.25f, 0f);
            jr.transform.localScale = new Vector3(0.05f, 2.5f, 0.32f);
            jr.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Door Frame top
            GameObject jt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jt.name = "JambTop";
            jt.transform.SetParent(go.transform);
            jt.transform.localPosition = new Vector3(0f, 2.525f, 0f);
            jt.transform.localScale = new Vector3(1.45f, 0.05f, 0.32f);
            jt.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Baseboards
            GameObject bbl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bbl.name = "BaseboardLeft";
            bbl.transform.SetParent(go.transform);
            bbl.transform.localPosition = new Vector3(-0.85f, 0.075f, 0f);
            bbl.transform.localScale = new Vector3(0.3f, 0.15f, 0.32f);
            bbl.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            GameObject bbr = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bbr.name = "BaseboardRight";
            bbr.transform.SetParent(go.transform);
            bbr.transform.localPosition = new Vector3(0.85f, 0.075f, 0f);
            bbr.transform.localScale = new Vector3(0.3f, 0.15f, 0.32f);
            bbr.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Crown Molding
            GameObject molding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            molding.name = "CrownMolding";
            molding.transform.SetParent(go.transform);
            molding.transform.localPosition = new Vector3(0f, 3.425f, 0f);
            molding.transform.localScale = new Vector3(2.0f, 0.15f, 0.32f);
            molding.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Door Assembly: Hinge & Panel
            GameObject hingeGo = new GameObject("Hinge");
            hingeGo.transform.SetParent(go.transform);
            // Position of Hinge is at the left jamb edge (X = -0.675)
            hingeGo.transform.localPosition = new Vector3(-0.675f, 0f, 0f);
            hingeGo.transform.localRotation = Quaternion.identity;
            hingeGo.transform.localScale = Vector3.one;

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(hingeGo.transform);
            // Center of the panel is offset right relative to hinge by half-width (0.675)
            panel.transform.localPosition = new Vector3(0.675f, 1.25f, 0f);
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = new Vector3(1.35f, 2.5f, 0.05f);
            panel.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMat };

            // Add Door Handle
            GameObject hBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hBase.name = "HandleBase";
            hBase.transform.SetParent(panel.transform);
            hBase.transform.localPosition = new Vector3(0.4f, 0f, 0.6f); // relative to Panel scale (X=0.4 is near edge, Z=0.6 is slightly offset from surface)
            // Scale and rotate handle base
            hBase.transform.localScale = new Vector3(0.05f, 0.03f, 0.05f);
            hBase.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hBase.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { chromeMat };
            
            // Add Lever
            GameObject hLever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hLever.name = "HandleLever";
            hLever.transform.SetParent(hBase.transform);
            hLever.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            hLever.transform.localScale = new Vector3(0.4f, 1.5f, 0.2f);
            hLever.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hLever.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { chromeMat };

            // Remove colliders from handle parts
            Object.DestroyImmediate(hBase.GetComponent<Collider>());
            Object.DestroyImmediate(hLever.GetComponent<Collider>());

            // Add InteractiveDoor component on root
            var doorScript = go.AddComponent<InteractiveDoor>();
            // Use reflection to assign internal Hinge transform
            var hingeField = typeof(InteractiveDoor).GetField("hinge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (hingeField != null)
            {
                hingeField.SetValue(doorScript, hingeGo.transform);
            }

            // Setup a BoxCollider on the root doorway for general physics (covering side walls)
            // Note: the door panel itself has a BoxCollider which rotates with it, blocking physics when closed and letting through when open!
            // We just need to make sure the door panel has a standard collider.
            // When panel is instantiated via CreatePrimitive, it has a BoxCollider by default.

            // Add NavMeshObstacle to dynamically block/unblock pathfinding on root
            var obstacle = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = new Vector3(1.35f, 2.5f, 0.1f);
            obstacle.center = new Vector3(0f, 1.25f, 0f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateVentPrefab(string folderPath, Material metalMat)
        {
            string path = folderPath + "/Ceiling_Vent.prefab";
            GameObject go = new GameObject("Ceiling_Vent");

            // Main Vent Pipe
            GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "Pipe";
            pipe.transform.SetParent(go.transform);
            pipe.transform.localPosition = Vector3.zero;
            pipe.transform.localScale = new Vector3(0.2f, 1.5f, 0.2f);
            pipe.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Rotate along Z/X
            pipe.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { metalMat };
            Object.DestroyImmediate(pipe.GetComponent<Collider>());

            // Joint ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(go.transform);
            ring.transform.localPosition = new Vector3(0f, 0f, 0.7f);
            ring.transform.localScale = new Vector3(0.22f, 0.05f, 0.22f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { metalMat };
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateLampPrefab(string folderPath, Material plasticMat, Material chromeMat)
        {
            string path = folderPath + "/Hanging_Lamp.prefab";
            GameObject go = new GameObject("Hanging_Lamp");

            // Thin hanging wire (rod)
            GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wire.name = "Wire";
            wire.transform.SetParent(go.transform);
            wire.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            wire.transform.localScale = new Vector3(0.015f, 0.2f, 0.015f);
            wire.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { plasticMat };
            Object.DestroyImmediate(wire.GetComponent<Collider>());

            // Metal lamp shade
            GameObject shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shade.name = "Shade";
            shade.transform.SetParent(go.transform);
            shade.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            shade.transform.localScale = new Vector3(0.18f, 0.04f, 0.18f);
            shade.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { chromeMat };
            Object.DestroyImmediate(shade.GetComponent<Collider>());

            // Small glowing bulb inside
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(go.transform);
            bulb.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            bulb.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            
            Material bulbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WarmBulbMaterial.mat");
            if (bulbMat != null)
            {
                bulb.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { bulbMat };
            }
            Object.DestroyImmediate(bulb.GetComponent<Collider>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void FixWarehouseShelfPrefab(Material defaultMat)
        {
            string path = "Assets/Prefabs/WarehouseShelf.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("WarehouseShelf prefab not found at " + path);
                return;
            }

            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            bool anyChanged = false;
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null)
                    {
                        mats[i] = defaultMat;
                        changed = true;
                    }
                }
                if (changed)
                {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("WarehouseShelf prefab materials fixed successfully with " + defaultMat.name);
            }
        }
    }
}
