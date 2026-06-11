using UnityEngine;

namespace LemonEmpire.Player
{
    /// <summary>
    /// Attach this script DIRECTLY to the 3D model that has the Animator component.
    /// It uses Inverse Kinematics (IK) to pull the hands toward the carried object automatically.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerCarryIK : MonoBehaviour
    {
        private Animator _animator;
        private PlayerCarry _playerCarry;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerCarry = GetComponentInParent<PlayerCarry>();

            if (!_animator.isHuman)
            {
                Debug.LogError("PlayerCarryIK: Твоя модель не Humanoid! Вкладка Rig -> Animation Type -> поставь Humanoid. Иначе IK не сработает!");
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || _playerCarry == null || !_animator.isHuman) return;

            // Optional: to use IK, the Animator Controller must have "IK Pass" enabled in settings for the Base Layer!

            if (_playerCarry.IsCarrying && _playerCarry.CarriedItem != null)
            {
                var carriedTransform = _playerCarry.CarriedItem.transform;

                // Максимальный вес IK для точного позиционирования
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);

                // Находим размеры ящика
                float boxBottomOffsetY = -0.3f;
                float boxWidth = 0.5f;

                var boxCol = _playerCarry.CarriedItem.GetComponent<BoxCollider>();
                if (boxCol != null)
                {
                    // Нижняя грань ящика
                    boxBottomOffsetY = (boxCol.center.y - boxCol.size.y * 0.5f) * carriedTransform.localScale.y;
                    // Ширина ящика для расстановки рук
                    boxWidth = boxCol.size.x * carriedTransform.localScale.x;
                }

                // Точка на дне ящика (центр)
                Vector3 boxBottom = carriedTransform.position + carriedTransform.up * boxBottomOffsetY;

                // Руки держат ящик снизу по бокам
                float handSpacing = Mathf.Max(0.2f, boxWidth * 0.4f); // Минимум 20см между руками

                Vector3 leftHandPos = boxBottom - carriedTransform.right * handSpacing;
                Vector3 rightHandPos = boxBottom + carriedTransform.right * handSpacing;

                // Руки чуть впереди для естественного хвата
                Vector3 forwardOffset = carriedTransform.forward * 0.1f;
                leftHandPos += forwardOffset;
                rightHandPos += forwardOffset;

                _animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);
                _animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);

                // Ладони смотрят вверх (держат снизу)
                Quaternion handRotation = Quaternion.LookRotation(carriedTransform.forward, carriedTransform.up);
                _animator.SetIKRotation(AvatarIKGoal.LeftHand, handRotation);
                _animator.SetIKRotation(AvatarIKGoal.RightHand, handRotation);

                // Debug визуализация
                Debug.DrawLine(leftHandPos, rightHandPos, Color.green);
                Debug.DrawLine(boxBottom, leftHandPos, Color.yellow);
                Debug.DrawLine(boxBottom, rightHandPos, Color.yellow);
            }
            else
            {
                // Отпускаем руки
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            }
        }
    }
}
