using Memories.Contracts;

namespace Memories.Gameplay.Interaction
{
    public sealed class InteractionPresenter
    {
        private readonly IInteractionInput input;
        private readonly InteractionProbe probe;
        private readonly IInteractionView view;
        private readonly UI.StoryTextPresenter texts;
        private readonly UI.GameplayModeState gameplayState;

        public InteractionPresenter(IInteractionInput input, InteractionProbe probe, IInteractionView view,
            UI.StoryTextPresenter texts = null, UI.GameplayModeState gameplayState = null)
        {
            this.input = input;
            this.probe = probe;
            this.view = view;
            this.texts = texts;
            this.gameplayState = gameplayState;
        }

        public void Tick()
        {
            if (!input.InteractionEnabled || texts?.BlocksInteractions == true || gameplayState?.ControlsBlocked == true)
            {
                view.Show(false, false, "");
                return;
            }
            var target = probe.FindTarget();
            if (target != null && input.InteractPressed)
            {
                target.Interact();
                if (texts?.BlocksInteractions == true || gameplayState?.ControlsBlocked == true)
                {
                    view.Show(false, false, "");
                    return;
                }
                // Interaction may destroy, disable or move the target.
                target = probe.FindTarget();
            }
            view.Show(true, target != null, target != null ? "[E] " + target.Prompt : "");
        }
    }
}
