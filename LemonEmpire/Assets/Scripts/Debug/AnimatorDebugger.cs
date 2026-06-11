using UnityEngine;

namespace LemonEmpire.Debugging
{
    /// <summary>
    /// Детальная отладка Animator для диагностики задержек анимаций
    /// Показывает текущее состояние, переходы и параметры в реальном времени
    /// </summary>
    public class AnimatorDebugger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enableDebug = true;
        [SerializeField] private bool showOnScreenGUI = true;
        [SerializeField] private float updateInterval = 0.1f;

        private Animator _animator;
        private float _lastUpdateTime;

        private string _currentStateName = "";
        private string _nextStateName = "";
        private float _transitionProgress = 0f;
        private bool _isInTransition = false;

        private float _speedParam = 0f;
        private bool _isGroundedParam = false;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("[AnimatorDebugger] Animator не найден на этом объекте!");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!enableDebug || _animator == null) return;

            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                UpdateAnimatorInfo();
                _lastUpdateTime = Time.time;
            }
        }

        private void UpdateAnimatorInfo()
        {
            // Получаем информацию о текущем состоянии
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo nextStateInfo = _animator.GetNextAnimatorStateInfo(0);

            _isInTransition = _animator.IsInTransition(0);

            // Получаем имена состояний
            _currentStateName = GetStateName(stateInfo);
            _nextStateName = _isInTransition ? GetStateName(nextStateInfo) : "";

            // Прогресс перехода
            if (_isInTransition)
            {
                AnimatorTransitionInfo transitionInfo = _animator.GetAnimatorTransitionInfo(0);
                _transitionProgress = transitionInfo.normalizedTime;
            }

            // Параметры
            _speedParam = _animator.GetFloat("Speed");
            _isGroundedParam = _animator.GetBool("IsGrounded");

            // Логи в Console
            if (_isInTransition)
            {
                Debug.Log($"[AnimatorDebugger] ПЕРЕХОД: {_currentStateName} → {_nextStateName} ({_transitionProgress:P0}) | Speed: {_speedParam:F2}");
            }
            else
            {
                Debug.Log($"[AnimatorDebugger] Состояние: {_currentStateName} | Speed: {_speedParam:F2} | Grounded: {_isGroundedParam}");
            }
        }

        private string GetStateName(AnimatorStateInfo stateInfo)
        {
            // Пытаемся получить читаемое имя состояния
            if (stateInfo.IsName("Idle")) return "Idle";
            if (stateInfo.IsName("Walk")) return "Walk";
            if (stateInfo.IsName("Run")) return "Run";
            if (stateInfo.IsName("Jump")) return "Jump";
            if (stateInfo.IsName("Fall")) return "Fall";

            // Если не нашли, возвращаем хеш
            return $"State_{stateInfo.shortNameHash}";
        }

        private void OnGUI()
        {
            if (!enableDebug || !showOnScreenGUI || _animator == null) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            style.padding = new RectOffset(10, 10, 10, 10);

            string info = "=== ANIMATOR DEBUG ===\n\n";

            if (_isInTransition)
            {
                info += $"<color=yellow>ПЕРЕХОД: {_currentStateName} → {_nextStateName}</color>\n";
                info += $"Прогресс: {_transitionProgress:P0}\n\n";
            }
            else
            {
                info += $"<color=lime>Состояние: {_currentStateName}</color>\n\n";
            }

            info += $"Speed: {_speedParam:F2}\n";
            info += $"IsGrounded: {_isGroundedParam}\n";

            GUI.Box(new Rect(10, 10, 300, 150), info, style);
        }

        // Метод для проверки настроек Animator Controller
        [ContextMenu("Проверить настройки Animator Controller")]
        public void ValidateAnimatorController()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[AnimatorDebugger] Animator Controller не назначен!");
                return;
            }

            Debug.Log("=== ПРОВЕРКА ANIMATOR CONTROLLER ===");

            // Проверяем параметры
            bool hasSpeed = false;
            bool hasGrounded = false;
            bool hasJump = false;

            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                if (param.name == "Speed") hasSpeed = true;
                if (param.name == "IsGrounded") hasGrounded = true;
                if (param.name == "Jump") hasJump = true;
            }

            Debug.Log($"Параметр 'Speed': {(hasSpeed ? "✓" : "✗ ОТСУТСТВУЕТ")}");
            Debug.Log($"Параметр 'IsGrounded': {(hasGrounded ? "✓" : "✗ ОТСУТСТВУЕТ")}");
            Debug.Log($"Параметр 'Jump': {(hasJump ? "✓" : "✗ ОТСУТСТВУЕТ")}");

            Debug.Log($"\nВсего слоёв: {_animator.layerCount}");

            Debug.Log("\n=== РЕКОМЕНДАЦИИ ===");
            Debug.Log("1. Проверьте Has Exit Time = false для всех переходов движения");
            Debug.Log("2. Transition Duration должен быть 0.1-0.25");
            Debug.Log("3. Включите IK Pass в Base Layer для работы рук");
        }
    }
}
