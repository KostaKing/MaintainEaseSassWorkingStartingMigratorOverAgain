namespace MaintainEase.Core.Domain.Enums
{
    /// <summary>
    /// Represents the status of a lease
    /// </summary>
    public enum LeaseStatus
    {
        Draft = 0,
        Active = 1,
        Expired = 2,
        Terminated = 3,
        Renewed = 4,
        InDefault = 5
    }
}
