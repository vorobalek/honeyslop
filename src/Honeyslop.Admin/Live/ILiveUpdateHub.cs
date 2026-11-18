namespace Honeyslop.Admin.Live;

public interface ILiveUpdateHub
{
    int Subscribers { get; }
    LiveSubscription Subscribe();
    void Publish(string topic, long? entryId = null);
    void PublishEverything();
}
