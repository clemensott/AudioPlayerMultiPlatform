namespace AudioPlayerBackend.FileSystem
{
    public struct LocalKnownFolder
    {
        public string Name { get; }

        public string Value { get; }

        public string CurrentFullPath { get; }

        public LocalKnownFolder(string name, string value, string currentFullPath)
        {
            Name = name;
            Value = value;
            CurrentFullPath = currentFullPath;
        }
    }
}
