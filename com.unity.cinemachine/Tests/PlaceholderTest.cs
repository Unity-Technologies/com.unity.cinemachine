using NUnit.Framework;
namespace Tests
{
    /// <summary>
    /// Placeholder test for testing pipeline. Our tests are found in separate packages and our pipeline uses that.
    /// Package validation however requests us to have at least one test in the package itself.
    /// </summary>
    [TestFixture]
    public class PlaceholderTest
    {
        [Test]
        public void CanaryChirp()
        {
            Assert.True(true);
        }
    }
}
