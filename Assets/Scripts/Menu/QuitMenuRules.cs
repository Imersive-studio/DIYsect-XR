// Authority: none. Local presentation only, sends nothing over the network.
namespace DIYsect.Menu
{
    /// <summary>The rule table for the quit menu. Plain C#, so it is tested without a scene.</summary>
    public static class QuitMenuRules
    {
        /// <summary>The state after the event. Anything not listed leaves the state unchanged.</summary>
        public static QuitMenuState Next(QuitMenuState state, QuitMenuEvent menuEvent)
        {
            switch (state)
            {
                case QuitMenuState.Closed:
                    return menuEvent == QuitMenuEvent.Toggle ? QuitMenuState.Open : state;
                case QuitMenuState.Open:
                    switch (menuEvent)
                    {
                        case QuitMenuEvent.Toggle: return QuitMenuState.Closed;
                        case QuitMenuEvent.Quit: return QuitMenuState.Confirming;
                        case QuitMenuEvent.Cancel: return QuitMenuState.Closed;
                    }
                    return state;
                case QuitMenuState.Confirming:
                    return menuEvent == QuitMenuEvent.Cancel ? QuitMenuState.Open : state;
                default:
                    return state;
            }
        }

        /// <summary>True when the event, in this state, means the app should quit now.</summary>
        public static bool IsQuitRequested(QuitMenuState state, QuitMenuEvent menuEvent)
        {
            return state == QuitMenuState.Confirming && menuEvent == QuitMenuEvent.Quit;
        }
    }
}
