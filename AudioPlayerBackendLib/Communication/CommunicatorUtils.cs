using System.Threading.Tasks;

namespace AudioPlayerBackend.Communication
{
    public static class CommunicatorUtils
    {
        public static Task PreviousSong(this ICommunicator communicator)
        {
            return communicator.SendCommand("previous");
        }

        public static Task PlaySong(this ICommunicator communicator)
        {
            return communicator.SendCommand("play");
        }

        public static Task PauseSong(this ICommunicator communicator)
        {
            return communicator.SendCommand("pause");
        }

        public static Task NextSong(this ICommunicator communicator)
        {
            return communicator.SendCommand("Next");
        }
    }
}
