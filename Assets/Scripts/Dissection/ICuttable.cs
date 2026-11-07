using System;
using UnityEngine;

namespace DIYsect.Dissection
{
    /// <summary>What a cutting tool needs from the thing it cuts. Lets tools stay ignorant of networking.</summary>
    public interface ICuttable
    {
        DissectionStage Stage { get; }

        /// <summary>Raised on every peer when the stage changes. The flag is true for the state a peer sees when it spawns.</summary>
        event Action<DissectionStage, bool> StageChanged;

        /// <summary>Reports where the blade touches, in world space. The target decides whether it counts.</summary>
        void SubmitContact(Vector3 worldPoint);

        /// <summary>Asks for the cut skin to be opened. Only honoured once the incision is complete.</summary>
        void RequestOpen();
    }
}
