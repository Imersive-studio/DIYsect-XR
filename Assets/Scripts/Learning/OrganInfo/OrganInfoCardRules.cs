namespace DIYsect.Learning
{
    /// <summary>
    /// The rule table for one organ's info card: (state, event) -> next state. Plain C#, no Unity, so it is unit tested.
    /// A card is shown while the organ is held or pointed at, and hidden when it is neither. Letting go of an organ ends
    /// the hold in every case (put back in its socket, eased home, or dropped loose), so one event covers "put back".
    /// If a pointer still rests on the organ after it is let go, the card stays.
    /// </summary>
    public static class OrganInfoCardRules
    {
        private const OrganInfoCardState Hid = OrganInfoCardState.Hidden;
        private const OrganInfoCardState Ptd = OrganInfoCardState.Pointed;
        private const OrganInfoCardState Hld = OrganInfoCardState.Held;
        private const OrganInfoCardState Both = OrganInfoCardState.HeldAndPointed;

        // Rows are the current state, columns are the event.
        //                                   PointerEntered  PointerLeft  Grabbed  Released
        private static readonly OrganInfoCardState[,] Table =
        {
            /* Hidden         */ { Ptd,   Hid, Hld,  Hid },
            /* Pointed        */ { Ptd,   Hid, Both, Ptd },
            /* Held           */ { Both,  Hld, Hld,  Hid },
            /* HeldAndPointed */ { Both,  Hld, Both, Ptd },
        };

        /// <summary>Gives the next state and the effect for the view. An unknown state or event changes nothing.</summary>
        public static OrganInfoCardStep Apply(OrganInfoCardState state, OrganInfoCardEvent cardEvent)
        {
            if ((int)state >= Table.GetLength(0) || (int)cardEvent >= Table.GetLength(1))
                return new OrganInfoCardStep(state, OrganInfoCardEffect.None);

            OrganInfoCardState next = Table[(int)state, (int)cardEvent];
            bool wasShown = IsShown(state);
            bool isShown = IsShown(next);
            OrganInfoCardEffect effect = wasShown == isShown
                ? OrganInfoCardEffect.None
                : (isShown ? OrganInfoCardEffect.Show : OrganInfoCardEffect.Hide);
            return new OrganInfoCardStep(next, effect);
        }

        /// <summary>True when the card is visible in this state.</summary>
        public static bool IsShown(OrganInfoCardState state)
        {
            return state != OrganInfoCardState.Hidden;
        }

        /// <summary>True when a pointer rests on the organ in this state.</summary>
        public static bool IsPointed(OrganInfoCardState state)
        {
            return state == OrganInfoCardState.Pointed || state == OrganInfoCardState.HeldAndPointed;
        }
    }
}
