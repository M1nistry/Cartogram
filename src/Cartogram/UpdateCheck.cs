using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;

namespace Cartogram
{
    public static class UpdateCheck
    {

        static UpdateCheck()
        {
            
        }

        internal async static Task<bool> UpdateAvaliable()
        {
            var github = new GitHubClient(new ProductHeaderValue("Cartogram"));

            var releases = await github.Release.GetAll("M1nistry", "Cartogram");

            var assem = Assembly.GetEntryAssembly();
            var assemName = assem.GetName();
            var ver = assemName.Version;
            var releaseTag = releases[0].TagName;
            if (!AcceptableVersion(releaseTag)) return false;
            var githubVersion = new Version(releaseTag);
            return githubVersion > ver;
        }

        private static bool AcceptableVersion(string str)
        {
            return str.All(c => (c < '0' || c > '9') || c != '.');
        }
    }
}
