using UnityEngine;

namespace LemonEmpire.Player
{
    /// <summary>
    /// Привязывает переносимый предмет к костям рук вместо HoldPoint
    /// Предмет будет физически в руках персонажа
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerCarryBoneAttachment : MonoBehaviour
    {
        [Header("Bone Attachment")]
        [SerializeField] private bool useBoneAttachment = true;
        [SerializeField] private HumanBodyBones attachBone = HumanBodyBones.Chest;
        [SerializeField] private Vector3 attachOffset = new Vector3(0f, 0.2f, 0.3f);
        [SerializeField] private Vector3 attachRotation = new Vector3(0f, 0f, 0f);

        [Header("References")]
        [SerializeField] private Transform holdPoint;

        private Animator _animator;
        private PlayerCarry _playerCarry;
        private Transform _attachTransform;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerCarry = GetComponentInParent<PlayerCarry>();

            if (!_animator.isHuman)
            {
                Debug.LogError("[PlayerCarryBoneAttachment] Модель не Humanoid! IK не будет работать.");
                useBoneAttachment = false;
                return;
            }

            // Получаем кость для привязки
            _attachTransform = _animator.GetBoneTransform(attachBone);

            if (_attachTransform == null)
            {
                Debug.LogError($"[PlayerCarryBoneAttachment] Кость {attachBone} не найдена!");
                useBoneAttachment = false;
            }
            else
            {
                Debug.Log($"[PlayerCarryBoneAttachment] Привязка к кости: {attachBone} ({_attachTransform.name})");
            }
        }

        private void LateUpdate()
        {
            if (!useBoneAttachment || _attachTransform == null || _playerCarry == null) return;
            if (!_playerCarry.IsCarrying || holdPoint == null) return;

            // Позиционируем HoldPoint относительно кости
            holdPoint.position = _attachTransform.position +
                _attachTransform.right * attachOffset.x +
                _attachTransform.up * attachOffset.y +
                _attachTransform.forward * attachOffset.z;

            // Применяем поворот
            holdPoint.rotation = _attachTransform.rotation * Quaternion.Euler(attachRotation);
        }

        // Метод для настройки в runtime
        public void SetAttachmentSettings(HumanBodyBones bone, Vector3 offset, Vector3 rotation)
        {
            attachBone = bone;
            attachOffset = offset;
            attachRotation = rotation;

            if (_animator != null && _animator.isHuman)
            {
                _attachTransform = _animator.GetBoneTransform(attachBone);
            }
        }

        private void OnDrawGizmos()
        {
            if (!useBoneAttachment || _attachTransform == null) return;

            // Показываем точку привязки
            Gizmos.color = Color.green;
            Vector3 attachPos = _attachTransform.position +
                _attachTransform.right * attachOffset.x +
                _attachTransform.up * attachOffset.y +
                _attachTransform.forward * attachOffset.z;

            Gizmos.DrawWireSphere(attachPos, 0.05f);
            Gizmos.DrawLine(_attachTransform.position, attachPos);
        }
    }
}
