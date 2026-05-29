using System.Net;

namespace ZyncMaster.Server;

public static class PairApprovalEndpoints
{
    public static void MapPairApprovalEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Browser-facing approval page. Session-gated on a connected Microsoft account:
        // without one we cannot sync anything, so send the user through /connect first and
        // return them here afterwards. This page is NOT api-key protected — it is reached
        // from a human's browser, the api key lives only on the paired device.
        app.MapGet("/pair", async (HttpContext context, IDeviceStore devices, IConnectedAccountStore accounts) =>
        {
            var code = context.Request.Query["code"].ToString();

            if (!await accounts.HasAnyAsync(context.RequestAborted))
            {
                var returnTo = "/pair" + (string.IsNullOrEmpty(code)
                    ? ""
                    : "?code=" + Uri.EscapeDataString(code));
                return Results.Redirect("/connect?returnTo=" + Uri.EscapeDataString(returnTo));
            }

            if (string.IsNullOrWhiteSpace(code))
                return Results.Content(Page("Pair a device", "<p>No pairing code supplied.</p>"), "text/html");

            var pending = await devices.GetPendingByCodeAsync(code, context.RequestAborted);
            if (pending is null)
                return Results.Content(Page("Pair a device",
                    "<p>That pairing code is not valid or has expired.</p>"), "text/html");

            if (pending.Approved)
                return Results.Content(Page("Device approved",
                    $"<p><strong>{Encode(pending.DeviceName)}</strong> is already approved.</p>"), "text/html");

            // The existing approve endpoint consumes JSON, so the button POSTs the code as
            // a JSON body via fetch rather than a classic urlencoded form submit.
            var body =
                $"<p>Approve <strong>{Encode(pending.DeviceName)}</strong> to sync with this account?</p>" +
                $"<p class=\"code\">{Encode(pending.Code)}</p>" +
                $"<button id=\"approve\" type=\"button\" data-code=\"{Encode(pending.Code)}\">Approve</button>" +
                "<p id=\"result\"></p>" +
                "<script>document.getElementById('approve').addEventListener('click',async function(){" +
                "var c=this.getAttribute('data-code');" +
                "var r=await fetch('/api/devices/approve',{method:'POST'," +
                "headers:{'Content-Type':'application/json'},body:JSON.stringify({code:c})});" +
                "document.getElementById('result').textContent=r.ok?'Approved.':'Approval failed.';" +
                "});</script>";

            return Results.Content(Page("Pair a device", body), "text/html");
        });
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string Page(string title, string body) =>
        "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\" />" +
        $"<title>{Encode(title)}</title></head><body>" +
        $"<h1>{Encode(title)}</h1>{body}</body></html>";
}
