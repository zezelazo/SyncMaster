namespace SyncMaster.Server;

public interface ISecretProvider
{
    string GetMicrosoftClientSecret();
}
