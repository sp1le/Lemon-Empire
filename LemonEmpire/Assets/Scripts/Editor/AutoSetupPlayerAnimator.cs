using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LemonEmpire.Editor
{
    /// <summary>
    /// Автоматический сетап Blend Tree в Animator Controller игрока при загрузке редактора.
    /// Гарантирует плавное смешивание Idle -> Walk -> Run без мертвой позы.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSetupPlayerAnimator
    {
        static AutoSetupPlayerAnimator()
        {
            // Задержка вызова для безопасности загрузки AssetDatabase
            EditorApplication.delayCall += SetupAnimator;
        }

        [MenuItem("Lemon Empire/Force Setup Player Animator")]
        public static void SetupAnimator()
        {
            string controllerPath = "Assets/Models/Character@Idle.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogWarning("[AutoSetupPlayerAnimator] Character@Idle.controller не найден по пути: " + controllerPath);
                return;
            }

            var baseLayer = controller.layers[0];
            var stateMachine = baseLayer.stateMachine;

            AnimatorState movementState = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == "Movement")
                {
                    movementState = childState.state;
                    break;
                }
            }

            if (movementState == null)
            {
                Debug.LogWarning("[AutoSetupPlayerAnimator] Состояние 'Movement' не найдено в Character@Idle.controller");
                return;
            }

            // Проверяем настроен ли уже Blend Tree
            if (movementState.motion is BlendTree)
            {
                Debug.Log("[AutoSetupPlayerAnimator] Blend Tree уже настроен для состояния 'Movement'!");
                return;
            }

            Debug.Log("[AutoSetupPlayerAnimator] Настройка Blend Tree для движения игрока...");

            // Загрузка клипов анимаций из FBX
            AnimationClip idleClip = LoadAnimationClip("Assets/Models/Character@Idle.fbx");
            AnimationClip walkClip = LoadAnimationClip("Assets/Models/Character@Walking.fbx");
            AnimationClip runClip = LoadAnimationClip("Assets/Models/Character@Running.fbx");

            if (idleClip == null || walkClip == null || runClip == null)
            {
                Debug.LogError("[AutoSetupPlayerAnimator] Не удалось загрузить все необходимые анимационные клипы (Idle, Walking, Running)!");
                return;
            }

            // Создаем Blend Tree
            BlendTree blendTree = new BlendTree
            {
                name = "MovementBlendTree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

            // Добавляем Blend Tree внутрь ассета контроллера для сохранения
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            // Добавляем дочерние клипы
            blendTree.AddChild(idleClip, 0f);      // Speed = 0 (Idle)
            blendTree.AddChild(walkClip, 4f);      // Speed = 4 (walkSpeed)
            blendTree.AddChild(runClip, 7f);       // Speed = 7 (sprintSpeed)

            movementState.motion = blendTree;

            // Включаем IK Pass на Base Layer для поддержки IK рук при переноске
            var layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log("[AutoSetupPlayerAnimator] ✓ Blend Tree движения игрока успешно создан и настроен в Character@Idle.controller!");
        }

        private static AnimationClip LoadAnimationClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
