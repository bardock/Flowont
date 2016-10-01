using System;

namespace Bardock.Flowont.Entities
{
    public class PathLog<TNodeID, TActionID>
        where TNodeID : struct, IConvertible
        where TActionID : struct, IConvertible
    {
        public TActionID? ActionID { get; set; }

        public TNodeID? FromNodeID { get; set; }

        public TNodeID ToNodeID { get; set; }
    }
}