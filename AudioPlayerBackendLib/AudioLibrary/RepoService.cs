using System.Collections.Generic;
using System.Linq;

namespace AudioPlayerBackend.AudioLibrary
{
    internal class RepoService<T>
    {
        private readonly IList<T> repos = new List<T>();

        public IEnumerable<T> GetRepos()
        {
            return repos.AsEnumerable();
        }

        public void AddRepo(T repo)
        {
            repos.Add(repo);
        }

        public void RemoveRepo(T repo)
        {
            repos.Remove(repo);
        }
    }
}
