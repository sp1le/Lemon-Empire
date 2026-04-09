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
                var holdPoint = _playerCarry.CarriedItem.transform;

                // Turn on IK IK interpolation
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);

                // Find the local bottom of the box without relying on world physics updates
                float boxBottomOffsetY = -0.3f; // Default fallback
                var boxCol = _playerCarry.CarriedItem.GetComponent<BoxCollider>();
                if (boxCol != null)
                {
                    // Perfectly calculates the distance to the bottom floor of the box based on its local center and size
                    boxBottomOffsetY = (boxCol.center.y - boxCol.size.y * 0.5f) * holdPoint.localScale.y;
                }
                
                // We add a tiny bit (0.05) so hands clip slightly into the bottom instead of hovering below
                Vector3 bottomCenter = holdPoint.position + transform.up * (boxBottomOffsetY + 0.05f);

                // Place hands UNDER the box
                // 0.25f apart from center, 0.2f forward into the box floor
                Vector3 leftHandPos = bottomCenter - transform.right * 0.25f + transform.forward * 0.2f;
                Vector3 rightHandPos = bottomCenter + transform.right * 0.25f + transform.forward * 0.2f;

                _animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);
                _animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);

                // Force wrist rotation so palms face UP (like holding a tray)
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
                
                // We point the fingers forward and the palm upward
                _animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.LookRotation(transform.forward, transform.up));
                _animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(transform.forward, transform.up));
            }
            else
            {
                // Unclasp hands
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            }
        }
    }
}
