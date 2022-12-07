using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Our tests are found in separate packages and our pipeline uses that.
    /// Package validation however requests at least one test in the package itself.
    /// </summary>
    public class RuntimePlaceholder
    {
        [Test]
        public void CanaryChirp()
        {
            Assert.Pass("Chirp!");
        }
    }
}
