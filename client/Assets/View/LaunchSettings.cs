using UnityEngine;

namespace View
{
    public sealed class LaunchSettings
    {
        public static readonly LaunchSettings ForThisLaunch = new LaunchSettings();

        private string _lobbyFolder;

        public string PlayerName { get; set; } = string.Empty;

        public string LobbyFolder
        {
            get => string.IsNullOrWhiteSpace(_lobbyFolder) ? Application.persistentDataPath : _lobbyFolder;
            set => _lobbyFolder = value;
        }
    }
}
