using Verse;

namespace RimMonitorWorkTab.Core
{
    [StaticConstructorOnStartup]
    internal static class ModStartup
    {
        static ModStartup()
        {
            // Intentionally empty
            // Background worker lifetime is owned by WorkTabGameComponent
        }
    }
}
