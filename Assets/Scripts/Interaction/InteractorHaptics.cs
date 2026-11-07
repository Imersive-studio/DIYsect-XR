// Authority: none. Local presentation only; it vibrates a controller on this device and sends nothing over the network.
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The one place that turns "pulse this interactor" into the XR Interaction Toolkit call, so the other components do not repeat it.
    /// The hand that is hovering or holding is an XRBaseInputInteractor (direct, ray or near-far); it forwards the pulse to the controller's haptic player.
    /// </summary>
    public static class InteractorHaptics
    {
        /// <summary>Vibrates the controller behind <paramref name="interactor"/>. Returns false if there is nothing to vibrate (no controller, destroyed, or no haptic output).</summary>
        public static bool Send(IXRInteractor interactor, HapticPulse pulse)
        {
            if (!(interactor is XRBaseInputInteractor input) || input == null || pulse.Amplitude <= 0f || pulse.DurationSeconds <= 0f)
                return false;
            return input.SendHapticImpulse(pulse.Amplitude, pulse.DurationSeconds);
        }
    }
}
