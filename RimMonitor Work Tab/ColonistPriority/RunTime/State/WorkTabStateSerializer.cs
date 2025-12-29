using System.Text;

namespace RimMonitorWorkTab.ColonistPriority.RunTime.State
{
    internal static class WorkTabStateSerializer
    {
        public static string Serialize(WorkTabWorldState state)
        {
            if (state == null)
                return "{}";

            StringBuilder sb = new StringBuilder(16384);

            sb.Append('{');

            sb.Append("\"manualPriorities\":")
              .Append(state.ManualPrioritiesEnabled ? "true" : "false");

            sb.Append(",\"workTypes\":[");
            for (int i = 0; i < state.WorkTypes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var wt = state.WorkTypes[i];

                sb.Append('{')
                  .Append("\"id\":\"").Append(wt.Id).Append("\",")
                  .Append("\"label\":\"").Append(wt.Label).Append("\",")
                  .Append("\"shortLabel\":\"").Append(wt.ShortLabel).Append("\"")
                  .Append('}');
            }
            sb.Append(']');

            sb.Append(",\"maps\":[");
            for (int m = 0; m < state.Maps.Count; m++)
            {
                if (m > 0) sb.Append(',');
                var map = state.Maps[m];

                sb.Append('{')
                  .Append("\"mapId\":").Append(map.MapId).Append(',')
                  .Append("\"label\":\"").Append(map.MapLabel).Append("\",");

                sb.Append("\"pawns\":[");
                for (int p = 0; p < map.Pawns.Count; p++)
                {
                    if (p > 0) sb.Append(',');
                    var pawn = map.Pawns[p];

                    sb.Append('{')
                      .Append("\"id\":").Append(pawn.PawnThingId).Append(',')
                      .Append("\"name\":\"").Append(pawn.Name).Append("\",");

                    sb.Append("\"cells\":{");
                    bool first = true;
                    foreach (var kv in pawn.CellsByWorkType)
                    {
                        if (!first) sb.Append(',');
                        first = false;

                        var c = kv.Value;
                        sb.Append('"').Append(kv.Key).Append("\":{")
                          .Append("\"p\":").Append(c.Priority).Append(',')
                          .Append("\"s\":").Append(c.SkillLevel.HasValue ? c.SkillLevel.Value.ToString() : "null").Append(',')
                          .Append("\"pa\":").Append(c.Passion.HasValue ? c.Passion.Value.ToString() : "null").Append(',')
                          .Append("\"i\":").Append(c.IdeologyOpposed ? "true" : "false")
                          .Append('}');
                    }
                    sb.Append('}'); // cells

                    sb.Append('}');
                }
                sb.Append(']'); // pawns

                sb.Append('}');
            }
            sb.Append(']'); // maps

            sb.Append('}');
            return sb.ToString();
        }
    }
}
