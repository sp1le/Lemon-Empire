using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class EmptyCrate : ItemBase
    {
        public static EmptyCrate SpawnEmptyCrate(Vector3 position, string name = "Пустой ящик", ItemType type = ItemType.EquipmentBox)
        {
            var prefab = Resources.Load<GameObject>("EmptyCrate");
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab);
                go.name = name;
                go.transform.position = position;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.position = position;
                go.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);

                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.6f, 0.45f, 0.3f); // Cardboard brown
                    renderer.material = mat;
                }

                var rb = go.GetComponent<Rigidbody>();
                if (rb == null) rb = go.AddComponent<Rigidbody>();
                rb.mass = 1.5f;
            }

            var emptyCrate = go.GetComponent<EmptyCrate>();
            if (emptyCrate == null)
            {
                emptyCrate = go.AddComponent<EmptyCrate>();
            }
            emptyCrate.Setup(type, name, 100f, 0);

            return emptyCrate;
        }
    }
}
