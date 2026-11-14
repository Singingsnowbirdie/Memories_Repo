using UnityEngine;
namespace Memories.Gameplay.Reactions
{
    public sealed class ParkCinemaActionReaction : CustomReaction
    {
        public enum CinemaAction { OpenCashier, TicketsPurchased, Finish }
        [SerializeField] private ParkCinemaSequence sequence;
        [SerializeField] private CinemaAction action;
        public override void Execute()
        {
            switch (action)
            {
                case CinemaAction.OpenCashier: sequence.OpenCashier(); break;
                case CinemaAction.TicketsPurchased: sequence.TicketsPurchased(); break;
                case CinemaAction.Finish: sequence.Finish(); break;
            }
        }
    }
}
