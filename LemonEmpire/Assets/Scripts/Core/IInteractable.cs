namespace LemonEmpire.Core
{

    public interface IInteractable
    {

        string InteractionPrompt { get; }

        bool CanInteract { get; }

        void Interact(PlayerInteractionContext context);
    }

    public struct PlayerInteractionContext
    {
        public UnityEngine.Transform PlayerTransform;
        public LemonEmpire.Player.PlayerCarry PlayerCarry;
    }
}
