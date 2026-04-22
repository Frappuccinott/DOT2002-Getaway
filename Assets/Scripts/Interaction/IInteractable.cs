public interface IInteractable
{
    string InteractionPrompt { get; }
    InteractionType Type { get; }
    bool CanInteract { get; }
    void Interact() { }
}
