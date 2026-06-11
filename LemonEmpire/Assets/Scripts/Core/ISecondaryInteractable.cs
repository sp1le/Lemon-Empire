namespace LemonEmpire.Core
{
    public interface ISecondaryInteractable
    {
        string SecondaryInteractionPrompt { get; }
        bool CanSecondaryInteract { get; }
        void SecondaryInteract(PlayerInteractionContext context);
    }
}
