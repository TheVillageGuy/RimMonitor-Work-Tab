using RimMonitorWorkTab.ColonistPriority.RunTime.Enrichment;
using RimMonitorWorkTab.ColonistPriority.RunTime.State;
using RimMonitorWorkTab.RimMonitor;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace RimMonitorWorkTab.ColonistPriority.RunTime
{
    internal static class WorkTabPage
    {
        private static volatile WorkTabWorldState _latestState;

        // Monotonic revision counter for client-side sync
        private static int _revisionCounter = 0;

        public static void PublishStateFromWorker(WorkTabWorldState state)
        {
            _latestState = state;
            _revisionCounter++;
        }

        public static string BuildRevisionJson()
        {
            return "{\"revision\":" + _revisionCounter + "}";
        }

        public static string BuildHtml()
        {
            WorkTabWorldState state = _latestState;

            // Display-only enrichment using RimMonitor data.
            if (state != null)
                WorkTabRimMonitorEnricher.Enrich(state);

            // Static DOM structure
            string template = ReadEmbeddedText(
                "RimMonitorWorkTab.Web.WorkTab.worktab.html"
            );

            // Executable UI logic (embedded inline by design)
            string jsSkills = ReadEmbeddedText("RimMonitorWorkTab.Web.WorkTab.worktab.skills.js");            
            string jsPriority = ReadEmbeddedText("RimMonitorWorkTab.Web.WorkTab.worktab.priority.js");
            string jsRender = ReadEmbeddedText("RimMonitorWorkTab.Web.WorkTab.worktab.render.js");
            string jsInteraction = ReadEmbeddedText("RimMonitorWorkTab.Web.WorkTab.worktab.interaction.js");

            string cssBase = ReadEmbeddedText("RimMonitorWorkTab.Web.WorkTab.worktab.base.css");
            string cssSkills = ReadEmbeddedText("RimMonitorWorkTab.Web.WorkTab.worktab.skills.css");

            string stateJson = state != null ? SerializeWorldState(state) : "{}";

            var sb = new StringBuilder(32 * 1024);

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<title>RimMonitor · Work Tab</title>");

            sb.AppendLine("<style>");
            sb.AppendLine(cssBase);
            sb.AppendLine();
            sb.AppendLine(cssSkills);
            sb.AppendLine("</style>");

            sb.Append("<script>");
            sb.Append("window.__WORKTAB_STATE__=");
            sb.Append(stateJson);
            sb.AppendLine(";");
            sb.AppendLine("</script>");

            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine(template);

            sb.AppendLine("<script>");
            sb.AppendLine(jsSkills);
            sb.AppendLine("</script>");

            sb.AppendLine("<script>");
            sb.AppendLine(jsPriority);
            sb.AppendLine("</script>");

            sb.AppendLine("<script>");
            sb.AppendLine(jsRender);
            sb.AppendLine("</script>");

            sb.AppendLine("<script>");
            sb.AppendLine(jsInteraction);
            sb.AppendLine("</script>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Builds the latest WorkTab world state JSON snapshot.
        /// Used by the /mod/worktab/state endpoint for live refresh.
        /// </summary>
        public static string BuildStateJson()
        {
            WorkTabWorldState state = _latestState;

            if (state == null)
                return "{}";

            // Display-only enrichment using RimMonitor data.
            WorkTabRimMonitorEnricher.Enrich(state);

            return SerializeWorldState(state);
        }

        private static string SerializeWorldState(WorkTabWorldState state)
        {
            var sb = new StringBuilder(8192);
            sb.Append("{");

            sb.Append("\"revision\":").Append(state.Revision).Append(",");

            sb.Append("\"manualPrioritiesEnabled\":");
            sb.Append(state.ManualPrioritiesEnabled ? "true" : "false");
            sb.Append(",");

            sb.Append("\"workTypes\":[");
            for (int i = 0; i < state.WorkTypes.Count; i++)
            {
                if (i > 0) sb.Append(",");
                WriteWorkType(sb, state.WorkTypes[i]);
            }
            sb.Append("],");

            sb.Append("\"maps\":[");
            for (int m = 0; m < state.Maps.Count; m++)
            {
                if (m > 0) sb.Append(",");
                WriteMap(sb, state.Maps[m]);
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static void WriteWorkType(StringBuilder sb, WorkTabWorkType wt)
        {
            sb.Append("{");
            sb.Append("\"id\":\"").Append(Escape(wt.Id)).Append("\",");
            sb.Append("\"label\":\"").Append(Escape(wt.Label)).Append("\",");
            sb.Append("\"shortLabel\":\"").Append(Escape(wt.ShortLabel)).Append("\"");
            sb.Append("}");
        }

        private static void WriteMap(StringBuilder sb, WorkTabMapState map)
        {
            sb.Append("{");
            sb.Append("\"mapId\":").Append(map.MapId).Append(",");
            sb.Append("\"mapLabel\":\"").Append(Escape(map.MapLabel)).Append("\",");

            sb.Append("\"pawns\":[");
            for (int i = 0; i < map.Pawns.Count; i++)
            {
                if (i > 0) sb.Append(",");
                WritePawn(sb, map.Pawns[i]);
            }
            sb.Append("]");

            sb.Append("}");
        }

        private static void WritePawn(StringBuilder sb, WorkTabPawnState pawn)
        {
            sb.Append("{");
            sb.Append("\"pawnThingId\":").Append(pawn.PawnThingId).Append(",");
            sb.Append("\"name\":\"").Append(Escape(pawn.Name)).Append("\",");

            sb.Append("\"cellsByWorkType\":{");
            bool first = true;
            foreach (var kv in pawn.CellsByWorkType)
            {
                if (!first) sb.Append(",");
                first = false;

                sb.Append("\"").Append(Escape(kv.Key)).Append("\":");
                WriteCell(sb, kv.Value);
            }
            sb.Append("}");

            sb.Append("}");
        }

        private static void WriteCell(StringBuilder sb, WorkTabCellState cell)
        {
            sb.Append("{");

            // NEW: explicit availability flag
            sb.Append("\"available\":").Append(cell.Available ? "true" : "false").Append(",");

            sb.Append("\"priority\":").Append(cell.Priority).Append(",");

            if (cell.SkillLevel.HasValue)
                sb.Append("\"skillLevel\":").Append(cell.SkillLevel.Value).Append(",");

            if (cell.Passion.HasValue)
                sb.Append("\"passion\":").Append(cell.Passion.Value).Append(",");

            sb.Append("\"ideologyOpposed\":").Append(cell.IdeologyOpposed ? "true" : "false").Append(",");

            sb.Append("\"activityScore\":")
              .Append(cell.ActivityScore.ToString(System.Globalization.CultureInfo.InvariantCulture))
              .Append(",");

            sb.Append("\"recentlyActive\":")
              .Append(cell.RecentlyActive ? "true" : "false");

            sb.Append("}");
        }


        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ReadEmbeddedText(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(resourceName))
            {
                if (s == null)
                    return "";

                using (var r = new StreamReader(s, Encoding.UTF8))
                    return r.ReadToEnd();
            }
        }
    }
}
