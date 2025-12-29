using RimMonitorWorkTab;
using RimMonitorWorkTab.RimMonitor;
using Verse;

namespace RimMonitorWorkTab.Web
{
    [StaticConstructorOnStartup]
    internal static class WorkTabWebModule
    {
        static WorkTabWebModule()
        {
            if (!RimMonitorAPI.Available)
            {
                Log.Warning("[WorkTab] RimMonitor API not available");
                return;
            }

            RimMonitorAPI.EnqueueOnMainThread(() =>
            {
                RimMonitorAPI.RegisterWebsiteButton(
                    id: "worktab",
                    label: "Work",
                    imageUrl: null,
                    href: "/mod/worktab",
                    newTab: false
                );

                Log.Message("[WorkTab] Registered Work tab button with RimMonitor");
            });
        }
    }
}
