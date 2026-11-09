namespace DIYsect.Interaction
{
    /// <summary>
    /// The words a label shows for one organ: its name, the body system it belongs to (for example "Digestive system")
    /// and a two to four word role (for example "Filters blood"). Either of the last two can be empty.
    /// </summary>
    public readonly struct OrganCaption
    {
        public OrganCaption(string name, string system, string role)
        {
            Name = name ?? string.Empty;
            System = system ?? string.Empty;
            Role = role ?? string.Empty;
        }

        public string Name { get; }
        public string System { get; }
        public string Role { get; }
    }
}
