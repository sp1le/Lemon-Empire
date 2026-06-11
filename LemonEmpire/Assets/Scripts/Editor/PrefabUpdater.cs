using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LemonEmpire.Editor
{
    public class PrefabUpdater
    {
        [MenuItem("Lemon Empire/Update Prefabs")]
        public static void UpdateAllPrefabs()
        {
            Debug.Log("Starting Prefab update...");

            // 1. Load models and materials
            string lemonBoxPath = "Assets/Models/Props/LemonBox/LemonBox.fbx";
            GameObject lemonBoxFbx = AssetDatabase.LoadAssetAtPath<GameObject>(lemonBoxPath);
            if (lemonBoxFbx == null)
            {
                Debug.LogError("Failed to load LemonBox.fbx");
                return;
            }

            Mesh openBoxMesh = null;
            foreach (Transform child in lemonBoxFbx.transform)
            {
                if (child.name == "Cube")
                {
                    openBoxMesh = child.GetComponent<MeshFilter>().sharedMesh;
                    break;
                }
            }

            if (openBoxMesh == null)
            {
                Debug.LogError("Failed to find Cube mesh inside LemonBox.fbx");
                return;
            }

            Material woodMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Props/LemonBox/Wood_Color.001.mat");
            if (woodMaterial == null)
            {
                Debug.LogError("Failed to load Wood_Color.001.mat");
                return;
            }

            string sodaBottlePath = "Assets/Models/FoodKit/soda-bottle.fbx";
            GameObject sodaBottleFbx = AssetDatabase.LoadAssetAtPath<GameObject>(sodaBottlePath);
            if (sodaBottleFbx == null)
            {
                Debug.LogError("Failed to load soda-bottle.fbx");
                return;
            }

            Mesh bottleMesh = sodaBottleFbx.GetComponent<MeshFilter>().sharedMesh;
            if (bottleMesh == null)
            {
                Debug.LogError("Failed to find mesh inside soda-bottle.fbx");
                return;
            }

            Material bottleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/FoodKit/FoodKitMaterial.mat");
            if (bottleMaterial == null)
            {
                Debug.LogError("Failed to load FoodKitMaterial.mat");
                return;
            }

            // --- UPDATE EmptyCrate.prefab ---
            string emptyCratePath = "Assets/Resources/EmptyCrate.prefab";
            GameObject emptyCrateGo = PrefabUtility.LoadPrefabContents(emptyCratePath);
            if (emptyCrateGo != null)
            {
                Transform cube = emptyCrateGo.transform.Find("Cube");
                if (cube != null)
                {
                    cube.GetComponent<MeshFilter>().sharedMesh = openBoxMesh;
                    cube.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMaterial };
                    cube.transform.localPosition = new Vector3(0f, -0.06f, 0f);
                    cube.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Fixed rotated-by-90-degrees-sideways/upside-down issue
                    cube.transform.localScale = new Vector3(100f, 100f, 100f);
                }

                BoxCollider col = emptyCrateGo.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.center = new Vector3(0f, 0.16f, 0f);
                    col.size = new Vector3(0.8f, 0.32f, 0.8f);
                }

                // Add the 12 bottle children as placeholder slots initialized to inactive
                Transform bottlesContainer = emptyCrateGo.transform.Find("Bottles");
                if (bottlesContainer != null)
                {
                    for (int i = bottlesContainer.childCount - 1; i >= 0; i--)
                    {
                        Object.DestroyImmediate(bottlesContainer.GetChild(i).gameObject);
                    }
                }
                else
                {
                    GameObject newContainer = new GameObject("Bottles");
                    newContainer.transform.SetParent(emptyCrateGo.transform);
                    newContainer.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                    newContainer.transform.localRotation = Quaternion.identity;
                    newContainer.transform.localScale = Vector3.one;
                    bottlesContainer = newContainer.transform;
                }

                float[] xPositions = new float[] { -0.19f, 0f, 0.19f };
                float[] zPositions = new float[] { -0.22f, -0.075f, 0.075f, 0.22f };
                int bottleIdx = 1;
                for (int x = 0; x < 3; x++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        GameObject slotGo = new GameObject("Bottle" + bottleIdx);
                        slotGo.transform.SetParent(bottlesContainer);
                        slotGo.transform.localPosition = new Vector3(xPositions[x], 0.065f, zPositions[z]);
                        slotGo.transform.localRotation = Quaternion.identity;
                        slotGo.transform.localScale = Vector3.one;

                        GameObject visualGo = new GameObject("Bottle");
                        visualGo.transform.SetParent(slotGo.transform);
                        visualGo.transform.localPosition = Vector3.zero;
                        visualGo.transform.localRotation = Quaternion.identity;
                        visualGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                        var mf = visualGo.AddComponent<MeshFilter>();
                        mf.sharedMesh = bottleMesh;
                        var mr = visualGo.AddComponent<MeshRenderer>();
                        mr.sharedMaterials = new Material[] { bottleMaterial };

                        slotGo.SetActive(false); // Inactive for empty crate
                        bottleIdx++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(emptyCrateGo, emptyCratePath);
                PrefabUtility.UnloadPrefabContents(emptyCrateGo);
                Debug.Log("Successfully updated EmptyCrate.prefab");
            }

            // --- UPDATE LemonadeBottle.prefab (both Prefabs and Resources) ---
            string[] bottlePaths = new string[]
            {
                "Assets/Prefabs/LemonadeBottle.prefab",
                "Assets/Resources/LemonadeBottle.prefab"
            };
            foreach (string bPath in bottlePaths)
            {
                GameObject bottleGo = PrefabUtility.LoadPrefabContents(bPath);
                if (bottleGo != null)
                {
                    Transform liquid = bottleGo.transform.Find("Liquid");
                    if (liquid != null)
                    {
                        Object.DestroyImmediate(liquid.gameObject);
                    }

                    Transform bottle = bottleGo.transform.Find("Bottle");
                    if (bottle != null)
                    {
                        bottle.GetComponent<MeshFilter>().sharedMesh = bottleMesh;
                        bottle.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { bottleMaterial }; // Fixed 3 material slots mismatch
                        bottle.transform.localPosition = Vector3.zero;
                        bottle.transform.localRotation = Quaternion.identity;
                        bottle.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    }

                    BoxCollider col = bottleGo.GetComponent<BoxCollider>();
                    if (col != null)
                    {
                        col.center = new Vector3(0f, 0.145f, 0f);
                        col.size = new Vector3(0.12f, 0.29f, 0.12f);
                    }

                    PrefabUtility.SaveAsPrefabAsset(bottleGo, bPath);
                    PrefabUtility.UnloadPrefabContents(bottleGo);
                    Debug.Log("Successfully updated: " + bPath);
                }
            }

            // --- UPDATE Crate Prefabs (BottledLemonade and BottlePack) ---
            string[] cratePaths = new string[]
            {
                "Assets/Prefabs/BottledLemonade.prefab",
                "Assets/Prefabs/BottlePack.prefab"
            };
            foreach (string cPath in cratePaths)
            {
                GameObject crateGo = PrefabUtility.LoadPrefabContents(cPath);
                if (crateGo != null)
                {
                    Transform cube = crateGo.transform.Find("Cube");
                    if (cube != null)
                    {
                        cube.GetComponent<MeshFilter>().sharedMesh = openBoxMesh;
                        cube.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { woodMaterial };
                        cube.transform.localPosition = new Vector3(0f, -0.06f, 0f);
                        cube.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Fixed rotation
                        cube.transform.localScale = new Vector3(100f, 100f, 100f);
                    }

                    BoxCollider col = crateGo.GetComponent<BoxCollider>();
                    if (col != null)
                    {
                        col.center = new Vector3(0f, 0.16f, 0f);
                        col.size = new Vector3(0.8f, 0.32f, 0.8f);
                    }

                    Transform bottlesContainer = crateGo.transform.Find("Bottles");
                    if (bottlesContainer != null)
                    {
                        for (int i = bottlesContainer.childCount - 1; i >= 0; i--)
                        {
                            Object.DestroyImmediate(bottlesContainer.GetChild(i).gameObject);
                        }
                    }
                    else
                    {
                        GameObject newContainer = new GameObject("Bottles");
                        newContainer.transform.SetParent(crateGo.transform);
                        newContainer.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                        newContainer.transform.localRotation = Quaternion.identity;
                        newContainer.transform.localScale = Vector3.one;
                        bottlesContainer = newContainer.transform;
                    }

                    float[] xPositions = new float[] { -0.19f, 0f, 0.19f };
                    float[] zPositions = new float[] { -0.22f, -0.075f, 0.075f, 0.22f };
                    int bottleIdx = 1;
                    for (int x = 0; x < 3; x++)
                    {
                        for (int z = 0; z < 4; z++)
                        {
                            GameObject slotGo = new GameObject("Bottle" + bottleIdx);
                            slotGo.transform.SetParent(bottlesContainer);
                            slotGo.transform.localPosition = new Vector3(xPositions[x], 0.065f, zPositions[z]);
                            slotGo.transform.localRotation = Quaternion.identity;
                            slotGo.transform.localScale = Vector3.one;

                            GameObject visualGo = new GameObject("Bottle");
                            visualGo.transform.SetParent(slotGo.transform);
                            visualGo.transform.localPosition = Vector3.zero;
                            visualGo.transform.localRotation = Quaternion.identity;
                            visualGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                            var mf = visualGo.AddComponent<MeshFilter>();
                            mf.sharedMesh = bottleMesh;
                            var mr = visualGo.AddComponent<MeshRenderer>();
                            mr.sharedMaterials = new Material[] { bottleMaterial };

                            bottleIdx++;
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(crateGo, cPath);
                    PrefabUtility.UnloadPrefabContents(crateGo);
                    Debug.Log("Successfully updated: " + cPath);
                }
            }

            // --- UPDATE TradeStand.bottlePrefab reference in the active scene ---
            var scene = EditorSceneManager.GetActiveScene();
            var stands = GameObject.FindObjectsOfType<LemonEmpire.Trading.TradeStand>();
            if (stands.Length > 0)
            {
                GameObject bottlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LemonadeBottle.prefab");
                if (bottlePrefab != null)
                {
                    foreach (var stand in stands)
                    {
                        var type = stand.GetType();
                        var field = type.GetField("bottlePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(stand, bottlePrefab);
                            EditorUtility.SetDirty(stand);
                            Debug.Log("Assigned bottlePrefab to TradeStand: " + stand.name);
                        }
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            Debug.Log("Prefab update completed successfully!");
        }
    }
}
