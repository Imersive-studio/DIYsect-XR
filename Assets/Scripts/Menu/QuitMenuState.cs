// Authority: none. Local presentation only, sends nothing over the network.
namespace DIYsect.Menu
{
    /// <summary>What the in-app quit menu is showing on this client.</summary>
    public enum QuitMenuState
    {
        /// <summary>The menu is hidden.</summary>
        Closed,

        /// <summary>The menu shows Resume and Quit.</summary>
        Open,

        /// <summary>The menu asks "Leave the dissection?" and waits for Yes or Stay.</summary>
        Confirming,
    }

    /// <summary>Things that can happen to the quit menu.</summary>
    public enum QuitMenuEvent
    {
        /// <summary>The menu button, a key or a hand gesture.</summary>
        Toggle,

        /// <summary>The Quit or Yes button.</summary>
        Quit,

        /// <summary>The Resume or Stay button.</summary>
        Cancel,
    }
}
