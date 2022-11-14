using System;
using UnityEditor.PackageManager.UI;

namespace Cinemachine.Editor
{
    /// <summary>
    /// A configuration class defining information related to samples for the package.
    /// </summary>
    /// <remarks>
    /// This is an extension on the built-in information related to samples.
    /// Information related to common dependencies and doc links can be defined in this class.
    /// </remarks>
    class SampleConfiguration
    {
        /// <summary>
        /// This class defines the path and dependencies for a specific sample.
        /// </summary>
        internal class SampleEntry
        {
            public string Path { get; set; }
            public string[] AssetDependencies { get; set; }
            public string[] PackageDependencies { get; set; }
        }

        /// <summary>
        /// The url to the documentation page for this package's samples.
        /// </summary>
        public string DocumentationURL { get; set; }

        /// <summary>
        /// Paths to the shared assets which should be imported alongside the sample's own assets.
        /// </summary>
        public string[] CommonAssetDependencies { get; set; }

        /// <summary>
        /// The list of specific sample entries for shared asset paths.
        /// </summary>
        public SampleEntry[] SampleEntries { get; set; }

        internal SampleEntry GetEntry(Sample sample)
        {
            if (SampleEntries == null)
                return null;

            for (int i = 0; i < SampleEntries.Length; ++i)
            {
                if (sample.resolvedPath.EndsWith(SampleEntries[i].Path))
                    return SampleEntries[i];
            }

            return null;
        }
    }
}
