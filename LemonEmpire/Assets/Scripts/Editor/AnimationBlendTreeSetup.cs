using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace LemonEmpire.Editor
{
    /// <summary>
    /// Автоматическое создание Blend Tree для плавных переходов анимаций
    /// Меню: Lemon Empire → Setup Animation Blend Tree
    /// </summary>
    public class AnimationBlendTreeSetup : EditorWindow
    {
        private AnimatorController animatorController;
        private AnimationClip idleClip;
        private AnimationClip walkClip;
        private AnimationClip runClip;
        private AnimationClip jumpClip;

        [MenuItem("Lemon Empire/Setup Animation Blend Tree")]
        public static void ShowWindow()
        {
            GetWindow<AnimationBlendTreeSetup>("Blend Tree Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Настройка Blend Tree для плавных переходов", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Этот инструмент создаст Blend Tree для плавных переходов между Idle/Walk/Run.\n\n" +
                "Преимущества:\n" +
                "• Плавные переходы без рывков\n" +
                "• Автоматическое смешивание анимаций\n" +
                "• Нет задержек при изменении скорости",
                MessageType.Info);

            GUILayout.Space(10);

            animatorController = (AnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller",
                animatorController,
                typeof(AnimatorController),
                false);

            GUILayout.Space(5);

            idleClip = (AnimationClip)EditorGUILayout.ObjectField(
                "Idle Animation",
                idleClip,
                typeof(AnimationClip),
                false);

            walkClip = (AnimationClip)EditorGUILayout.ObjectField(
                "Walk Animation",
                walkClip,
                typeof(AnimationClip),
                false);

            runClip = (AnimationClip)EditorGUILayout.ObjectField(
                "Run Animation",
                runClip,
                typeof(AnimationClip),
                false);

            GUILayout.Space(5);

            jumpClip = (AnimationClip)EditorGUILayout.ObjectField(
                "Jump Animation (optional)",
                jumpClip,
                typeof(AnimationClip),
                false);

            GUILayout.Space(15);

            GUI.enabled = animatorController != null && idleClip != null && walkClip != null && runClip != null;

            if (GUILayout.Button("Создать Blend Tree", GUILayout.Height(40)))
            {
                CreateBlendTree();
            }

            GUI.enabled = true;

            GUILayout.Space(10);

            if (GUILayout.Button("Найти анимации автоматически"))
            {
                AutoFindAnimations();
            }
        }

        private void AutoFindAnimations()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                if (clip == null) continue;

                string name = clip.name.ToLower();

                if (name.Contains("idle") && idleClip == null)
                    idleClip = clip;
                else if (name.Contains("walk") && walkClip == null)
                    walkClip = clip;
                else if (name.Contains("run") && runClip == null)
                    runClip = clip;
                else if (name.Contains("jump") && jumpClip == null)
                    jumpClip = clip;
            }

            if (idleClip != null || walkClip != null || runClip != null)
            {
                Debug.Log("[BlendTreeSetup] Найдены анимации автоматически!");
                Repaint();
            }
            else
            {
                EditorUtility.DisplayDialog("Не найдено",
                    "Не удалось найти анимации автоматически.\n\nНазначьте их вручную.",
                    "OK");
            }
        }

        private void CreateBlendTree()
        {
            if (animatorController == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Назначьте Animator Controller!", "OK");
                return;
            }

            // Получаем Base Layer
            AnimatorControllerLayer baseLayer = animatorController.layers[0];
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;

            // Создаём Blend Tree
            BlendTree blendTree = new BlendTree
            {
                name = "Movement",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

            // Добавляем анимации в Blend Tree
            blendTree.AddChild(idleClip, 0f);      // Speed = 0
            blendTree.AddChild(walkClip, 4f);      // Speed = 4
            blendTree.AddChild(runClip, 7f);       // Speed = 7

            // Создаём состояние Movement
            AnimatorState movementState = stateMachine.AddState("Movement", new Vector3(300, 100, 0));
            movementState.motion = blendTree;

            // Проверяем параметры
            EnsureParameter("Speed", AnimatorControllerParameterType.Float);
            EnsureParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            EnsureParameter("Jump", AnimatorControllerParameterType.Trigger);

            // Если есть Jump анимация, создаём состояние Jump
            if (jumpClip != null)
            {
                AnimatorState jumpState = stateMachine.AddState("Jump", new Vector3(300, 200, 0));
                jumpState.motion = jumpClip;

                // Movement → Jump
                AnimatorStateTransition toJump = movementState.AddTransition(jumpState);
                toJump.hasExitTime = false;
                toJump.duration = 0.1f;
                toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

                // Jump → Movement
                AnimatorStateTransition toMovement = jumpState.AddTransition(movementState);
                toMovement.hasExitTime = false;
                toMovement.duration = 0.25f;
                toMovement.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            }

            // Устанавливаем Movement как Default State
            stateMachine.defaultState = movementState;

            // Сохраняем изменения
            EditorUtility.SetDirty(animatorController);
            AssetDatabase.SaveAssets();

            Debug.Log("[BlendTreeSetup] ✓ Blend Tree создан успешно!");
            EditorUtility.DisplayDialog("Готово",
                "Blend Tree создан!\n\n" +
                "Теперь переходы между Idle/Walk/Run будут плавными.\n\n" +
                "Удалите старые состояния Idle/Walk/Run если они остались.",
                "OK");
        }

        private void EnsureParameter(string paramName, AnimatorControllerParameterType type)
        {
            foreach (var param in animatorController.parameters)
            {
                if (param.name == paramName)
                    return;
            }

            animatorController.AddParameter(paramName, type);
        }
    }
}
