using HarmonyLib;
using RimMonitorWorkTab.ColonistPriority.RunTime;
using RimMonitorWorkTab.Core;
using RimMonitorWorkTab.RimMonitor;
using RimWorld;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using Verse;


namespace RimMonitorWorkTab.Web
{
    internal static class WorkTabRouter
    {

        private static readonly MethodInfo EnableAndInitializeMethod = AccessTools.Method(typeof(Pawn_WorkSettings), "EnableAndInitialize");
        public static bool Handle(HttpListenerContext ctx, string path)
        {
            if (path == null)
                return false;

            // Main page
            if (path == "/mod/worktab")
            {
                WriteText(ctx, WorkTabPage.BuildHtml(), "text/html");
                return true;
            }



            // State snapshot (JSON)
            if (path == "/mod/worktab/state")
            {
                WriteText(ctx, WorkTabPage.BuildStateJson(), "application/json");
                return true;
            }

            if (path == "/mod/worktab/revision")
            {
                WriteText(ctx, WorkTabPage.BuildRevisionJson(), "application/json");
                return true;
            }

            // Set priority (POST)
            if (path == "/mod/worktab/setpriority" && ctx.Request.HttpMethod == "POST")
            {
                HandleSetPriority(ctx);
                return true;
            }

            // Passion icons (embedded PNGs)
            if (path == "/mod/worktab/assets/passion_minor.png")
            {
                WriteBinary(
                    ctx,
                    ReadEmbeddedBinary("RimMonitorWorkTab.Web.Assets.passion_minor.png"),
                    "image/png"
                );
                return true;
            }

            if (path == "/mod/worktab/assets/passion_major.png")
            {
                WriteBinary(
                    ctx,
                    ReadEmbeddedBinary("RimMonitorWorkTab.Web.Assets.passion_major.png"),
                    "image/png"
                );
                return true;
            }

            return false;
        }

        private static void HandleSetPriority(HttpListenerContext ctx)
        {
            try
            {
                Log.Warning("[WorkTab] setPriority request received.");
                string body;
                using (var reader = new StreamReader(ctx.Request.InputStream))
                    body = reader.ReadToEnd();

                int mapId = ReadJsonInt(body, "mapId");
                int pawnId = ReadJsonInt(body, "pawnId");
                string workTypeId = ReadJsonString(body, "workType");
                int priority = ReadJsonInt(body, "priority");

                RimMonitorAPI.EnqueueOnMainThread(() =>
                {
                    try
                    {
                        ApplyPriority(mapId, pawnId, workTypeId, priority);
                        WorkTabGameComponent.WakeWorker();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[WorkTab] Failed to apply priority: " + ex);
                    }
                });


                // SUCCESS RESPONSE
                WriteText(ctx, "", "text/plain");
            }
            catch (Exception ex)
            {
                Log.Error("[WorkTab] setPriority failed: " + ex);

                ctx.Response.StatusCode = 400;
                WriteText(ctx, "invalid request", "text/plain");
            }
        }

        private static void ApplyPriority(int mapId, int pawnId, string workTypeId, int priority)
        {
            if (string.IsNullOrEmpty(workTypeId))
                return;

            Map targetMap = null;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m != null && m.uniqueID == mapId)
                {
                    targetMap = m;
                    break;
                }
            }

            if (targetMap == null)
                return;

            Pawn pawn = null;
            try
            {
                var list = targetMap.mapPawns != null ? targetMap.mapPawns.FreeColonistsSpawned : null;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var p = list[i];
                        if (p != null && p.thingIDNumber == pawnId)
                        {
                            pawn = p;
                            break;
                        }
                    }
                }
            }
            catch { }

            if (pawn == null || pawn.workSettings == null)
                return;

            Log.Warning("[WorkTab] 2 Applying priority " + priority + " for pawn " + pawn.Name + " workType " + workTypeId);
            WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeId);
            if (workType == null)
                return;

            // Apply priority (0 disables, >0 enables)
            pawn.workSettings.SetPriority(workType, priority);


        }

        private static int ReadJsonInt(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return 0;

            var m = System.Text.RegularExpressions.Regex.Match(
                json,
                "\\\"" + System.Text.RegularExpressions.Regex.Escape(key) + "\\\"\\s*:\\s*(-?\\d+)",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant
            );

            if (!m.Success)
                return 0;

            int v;
            if (int.TryParse(m.Groups[1].Value, out v))
                return v;

            return 0;
        }

        private static string ReadJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return "";

            var m = System.Text.RegularExpressions.Regex.Match(
                json,
                "\\\"" + System.Text.RegularExpressions.Regex.Escape(key) + "\\\"\\s*:\\s*\\\"([^\\\"]*)\\\"",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant
            );

            if (!m.Success)
                return "";

            return m.Groups[1].Value;
        }

        private static void WriteText(HttpListenerContext ctx, string text, string contentType)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text ?? "");
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }

        private static void WriteBinary(HttpListenerContext ctx, byte[] bytes, string contentType)
        {
            bytes = bytes ?? new byte[0];
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }

        private static byte[] ReadEmbeddedBinary(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(resourceName))
            {
                if (s == null)
                    return new byte[0];

                using (var ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
