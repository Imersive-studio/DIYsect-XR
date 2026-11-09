using System;

namespace DIYsect.Learning
{
    /// <summary>Turns an organ's object name from a question into its index in the shared body. Pure logic.</summary>
    public static class QuizOrganLookup
    {
        /// <summary>
        /// The index of the organ whose name matches <paramref name="target"/> (ignoring capitals), or -1 when the name is
        /// empty or no organ has it. <paramref name="nameAt"/> returns the object name of the organ at an index.
        /// </summary>
        public static int IndexOf(int organCount, Func<int, string> nameAt, string target)
        {
            if (nameAt == null)
                throw new ArgumentNullException(nameof(nameAt));
            if (string.IsNullOrWhiteSpace(target))
                return -1;
            string wanted = target.Trim();
            for (int i = 0; i < organCount; i++)
                if (string.Equals(nameAt(i), wanted, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }
    }
}
