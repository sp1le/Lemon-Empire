using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{
    public class BroomToolRack : MonoBehaviour, IInteractable
    {
        [Header("Hanging Points")]
        [SerializeField] private Transform leftPeg;
        [SerializeField] private Transform rightPeg;

        [Header("Hanging State")]
        [SerializeField] private BroomTool hungBroomLeft;
        [SerializeField] private BroomTool hungBroomRight;

        public string InteractionPrompt
        {
            get
            {
                var player = FindFirstObjectByType<LemonEmpire.Player.PlayerController>();
                if (player == null) return "E — Взаимодействовать";
                var carry = player.GetComponent<LemonEmpire.Player.PlayerCarry>();
                if (carry != null && carry.IsCarrying && carry.CarriedItem != null && carry.CarriedItem.GetComponent<BroomTool>() != null)
                {
                    return "E — Повесить швабру";
                }
                return "E — Взять швабру";
            }
        }

        public bool CanInteract
        {
            get
            {
                var player = FindFirstObjectByType<LemonEmpire.Player.PlayerController>();
                if (player == null) return false;
                var carry = player.GetComponent<LemonEmpire.Player.PlayerCarry>();
                if (carry == null) return false;

                if (carry.IsCarrying && carry.CarriedItem != null)
                {
                    var broom = carry.CarriedItem.GetComponent<BroomTool>();
                    if (broom != null)
                    {
                        return hungBroomLeft == null || hungBroomRight == null;
                    }
                    return false;
                }
                else
                {
                    return hungBroomLeft != null || hungBroomRight != null;
                }
            }
        }

        public void Interact(PlayerInteractionContext context)
        {
            var carry = context.PlayerCarry;
            if (carry == null) return;

            if (carry.IsCarrying && carry.CarriedItem != null)
            {
                var broom = carry.CarriedItem.GetComponent<BroomTool>();
                if (broom == null) return;

                // Take the item from the player
                carry.TakeItem();

                // Hang it
                HangBroom(broom);
            }
            else
            {
                // Take broom from rack and give to player
                BroomTool broomToTake = null;
                if (hungBroomRight != null)
                {
                    broomToTake = hungBroomRight;
                    hungBroomRight = null;
                }
                else if (hungBroomLeft != null)
                {
                    broomToTake = hungBroomLeft;
                    hungBroomLeft = null;
                }

                if (broomToTake != null)
                {
                    carry.TryPickup(broomToTake);
                    Debug.Log($"[BroomToolRack] Broom taken from rack by player");
                }
            }
        }

        public void HangBroom(BroomTool broom)
        {
            if (hungBroomLeft == null)
            {
                hungBroomLeft = broom;
                AttachToPeg(broom, leftPeg);
            }
            else if (hungBroomRight == null)
            {
                hungBroomRight = broom;
                AttachToPeg(broom, rightPeg);
            }
        }

        private void AttachToPeg(BroomTool broom, Transform peg)
        {
            broom.OnPlaced(transform, peg.position - Vector3.up * 1.45f, Quaternion.identity);
            Debug.Log($"[BroomToolRack] Broom hung on peg {peg.name}");
        }

        private void Start()
        {
            // Find left and right pegs if not assigned
            if (leftPeg == null) leftPeg = transform.Find("Rack_Premium/Peg_Left");
            if (rightPeg == null) rightPeg = transform.Find("Rack_Premium/Peg_Right");

            // Look for any BroomTools in the scene that are very close to the pegs
            var brooms = FindObjectsByType<BroomTool>(FindObjectsSortMode.None);
            foreach (var broom in brooms)
            {
                if (broom.transform.parent == null || broom.transform.parent == transform || broom.transform.parent == leftPeg || broom.transform.parent == rightPeg)
                {
                    if (leftPeg != null && Vector3.Distance(broom.transform.position, leftPeg.position - Vector3.up * 1.45f) < 0.5f)
                    {
                        hungBroomLeft = broom;
                        AttachToPeg(broom, leftPeg);
                    }
                    else if (rightPeg != null && Vector3.Distance(broom.transform.position, rightPeg.position - Vector3.up * 1.45f) < 0.5f)
                    {
                        hungBroomRight = broom;
                        AttachToPeg(broom, rightPeg);
                    }
                }
            }
        }

        private void Update()
        {
            // If the broom is picked up, clear the reference.
            if (hungBroomLeft != null && hungBroomLeft.IsBeingCarried)
            {
                hungBroomLeft = null;
            }
            if (hungBroomRight != null && hungBroomRight.IsBeingCarried)
            {
                hungBroomRight = null;
            }
        }
    }
}
