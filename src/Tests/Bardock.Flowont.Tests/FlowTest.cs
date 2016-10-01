using Xunit;

namespace Bardock.Flowont.Tests
{
    public class FlowTest
    {
        private static readonly TicketFlow Flow = (TicketFlow)TicketFlowDefinitions.Instance.GetDefaultFlow();

        [Fact]
        public void HappyPathActions()
        {
            var actual = Flow.HappyPathActions;

            var expected = new[] { TicketActions.Approve, TicketActions.Stay, TicketActions.Close, TicketActions.Archive };
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void HappyPathStatus()
        {
            var actual = Flow.HappyPathStatus;

            var expected = new[] { TicketNodes.Pending, TicketNodes.Approved, TicketNodes.Closed, TicketNodes.Archived };
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SortedNodes()
        {
            var actual = Flow.SortedNodes;

            var expected = new[] { TicketNodes.Pending, TicketNodes.Approved, TicketNodes.Closed, TicketNodes.Archived, TicketNodes.Rejected };
            Assert.Equal(expected, actual);
        }
    }
}