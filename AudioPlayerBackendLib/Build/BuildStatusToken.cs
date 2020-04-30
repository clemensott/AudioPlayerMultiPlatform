using System;
using System.ComponentModel;
using System.Threading.Tasks;
using StdOttStandard;

namespace AudioPlayerBackend.Build
{
    public enum BuildEndedType { Successful, Canceled, Settings }

    public class BuildStatusToken : INotifyPropertyChanged
    {
        protected object lockObj;
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

        public Task<BuildEndedType> EndTask { get; private set; }

        public BuildStatusToken()
        {
            Reset();
        }

        public virtual void Reset()
        {
            if (EndTask != null && !IsEnded.HasValue) return;

            IsEnded = null;
            EndTask = GetIsEnded();
        }

        private async Task<BuildEndedType> GetIsEnded()
        {
            lockObj = new object();
            await StdUtils.WaitAsync(lockObj);

            return IsEnded ?? BuildEndedType.Successful;
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
