using Memories.Contracts;

namespace Memories.Gameplay.Interaction
{
    public sealed class InteractionPresenter
    {
        private readonly IInteractionInput input;
        private readonly InteractionProbe probe;
        private readonly IInteractionView view;

        public InteractionPresenter(IInteractionInput input, InteractionProbe probe, IInteractionView view)
        {
            this.input = input;
            this.probe = probe;
            this.view = view;
        }

        public void Tick()
        {
            if (!input.InteractionEnabled)
            {
                view.Show(false, false, "");
                return;
            }
            var target = probe.FindTarget();
            if (target != null && input.InteractPressed)
            {
                target.Interact();
                // Interaction may destroy, disable or move the target.
                target = probe.FindTarget();
            }
            view.Show(true, target != null, target != null ? "[E] " + target.Prompt : "");
        }
    }
}
