namespace Bardock.Flowont.Entities
{
    /// <summary>
    /// Entity that transits through a flow.
    /// These entities could have different flow versions.
    /// </summary>
    public interface IMultiFlowEntity<TFlowID> : IFlowEntity
    {
        TFlowID FlowID { get; set; }
    }
}