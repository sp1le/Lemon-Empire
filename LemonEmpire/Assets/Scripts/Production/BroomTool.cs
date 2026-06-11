using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class BroomTool : ItemBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            // Auto check if we are the master broom or if we need to spawn one at startup.
            // If this is instantiated, we don't need to do anything.
        }

        public static BroomTool SpawnBroom(Vector3 position)
        {
            GameObject broomGo = new GameObject("BroomTool");
            broomGo.transform.position = position;

            // Load or create materials
            Material woodMat = null;
            Material chromeMat = null;
            Material plasticMat = null;
            Material bristleMat = null;

#if UNITY_EDITOR
            woodMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/WoodSolidMaterial.mat");
            chromeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/ChromeMaterial.mat");
            plasticMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/BlackPlasticMaterial.mat");
            bristleMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/BroomBristlesMaterial.mat");
#endif

            // Fallbacks if not in editor or assets not loaded
            if (woodMat == null) {
                woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                woodMat.color = new Color(0.35f, 0.23f, 0.12f);
            }
            if (chromeMat == null) {
                chromeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                chromeMat.color = Color.white;
                chromeMat.SetFloat("_Metallic", 1f);
                chromeMat.SetFloat("_Smoothness", 0.9f);
            }
            if (plasticMat == null) {
                plasticMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                plasticMat.color = new Color(0.1f, 0.1f, 0.1f);
            }
            if (bristleMat == null) {
                bristleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }
            bristleMat.color = new Color(0.2f, 0.55f, 0.85f); // Microfiber blue color

            // Helper to create sub-objects
            System.Action<string, PrimitiveType, Vector3, Vector3, Quaternion, Material> createPart = (name, type, localPos, scale, rot, mat) => {
                var part = GameObject.CreatePrimitive(type);
                part.name = name;
                part.transform.SetParent(broomGo.transform, false);
                part.transform.localPosition = localPos;
                part.transform.localScale = scale;
                part.transform.localRotation = rot;
                var col = part.GetComponent<Collider>();
                if (col != null) {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
                var rend = part.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = mat;
            };

            // 1. Handle shaft (Cylinder)
            createPart("Handle", PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0f), new Vector3(0.025f, 0.55f, 0.025f), Quaternion.identity, woodMat);

            // 2. Black rubber grip at the top (Cylinder)
            createPart("TopGrip", PrimitiveType.Cylinder, new Vector3(0f, 1.35f, 0f), new Vector3(0.028f, 0.08f, 0.028f), Quaternion.identity, plasticMat);

            // 3. Chrome Loop Ring at the top of the handle (for hanging)
            createPart("TopRing", PrimitiveType.Cylinder, new Vector3(0f, 1.45f, 0f), new Vector3(0.035f, 0.005f, 0.035f), Quaternion.Euler(90, 0, 0), chromeMat);

            // 4. Chrome Connector sleeve at the bottom of the handle
            createPart("Connector", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f), new Vector3(0.035f, 0.03f, 0.035f), Quaternion.identity, chromeMat);

            // 5. Chrome Hinge Vertical Connector
            createPart("HingeVert", PrimitiveType.Cylinder, new Vector3(0f, 0.13f, 0f), new Vector3(0.02f, 0.02f, 0.02f), Quaternion.identity, chromeMat);

            // 6. Black plastic Hinge Horizontal Joint
            createPart("MopHinge", PrimitiveType.Cylinder, new Vector3(0f, 0.09f, 0f), new Vector3(0.04f, 0.015f, 0.04f), Quaternion.Euler(90f, 0f, 0f), plasticMat);

            // 7. Flat microfiber mop frame (blue base)
            createPart("MopFrame", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), new Vector3(0.35f, 0.02f, 0.1f), Quaternion.identity, bristleMat);

            // 8. Black plastic frame top plate
            createPart("FrameTopPlate", PrimitiveType.Cube, new Vector3(0f, 0.075f, 0f), new Vector3(0.2f, 0.015f, 0.08f), Quaternion.identity, plasticMat);

            // 9. Soft microfiber mop pad underneath
            createPart("MopPad", PrimitiveType.Cube, new Vector3(0f, 0.025f, 0f), new Vector3(0.37f, 0.03f, 0.12f), Quaternion.identity, bristleMat);

            // 10. Left microfiber pad clip
            createPart("MopClipLeft", PrimitiveType.Cube, new Vector3(-0.18f, 0.03f, 0f), new Vector3(0.015f, 0.032f, 0.122f), Quaternion.identity, plasticMat);

            // 11. Right microfiber pad clip
            createPart("MopClipRight", PrimitiveType.Cube, new Vector3(0.18f, 0.03f, 0f), new Vector3(0.015f, 0.032f, 0.122f), Quaternion.identity, plasticMat);

            // Setup single BoxCollider on the parent broom
            var boxCol = broomGo.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.75f, 0f);
            boxCol.size = new Vector3(0.4f, 1.5f, 0.2f);

            var rb = broomGo.AddComponent<Rigidbody>();
            rb.mass = 1.5f;

            var broomTool = broomGo.AddComponent<BroomTool>();
            // Set it up as an item
            broomTool.Setup(ItemType.EquipmentBox, "Швабра", 100f, 1);

            Debug.Log($"[BroomTool] Procedural Premium Broom spawned at {position}");
            return broomTool;
        }
    }
}
