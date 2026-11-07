// Authority: none. Presentation only. See LayerHinge.
namespace DIYsect.Dissection
{
    /// <summary>
    /// The hinge of one skin flap: a <see cref="LayerHinge"/> that opens at <see cref="DissectionStage.SkinOpen"/>
    /// (the default open stage). Kept as its own type so the skin flaps stay identifiable and their serialized
    /// data is unchanged.
    /// </summary>
    public class SkinFlapHinge : LayerHinge
    {
    }
}
