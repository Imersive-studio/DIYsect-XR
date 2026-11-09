namespace DIYsect.Learning
{
    /// <summary>The answer of <see cref="OrganInfoCardRules"/>: the state to move to and what the view must do about it.</summary>
    public readonly struct OrganInfoCardStep
    {
        public OrganInfoCardStep(OrganInfoCardState next, OrganInfoCardEffect effect)
        {
            Next = next;
            Effect = effect;
        }

        public OrganInfoCardState Next { get; }
        public OrganInfoCardEffect Effect { get; }
    }
}
