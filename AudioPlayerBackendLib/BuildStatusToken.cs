using System;
using System.ComponentModel;
using System.Threading.Tasks;
using StdOttStandard;

namespace AudioPlayerBackend
{
    public enum BuildEndedType { Successful, Canceled, Settings }

    public class BuildStatusToken : INotifyPropertyChanged
    {
        protected readonly object lockObj = new object();
        private BuildEndedType? ended;

        public event EventHandler<BuildEndedType> Ended;

        public BuildEndedType? IsEnded
        {
            get => ended;
            protected set
            {
                if (value == ended) return;

                ended = value;

                OnPropertyChanged(nameof(IsEnded));
            }
        }

        public Task<BuildEndedType> EndTask { get; }

        public BuildStatusToken()
        {
            EndTask = Utils.WaitAsync(lockObj).ContinueWith(t => IsEnded ?? BuildEndedType.Successful);
        }

        protected void RaiseEnded(BuildEndedType type)
        {
            Ended?.Invoke(this, type);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
