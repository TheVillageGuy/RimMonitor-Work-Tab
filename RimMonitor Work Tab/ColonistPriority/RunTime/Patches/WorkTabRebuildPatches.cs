using HarmonyLib;
using RimWorld;
using Verse;
using RimMonitorWorkTab.Core;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.Patches
{
    [StaticConstructorOnStartup]
    internal static class WorkTabRebuildPatches
    {
        static WorkTabRebuildPatches()
        {
            var harmony = new Harmony("rimmonitor.worktab.rebuilds");
            harmony.PatchAll();
        }

        /* =========================================================
           Work priority mutations (enable/disable included)
           ========================================================= */

        [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority))]
        private static class Patch_SetPriority
        {
            static void Postfix()
            {
                WorkTabGameComponent.RequestRebuild();
            }
        }

        /* =========================================================
           Manual priorities toggle
           ========================================================= */

        [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.useWorkPriorities), MethodType.Setter)]
        private static class Patch_UseWorkPriorities
        {
            static void Postfix()
            {
                WorkTabGameComponent.RequestRebuild();
            }
        }

        /* =========================================================
           Ideology / mode reapplication
           ========================================================= */

        [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.Notify_UseWorkPrioritiesChanged))]
        private static class Patch_NotifyWorkSettingsChanged
        {
            static void Postfix()
            {
                WorkTabGameComponent.RequestRebuild();
            }
        }
    }
}
