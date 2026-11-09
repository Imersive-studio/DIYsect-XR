namespace DIYsect.Classroom
{
    /// <summary>Why the server refused a pointer request.</summary>
    public enum PointerRejection : byte
    {
        BadOrgan = 0,
        TooFast = 1,
        /// <summary>The scalpel steerer (the student) asked for the classmate pointer, which is for phones only.</summary>
        NotAClassmate = 2,
    }
}
