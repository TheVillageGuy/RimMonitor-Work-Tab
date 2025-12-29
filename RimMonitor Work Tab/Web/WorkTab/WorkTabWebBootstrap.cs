using RimMonitorWorkTab.RimMonitor;
using Verse;

namespace RimMonitorWorkTab.Web
{
    /// <summary>
    /// Registers the Work Tab HTTP routes with RimMonitor's web server.
    ///
    /// This file contains no reflection and no RimMonitor references.
    /// All integration details are encapsulated in RimMonitorApi.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class WorkTabWebBootstrap
    {
        static WorkTabWebBootstrap()
        {
            Verse.Log.Message("[WorkTab] Web bootstrap static ctor running");
            RimMonitorAPI.RegisterExternalWebHandler(WorkTabRouter.Handle);
        }
    }
}
