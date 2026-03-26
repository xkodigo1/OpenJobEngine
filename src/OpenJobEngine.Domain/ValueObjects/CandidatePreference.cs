using OpenJobEngine.Domain.Enums;

namespace OpenJobEngine.Domain.ValueObjects;

public sealed class CandidatePreference
{
    public WorkMode PreferredPrimaryWorkMode { get; private set; } = WorkMode.Unknown;

    public bool AcceptRemote { get; private set; } = true;

    public bool AcceptHybrid { get; private set; } = true;

    public bool AcceptOnSite { get; private set; } = true;

    public static CandidatePreference Default() => new();

    public void Update(WorkMode preferredPrimaryWorkMode, bool acceptRemote, bool acceptHybrid, bool acceptOnSite)
    {
        PreferredPrimaryWorkMode = preferredPrimaryWorkMode;
        AcceptRemote = acceptRemote;
        AcceptHybrid = acceptHybrid;
        AcceptOnSite = acceptOnSite;
    }
}
