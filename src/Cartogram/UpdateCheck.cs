using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;
using OfficeOpenXml.ConditionalFormatting;

namespace Cartogram
{
    public static class UpdateCheck
    {

        static UpdateCheck()
        {
            
        }

        /// <summary>
        /// Async query to Github to get the most recent release for Cartogram
        /// </summary>
        /// <returns>The version found on Github</returns>
        internal async static Task<Version> UpdateAvaliable()
        {
            var github = new GitHubClient(new ProductHeaderValue("Cartogram"));
            var releases = await github.Release.GetAll("M1nistry", "Cartogram");

            if (releases.Count <= 0) return new Version(0,0,0,0);
            var releaseTag = releases[0].TagName;
            if (!AcceptableVersion(releaseTag)) return new Version(0,0,0,0);
            var githubVersion = new Version(releaseTag);
            return githubVersion;
        }

        private static bool AcceptableVersion(string str)
        {
            return str.All(c => (c < '0' || c > '9') || c != '.');
        }
    }
}
