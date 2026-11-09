using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>What a headset needs to say "this is where my head is". Lets the Interaction code stay free of the classroom code.</summary>
    public interface IHeadPoseSink
    {
        /// <summary>Sends this client's head pose in the body's local space, so every device can place it on its own copy of the body.</summary>
        void Publish(Vector3 localPosition, Quaternion localRotation);

        /// <summary>Tells the server this client no longer has a head to show.</summary>
        void Withdraw();
    }
}
