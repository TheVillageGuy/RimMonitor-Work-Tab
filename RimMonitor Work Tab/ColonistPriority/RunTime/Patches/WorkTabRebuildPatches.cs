using HarmonyLib;
using RimMonitorWorkTab.ColonistPriority.RunTime.Deltas;
using RimMonitorWorkTab.Core;
using RimWorld;
using System.Reflection;
using Verse;

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

        [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority))]
        internal static class Patch_SetPriority
        {
            private static readonly FieldInfo pawnField =
                AccessTools.Field(typeof(Pawn_WorkSettings), "pawn");

            static void Postfix(Pawn_WorkSettings __instance, WorkTypeDef w, int priority)
            {
                if (__instance == null || w == null)
                    return;

                Pawn pawn = pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null)
                    return;

                WorkTabDeltaQueue.Enqueue(new WorkTabDelta
                {
                    Kind = WorkTabDeltaKind.PriorityChanged,
                    PawnThingId = pawn.thingIDNumber,
                    WorkTypeId = w.defName,
                    Priority = priority
                });

                WorkTabGameComponent.WakeWorker();
            }
        }

        [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.useWorkPriorities), MethodType.Setter)]
        private static class Patch_UseWorkPriorities
        {
            static void Postfix()
            {
                if (Current.Game == null) return;

                WorkTabDeltaQueue.Enqueue(new WorkTabDelta
                {
                    Kind = WorkTabDeltaKind.ManualPrioritiesChanged,
                    BoolValue = Current.Game.playSettings.useWorkPriorities
                });

                WorkTabGameComponent.WakeWorker();
            }
        }

        [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.Notify_UseWorkPrioritiesChanged))]
        private static class Patch_Notify_UseWorkPrioritiesChanged
        {
            private static readonly FieldInfo pawnField =
                AccessTools.Field(typeof(Pawn_WorkSettings), "pawn");

            static void Postfix(Pawn_WorkSettings __instance)
            {
                if (__instance == null)
                    return;

                Pawn pawn = pawnField?.GetValue(__instance) as Pawn;
                if (pawn == null)
                    return;

                WorkTabDeltaQueue.Enqueue(new WorkTabDelta
                {
                    Kind = WorkTabDeltaKind.PawnWorkSettingsReapplied,
                    PawnThingId = pawn.thingIDNumber
                });

                WorkTabGameComponent.WakeWorker();
            }
        }
    }
}
