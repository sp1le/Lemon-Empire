using UnityEngine;

namespace LemonEmpire.Player
{
    /// <summary>
    /// Диагностика проблем с отдельной камерой для рук
    /// </summary>
    public class ArmCameraDebugger : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("=== ARM CAMERA DIAGNOSTIC ===");

            // Проверяем Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("Main Camera не найдена!");
                return;
            }

            Debug.Log($"Main Camera: {mainCam.name}");
            Debug.Log($"  Culling Mask: {LayerMaskToString(mainCam.cullingMask)}");
            Debug.Log($"  Near Clip: {mainCam.nearClipPlane}");
            Debug.Log($"  Far Clip: {mainCam.farClipPlane}");
            Debug.Log($"  Depth: {mainCam.depth}");

            // Проверяем ArmCamera
            Camera[] allCameras = FindObjectsOfType<Camera>();
            Camera armCam = null;

            foreach (var cam in allCameras)
            {
                if (cam.name.Contains("Arm"))
                {
                    armCam = cam;
                    break;
                }
            }

            if (armCam == null)
            {
                Debug.LogError("ArmCamera не найдена! Проверьте что ArmLayerCameraSetup создал её.");
            }
            else
            {
                Debug.Log($"\nArm Camera: {armCam.name}");
                Debug.Log($"  Culling Mask: {LayerMaskToString(armCam.cullingMask)}");
                Debug.Log($"  Near Clip: {armCam.nearClipPlane}");
                Debug.Log($"  Far Clip: {armCam.farClipPlane}");
                Debug.Log($"  Depth: {armCam.depth}");
                Debug.Log($"  Clear Flags: {armCam.clearFlags}");
            }

            // Проверяем слой PlayerArms
            int armLayer = LayerMask.NameToLayer("PlayerArms");
            if (armLayer == -1)
            {
                Debug.LogError("Слой 'PlayerArms' не существует! Создайте его в Project Settings → Tags and Layers");
            }
            else
            {
                Debug.Log($"\nPlayerArms Layer: {armLayer}");
            }

            // Проверяем меши рук
            var allRenderers = FindObjectsOfType<Renderer>();
            int armCount = 0;

            Debug.Log("\nМеши на слое PlayerArms:");
            foreach (var renderer in allRenderers)
            {
                if (renderer.gameObject.layer == armLayer)
                {
                    Debug.Log($"  - {renderer.name} (Layer: {armLayer})");
                    armCount++;
                }
            }

            if (armCount == 0)
            {
                Debug.LogWarning("НЕ НАЙДЕНО мешей на слое PlayerArms! Руки не будут рендериться отдельной камерой.");
            }

            Debug.Log("\n=== END DIAGNOSTIC ===");
        }

        private string LayerMaskToString(int mask)
        {
            string result = "";
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        result += layerName + ", ";
                    }
                }
            }
            return result.TrimEnd(',', ' ');
        }
    }
}
