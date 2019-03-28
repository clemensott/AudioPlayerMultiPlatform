using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using StdOttStandard;

namespace AudioPlayerBackend
{
    public enum BuildEndedType { Successful, Canceled, Settings }

    public class BuildStatusToken : INotifyPropertyChanged
    {
        private readonly object lockObj = new object();
        private BuildEndedType? ended;
        private Exception exception;

        public event EventHandler<BuildEndedType> Ended;

        public BuildEndedType? IsEnded
        {
            get => ended;
            private set
            {
                if (value == ended) return;

                ended = value;
                Ended?.Invoke(this, IsEnded ?? BuildEndedType.Successful);

                OnPropertyChanged(nameof(IsEnded));

                lock (lockObj) Monitor.Pulse(lockObj);
            }
        }

        public Exception Exception
        {
            get => exception;
            set
            {
                if (value == exception) return;

                exception = value;
                OnPropertyChanged(nameof(Exception));
            }
        }

        public Task<BuildEndedType> Task { get; }

        public BuildStatusToken()
        {
            Task = Utils.WaitAsync(lockObj).ContinueWith(t => IsEnded ?? BuildEndedType.Successful);
        }

        public void End(BuildEndedType type)
        {
            IsEnded = type;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
