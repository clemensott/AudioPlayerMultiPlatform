using System;
using System.Collections.Generic;

namespace AudioPlayerBackend.AudioLibrary
{
    internal class RepoService<T>
    {
        private readonly IList<T> repos;

        public void ForEachRepoExcept(Action<T> action, T exceptRepo)
        {
            foreach (T repo in repos)
            {
                if (!Equals(repo, exceptRepo)) action(repo);
            }
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
