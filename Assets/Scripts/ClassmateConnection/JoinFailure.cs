namespace DIYsect.ClassmateConnection
{
    /// <summary>Why a classmate could not get into the room, as far as the lobby messages let us tell.</summary>
    public enum JoinFailure
    {
        None,
        /// <summary>No room has that code. The lobby service says the same for a room that was closed.</summary>
        RoomNotFound,
        RoomFull,
        /// <summary>The room is gone. Only known when the classmate had been in it.</summary>
        RoomClosed,
        TooManyTries,
        /// <summary>Signing in failed, which in practice means no internet.</summary>
        NoInternet,
        /// <summary>The room exists but the connection to the professor's headset failed.</summary>
        HostUnreachable,
        /// <summary>A join was already running. Not worth telling the classmate.</summary>
        Busy,
        /// <summary>Anything else, including network errors that carry no reason.</summary>
        CouldNotJoin,
    }
}
