namespace DIYsect.Networking
{
    /// <summary>What a headset needs to say "I point at this organ". Lets the Interaction code stay free of the classroom code.</summary>
    public interface IOrganPointer
    {
        /// <summary>Asks the server to show this client's pointer on the organ with this index in <see cref="SharedBody.Organs"/>.</summary>
        void Point(int organIndex);

        /// <summary>Asks the server to remove this client's pointer.</summary>
        void StopPointing();
    }
}
