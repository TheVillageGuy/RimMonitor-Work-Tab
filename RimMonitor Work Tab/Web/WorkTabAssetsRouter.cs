using System.IO;
using System.Net;
using System.Reflection;

internal static class WorkTabAssetsRouter
{
    public static bool Handle(HttpListenerContext ctx, string path)
    {
        // Accept both old and new asset paths
        if (!path.StartsWith("/mod/worktab/"))
            return false;

        string fileName;

        if (path.StartsWith("/mod/worktab/assets/"))
            fileName = path.Substring("/mod/worktab/assets/".Length);
        else
            fileName = path.Substring("/mod/worktab/".Length);

        string resourceName =
            "RimMonitorWorkTab.Web.WorkTab.WebAssets." + fileName;

        Assembly asm = Assembly.GetExecutingAssembly();
        Stream stream = asm.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
            return true;
        }

        // Content type by extension
        if (fileName.EndsWith(".css"))
            ctx.Response.ContentType = "text/css";
        else if (fileName.EndsWith(".js"))
            ctx.Response.ContentType = "application/javascript";
        else if (fileName.EndsWith(".png"))
            ctx.Response.ContentType = "image/png";
        else
            ctx.Response.ContentType = "application/octet-stream";

        stream.CopyTo(ctx.Response.OutputStream);
        ctx.Response.OutputStream.Close();
        ctx.Response.Close();
        return true;
    }
}
