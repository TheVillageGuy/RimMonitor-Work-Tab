using HarmonyLib;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Verse;

namespace RimMonitorWorkTab.RimMonitor
{
    /// <summary>
    /// Optional integration bridge to RimMonitor.
    /// - No compile-time dependency on RimMonitor.
    /// - Uses reflection to bind RimMonitor's public APIs if present.
    /// - ALL RimMonitor interaction in this mod goes through this class.
    ///
    /// This file is intentionally explicit:
    /// it documents the *contract* between this mod and RimMonitor,
    /// not just the mechanics.
    /// </summary>
    internal static class RimMonitorAPI
    {
        // Must match the namespace/type in RimMonitorWebHostApi.cs
        private const string ApiTypeName = "RimMonitor.WebUI.RimMonitorWebHostApi";

        // external web handler registry (generic, no mod knowledge)
        // Must match the type added to RimMonitor.
        private const string ExternalWebHandlersTypeName =
            "RimMonitor.Web.ExternalWebHandlers";

        private static readonly bool _available;

        private static readonly Action<Action> _enqueueOnMainThread;
        private static readonly MethodInfo _getPawnActivitySnapshotMI;

        private static readonly Action<string, string, string, string > _registerWebsiteButton;

        // Upgraded incident API support
        private static readonly Action<string, string, string, string> _registerIncidentButton;
        private static readonly Action<string, string, string, string> _registerExternalIncident;

        // =========================================================
        // external web handler support (reflection-bound)
        // =========================================================

        private static readonly MethodInfo _registerExternalWebHandlerMI;
        private static readonly Type _externalWebHandlerDelegateType;

        static RimMonitorAPI()
        {
            try
            {
                // -------------------------------------------------
                // Core RimMonitor API
                // -------------------------------------------------

                Type apiType = AccessTools.TypeByName(ApiTypeName);
                if (apiType == null)
                    return;

                // EnqueueOnMainThread(Action action)
                MethodInfo enqueueMI =
                    AccessTools.Method(apiType, "EnqueueOnMainThread", new[] { typeof(Action) });

                if (enqueueMI != null)
                {
                    _enqueueOnMainThread =
                        (Action<Action>)enqueueMI.CreateDelegate(typeof(Action<Action>));
                }

                // GetPawnActivitySnapshot()
                // Kept opaque on purpose: we do NOT depend on RimMonitor DTOs.
                _getPawnActivitySnapshotMI =
                    AccessTools.Method(apiType, "GetPawnActivitySnapshot", Type.EmptyTypes);

                // RegisterWebsiteButton(string id, string label, string imageUrl, string href, bool newTab)
                MethodInfo regWebBtnMI = AccessTools.Method(
                    apiType,
                    "RegisterWebsiteButton",
                    new[] { typeof(string), typeof(string), typeof(string), typeof(string) }
                );

private static readonly Action<string, string, string, string> _registerWebsiteButton;

        // RegisterIncidentButton(string id, string label, string imageUrl, string triggerPath)
        MethodInfo regIncidentBtnMI = AccessTools.Method(
                    apiType,
                    "RegisterIncidentButton",
                    new[] { typeof(string), typeof(string), typeof(string), typeof(string) }
                );

                if (regIncidentBtnMI != null)
                {
                    _registerIncidentButton =
                        (Action<string, string, string, string>)
                        regIncidentBtnMI.CreateDelegate(
                            typeof(Action<string, string, string, string>)
                        );
                }

                // RegisterExternalIncident(string id, string label, string imageUrl, string triggerPath)
                MethodInfo regExternalIncidentMI = AccessTools.Method(
                    apiType,
                    "RegisterExternalIncident",
                    new[] { typeof(string), typeof(string), typeof(string), typeof(string) }
                );

                if (regExternalIncidentMI != null)
                {
                    _registerExternalIncident =
                        (Action<string, string, string, string>)
                        regExternalIncidentMI.CreateDelegate(
                            typeof(Action<string, string, string, string>)
                        );
                }

                // -------------------------------------------------
                // external web hosting integration
                // -------------------------------------------------
                // This allows a standalone mod to register HTTP routes
                // with RimMonitor's *single* web server, without RimMonitor
                // knowing anything about the mod.

                Assembly rimMonitorAsm = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "RimMonitorAssembly");

                if (rimMonitorAsm != null)
                {
                    Type externalWebHandlersType =
                        rimMonitorAsm.GetType("RimMonitor.Web.ExternalWebHandlers");

                    if (externalWebHandlersType != null)
                    {
                        _registerExternalWebHandlerMI =
                            externalWebHandlersType.GetMethod(
                                "Register",
                                BindingFlags.Public | BindingFlags.Static);

                        if (_registerExternalWebHandlerMI != null)
                        {
                            _externalWebHandlerDelegateType =
                                _registerExternalWebHandlerMI
                                    .GetParameters()[0]
                                    .ParameterType;
                        }
                    }
                }


                // "Available" means: at least main-thread enqueue + website button registration.
                _available = _enqueueOnMainThread != null && _registerWebsiteButton != null;
            }
            catch (Exception ex)
            {
                Log.Warning("[RimMonitorWorkTab] RimMonitorAPI init failed: " + ex);
            }
        }

        // =========================================================
        // Capability flags
        // =========================================================

        public static bool Available => _available;

        public static bool SupportsIncidentButtons => _registerIncidentButton != null;

        public static bool SupportsExternalIncidentRegistration => _registerExternalIncident != null;

        public static bool SupportsExternalWebHandlers => _registerExternalWebHandlerMI != null;

        // =========================================================
        // API surface
        // =========================================================

        public static void EnqueueOnMainThread(Action action)
        {
            if (action == null)
                return;

            _enqueueOnMainThread?.Invoke(action);
        }

        public static object GetPawnActivitySnapshotOpaque()
        {
            if (_getPawnActivitySnapshotMI == null)
                return null;

            try
            {
                return _getPawnActivitySnapshotMI.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Log.Warning("[RimMonitorWorkTab] GetPawnActivitySnapshotOpaque failed: " + ex);
                return null;
            }
        }

        public static void RegisterWebsiteButton(
            string id,
            string label,
            string imageUrl,
            string href
            )
        {
            if (string.IsNullOrEmpty(id))
                return;

            _registerWebsiteButton?.Invoke(id, label, imageUrl, href);
        }

        public static void RegisterIncidentButton(
            string id,
            string label,
            string imageUrl,
            string triggerPath)
        {
            if (string.IsNullOrEmpty(id))
                return;

            _registerIncidentButton?.Invoke(id, label, imageUrl, triggerPath);
        }

        public static void RegisterExternalIncident(
            string id,
            string label,
            string imageUrl,
            string triggerPath)
        {
            if (string.IsNullOrEmpty(id))
                return;

            _registerExternalIncident?.Invoke(id, label, imageUrl, triggerPath);
        }

        // =========================================================
        // external web handler registration
        // =========================================================

        /// <summary>
        /// Registers an HTTP handler with RimMonitor's web server.
        ///
        /// This is how standalone mods expose pages under the same
        /// port and HttpListener without RimMonitor knowing about them.
        ///
        /// Expected handler signature:
        ///   bool Handle(HttpListenerContext context, string path)
        /// </summary>
        public static void RegisterExternalWebHandler(Func<HttpListenerContext, string, bool> handler)
        {
            Verse.Log.Message("[WorkTab] RegisterExternalWebHandler called");

            if (handler == null)
            {
                Verse.Log.Warning("[WorkTab] Handler is null");
                return;
            }

            if (_registerExternalWebHandlerMI == null)
            {
                Verse.Log.Warning("[WorkTab] RimMonitor does AGAIN NOT expose ExternalWebHandlers");
                return;
            }

            try
            {
                Verse.Log.Message("[WorkTab] Creating delegate for external web handler");

                Delegate d =
                    Delegate.CreateDelegate(
                        _externalWebHandlerDelegateType,
                        handler.Target,
                        handler.Method);

                Verse.Log.Message("[WorkTab] Invoking RimMonitor ExternalWebHandlers.Register");

                _registerExternalWebHandlerMI.Invoke(null, new object[] { d });

                Verse.Log.Message("[WorkTab] External web handler registered with RimMonitor");
            }
            catch (Exception ex)
            {
                Verse.Log.Error(
                    "[WorkTab] RegisterExternalWebHandler failed:\n" + ex);
            }
        }
    }
}
