using System.Diagnostics;

namespace unison
{
    public class SnapcastHandler
    {
        private readonly Process _snapcast = new();
        public bool Started { get; private set; }
        private string _snapcastVersion = "snapclient_0.25.0-1_win64";

        public SnapcastHandler()
        {
        }

        public void Start(string host)
        {
            if (!Started)
            {
                _snapcast.StartInfo.FileName = _snapcastVersion + @"\snapclient.exe";
                _snapcast.StartInfo.Arguments = "--host " + host;
                _snapcast.StartInfo.CreateNoWindow = true;
                _snapcast.Start();
                Started = true;
            }
            else
            {
                _snapcast.Kill();
                Started = false;
            }
        }

        public void Stop()
        {
            if (Started)
            {
                _snapcast.Kill();
                Started = false;
            }
        }
    }
}