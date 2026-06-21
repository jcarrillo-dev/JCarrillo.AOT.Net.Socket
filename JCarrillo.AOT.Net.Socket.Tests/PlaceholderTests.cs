using Xunit;
using FluentAssertions;

namespace JCarrillo.AOT.Net.Socket.Tests
{
    public class PlaceholderTests
    {
        [Fact]
        public void SanityCheck_ShouldSucceed()
        {
            true.Should().BeTrue();
        }
    }
}
