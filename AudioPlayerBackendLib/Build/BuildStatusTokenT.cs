using System;
using System.Threading;
using System.Threading.Tasks;
using StdOttStandard;
using StdOttStandard.Equal;

namespace AudioPlayerBackend.Build
{
    public class BuildStatusToken<TResult> : BuildStatusToken
    {
        private TResult result;
        private Exception exception;

        public TResult Result
        {
            get => result;
            private set
            {
                if (Equals(value, result)) return;

                result = value;
                OnPropertyChanged(nameof(Result));
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

        public Task<TResult> ResultTask { get; private set; }

        public BuildStatusToken()
        {
            Reset();
        }

        public override void Reset()
        {
            base.Reset();

            if (ResultTask == null || IsEnded.HasValue) ResultTask = GetResult();
        }

        private async Task<TResult> GetResult()
        {
            await EndTask;

            return Result;
        }

        public void Cancel()
        {
            End(BuildEndedType.Canceled, default(TResult));
        }

        public void Settings()
        {
            End(BuildEndedType.Settings, default(TResult));
        }

        public void Successful(TResult result)
        {
            End(BuildEndedType.Successful, result);
        }

        public void End(BuildEndedType type, TResult result)
        {
            if (IsEnded.HasValue) return;

            IsEnded = type;
            Result = result;

            RaiseEnded(type);

            lock (lockObj) Monitor.PulseAll(lockObj);
        }
    }
}
