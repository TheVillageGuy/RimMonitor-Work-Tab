using RimMonitorWorkTab.ColonistPriority.RunTime.Capture;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.Capture
{
    internal static class WorkTabRawSnapshotBuilder
    {

        private static List<WorkTypeDef> GetWorkTypesInWorkTabOrder()
        {
            var result = new List<WorkTypeDef>();

            foreach (PawnColumnDef col in PawnTableDefOf.Work.columns)
            {
                if (col.defName == null)
                    continue;

                // RimWorld convention: WorkPriority_<WorkTypeDefName>
                const string prefix = "WorkPriority_";
                if (!col.defName.StartsWith(prefix))
                    continue;

                string workTypeDefName = col.defName.Substring(prefix.Length);

                WorkTypeDef wt = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeDefName);
                if (wt != null)
                {
                    result.Add(wt);
                }
            }

            return result;
        }

        public static WorkTabRawSnapshot Capture()
        {
            var raw = new WorkTabRawSnapshot
            {
                ManualPrioritiesEnabled =
                    Find.PlaySettings != null &&
                    Find.PlaySettings.useWorkPriorities
            };

            var orderedWorkTypes = GetWorkTypesInWorkTabOrder();

            foreach (var wt in orderedWorkTypes)
            {
                string label = wt.LabelCap.ToString();

                string shortLabel;
                if (!string.IsNullOrEmpty(wt.labelShort))
                    shortLabel = wt.labelShort.ToString();
                else if (!string.IsNullOrEmpty(label))
                    shortLabel = label.Substring(0, 1);
                else
                    shortLabel = "";

                raw.WorkTypes.Add(new RawWorkType
                {
                    DefName = wt.defName,
                    Label = label,
                    ShortLabel = shortLabel
                });
            }


            foreach (var map in Find.Maps)
            {
                if (map == null) continue;

                var rawMap = new RawMap
                {
                    MapId = map.uniqueID,
                    MapLabel = map.Parent?.LabelCap ?? "Map"
                };

                var pawns = map.mapPawns?.FreeColonistsSpawned;
                if (pawns == null) continue;

                foreach (var pawn in pawns)
                {
                    if (pawn?.workSettings == null) continue;

                    var rawPawn = new RawPawn
                    {
                        PawnThingId = pawn.thingIDNumber,
                        Name = pawn.LabelShortCap
                    };

                    foreach (var wt in orderedWorkTypes)
                    {
                        int priority = pawn.workSettings.GetPriority(wt);

                        bool ideologyOpposed =
                            ModsConfig.IdeologyActive &&
                            pawn.Ideo != null &&
                            pawn.Ideo.IsWorkTypeConsideredDangerous(wt);

                        int? skill = null;
                        int? passion = null;

                        if (wt.relevantSkills.Count > 0)
                        {
                            var rec = pawn.skills?.GetSkill(wt.relevantSkills[0]);
                            if (rec != null)
                            {
                                skill = rec.Level;
                                passion = (int)rec.passion;
                            }
                        }

                        bool available = !pawn.WorkTypeIsDisabled(wt);

                        rawPawn.CellsByWorkType[wt.defName] = new RawCell
                        {
                            Priority = priority,
                            SkillLevel = skill,
                            Passion = passion,
                            IdeologyOpposed = ideologyOpposed,
                            Available = available
                        };
                    }


                    rawMap.Pawns.Add(rawPawn);
                }

                raw.Maps.Add(rawMap);
            }

            return raw;
        }
    }
}
