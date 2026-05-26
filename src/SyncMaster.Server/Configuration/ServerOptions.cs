namespace SyncMaster.Server;

public sealed class ServerOptions
{
    public string MicrosoftClientId { get; set; } = "";
    public string Authority { get; set; } = "https://login.microsoftonline.com/common/oauth2/v2.0";
    public string RedirectUri { get; set; } = "";
    public string Scopes { get; set; } = "offline_access Calendars.ReadWrite User.Read";
    public int SyncWindowDays { get; set; } = 14;
}
