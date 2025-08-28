using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace XRMultiplayer
{
    /// <summary>
    /// Controls the music playback in a room.
    /// </summary>
    public class RoomMusic : NetworkBehaviour
    {
        /// <summary>
        /// Indicates whether the music should start playing automatically.
        /// </summary>
        [SerializeField] bool m_AutoPlay;

        /// <summary>
        /// The list of music clips to play.
        /// </summary>
        [SerializeField, Tooltip("Format clip name to 'Song Title-Artist Name'")] AudioClip[] m_MusicClips;

        /// <summary>
        /// The audio source to play the music.
        /// </summary>
        [SerializeField] AudioSource m_AudioSource;

        /// <summary>
        /// The slider to control the timeline of the current playing track.
        /// </summary>
        [SerializeField] Slider m_TimelineSlider;

        /// <summary>
        /// The slider to control the volume of the current playing track.
        /// </summary>
        [SerializeField] Slider m_VolumeSlider;

        /// <summary>
        /// The dropdown to select the current track to play.
        /// </summary>
        [SerializeField] TMP_Dropdown m_Dropdown;

        /// <summary>
        /// The text to display the current playing track.
        /// </summary>
        [SerializeField] TMP_Text[] m_CurrentSongText;

        /// <summary>
        /// The toggle to play/pause the current track.
        /// </summary>
        [SerializeField] Toggle m_PlayPauseToggle;

        /// <summary>
        /// The toggle to shuffle the music.
        /// </summary>
        [SerializeField] Toggle m_ShuffleToggle;

        /// <summary>
        /// The button to play the next track.
        /// </summary>
        [SerializeField] Button m_NextButton;

        /// <summary>
        /// The button to play the previous track.
        /// </summary>
        [SerializeField] Button m_PreviousButton;

        /// <summary>
        /// The image to display the play/pause state.
        /// </summary>
        [SerializeField] Image m_PlayPauseImage;

        /// <summary>
        /// The sprite to display the pause state.
        /// </summary>
        [SerializeField] Sprite m_PauseSprite;

        /// <summary>
        /// The sprite to display the play state.
        /// </summary>
        [SerializeField] Sprite m_PlaySprite;

        /// <summary>
        /// The network variable to store the current song ID.
        /// </summary>
        readonly NetworkVariable<int> m_CurrentSongIdNetworked = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>
        /// The network variable to store the state of whether or not we are actively playing music.
        /// </summary>
        readonly NetworkVariable<bool> m_IsPlayingNetworked = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>
        /// Indicates whether the music should be shuffled.
        /// </summary>
        bool m_Shuffle;

        /// <summary>
        /// The current song ID.
        /// </summary>
        int currentSongId = 0;


        /// <inheritdoc/>
        void Start()
        {
            // Clear the dropdown options
            m_Dropdown.ClearOptions();

            if (m_MusicClips == null || m_MusicClips.Length == 0)
            {
                Utils.LogWarning("No music clips found in RoomMusic script. Please add some music clips to the RoomMusic script.");
                enabled = false;
                return;
            }

            // Add the music clip names as options to the dropdown
            foreach (var c in m_MusicClips)
            {
                TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(c.name);
                m_Dropdown.options.Add(optionData);
            }

            // If auto play is enabled, pick a random song
            if (m_AutoPlay)
            {
                PickRandomSong();
            }

            // Set the initial volume and clip for the audio source
            m_AudioSource.volume = m_VolumeSlider.value;
            m_AudioSource.clip = m_MusicClips[Random.Range(0, m_MusicClips.Length)];

            SetupUIListeners();

            // Update the song title text
            UpdateSongTitleText();
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Set the volume of the audio source
            m_AudioSource.volume = m_VolumeSlider.value;

            // Add listeners for the network variable change events
            m_CurrentSongIdNetworked.OnValueChanged += CurrentSongUpdated;
            m_IsPlayingNetworked.OnValueChanged += OnIsPlayingChanged;

            if (IsServer)
            {
                m_IsPlayingNetworked.Value = m_AutoPlay;
                if (!m_AutoPlay)
                {
                    m_CurrentSongIdNetworked.Value = 0;
                    SetClipTime(0.0f);
                    m_TimelineSlider.SetValueWithoutNotify(0.0f);
                }
            }
            else
            {
                if (m_IsPlayingNetworked.Value)
                {
                    SetSong(m_CurrentSongIdNetworked.Value);
                    GetCurrentSongPercFromServerRpc();
                }
                OnIsPlayingChanged(false, m_IsPlayingNetworked.Value);
            }
            CurrentSongUpdated(0, m_CurrentSongIdNetworked.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            OnIsPlayingChanged(false, false);

            // Remove listeners for the network variable change events
            m_CurrentSongIdNetworked.OnValueChanged -= CurrentSongUpdated;
            m_IsPlayingNetworked.OnValueChanged -= OnIsPlayingChanged;


            SetClipTime(0.0f);
            m_TimelineSlider.SetValueWithoutNotify(0.0f);

            if (IsServer)
            {
                if (m_CurrentSongIdNetworked != null)
                    m_CurrentSongIdNetworked.Value = 0;
                if (m_IsPlayingNetworked != null)
                    m_IsPlayingNetworked.Value = false;
            }
        }

        /// <inheritdoc/>
        private void Update()
        {
            if (!m_IsPlayingNetworked.Value || !NetworkManager.Singleton.IsConnectedClient)
                return;

            // Calculate the current playback percentage
            float perc = m_AudioSource.time / m_AudioSource.clip.length;
            m_TimelineSlider.SetValueWithoutNotify(perc);

            // If the playback is near the end, pick a new song
            if (perc >= .999f)
            {
                PickNewSong();
            }
        }

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            base.OnDestroy();
            RemoveUIListeners();
        }

        /// <summary>
        /// Sets up the listeners for UI events.
        /// </summary>
        void SetupUIListeners()
        {
            m_TimelineSlider.onValueChanged.AddListener(SetClipTime);
            m_VolumeSlider.onValueChanged.AddListener(UpdateVolume);
            m_Dropdown.onValueChanged.AddListener(PickSong);
            m_PlayPauseToggle.onValueChanged.AddListener(TogglePlay);
            m_ShuffleToggle.onValueChanged.AddListener(ToggleShuffle);

            m_NextButton.onClick.AddListener(delegate { PickNewSong(1); });
            m_PreviousButton.onClick.AddListener(delegate { PickNewSong(-1); });
        }

        /// <summary>
        /// Removes the listeners for UI events.
        /// </summary>
        void RemoveUIListeners()
        {
            m_TimelineSlider.onValueChanged.RemoveListener(SetClipTime);
            m_VolumeSlider.onValueChanged.RemoveListener(UpdateVolume);
            m_Dropdown.onValueChanged.RemoveListener(PickSong);
            m_PlayPauseToggle.onValueChanged.RemoveListener(TogglePlay);
            m_ShuffleToggle.onValueChanged.RemoveListener(ToggleShuffle);

            m_NextButton.onClick.RemoveListener(delegate { PickNewSong(1); });
            m_PreviousButton.onClick.RemoveListener(delegate { PickNewSong(-1); });
        }

        /// <summary>
        /// Updates the volume of the audio source.
        /// </summary>
        /// <param name="volume">The new volume value.</param>
        void UpdateVolume(float volume)
        {
            m_AudioSource.volume = volume;
        }

        /// <summary>
        /// Toggles the shuffle mode.
        /// </summary>
        /// <param name="toggle">The new toggle state.</param>
        void ToggleShuffle(bool toggle)
        {
            m_Shuffle = toggle;
        }

        /// <summary>
        /// Toggles the play/pause state of the audio source.
        /// </summary>
        /// <param name="toggle">The new toggle state.</param>
        void TogglePlay(bool toggle)
        {
            if (IsServer)
            {
                m_IsPlayingNetworked.Value = toggle;
            }
        }

        /// <summary>
        /// Picks a new song based on the shuffle mode and direction.
        /// </summary>
        /// <param name="dir">The direction to pick the new song (-1 for previous, 1 for next).</param>
        void PickNewSong(int dir = 1)
        {
            if (!IsServer)
                return;

            if (m_Shuffle)
            {
                PickRandomSong();
            }
            else
            {
                int nextSongId = Utils.RealMod(m_Dropdown.value + dir, m_MusicClips.Length);
                PickSong(nextSongId);
            }
        }

        /// <summary>
        /// Picks a random song to play.
        /// </summary>
        void PickRandomSong()
        {
            int tries = 10;
            int randomSongId = m_CurrentSongIdNetworked.Value;
            while (tries > 0)
            {
                tries--;
                randomSongId = Random.Range(0, m_MusicClips.Length);

                if (randomSongId != m_CurrentSongIdNetworked.Value)
                {
                    break;
                }
            }

            PickSong(randomSongId);
        }

        /// <summary>
        /// Picks a song to play based on the selected dropdown option.
        /// </summary>
        /// <param name="songId">The ID of the song to play.</param>
        void PickSong(int songId)
        {
            m_Dropdown.value = songId;
            SetClipTime(0.0f);
            if (IsServer)
            {
                m_CurrentSongIdNetworked.Value = songId;
                m_IsPlayingNetworked.Value = true;
            }
        }

        /// <summary>
        /// Plays a song based on the given song ID.
        /// </summary>
        /// <param name="oldSongId">The ID of the previous song.</param>
        /// <param name="songId">The ID of the new song.</param>
        void CurrentSongUpdated(int oldSongId, int songId)
        {
            if (songId >= 0 && songId < m_MusicClips.Length)
            {
                SetSong(songId);

                if (m_IsPlayingNetworked.Value)
                {
                    SetClipTime(0.0f);
                    m_AudioSource.Play();
                    m_PlayPauseImage.sprite = m_PauseSprite;

                    m_PlayPauseToggle.SetIsOnWithoutNotify(true);
                }
            }
        }

        void SetSong(int songId)
        {
            m_AudioSource.clip = m_MusicClips[songId];
            currentSongId = songId;
            UpdateSongTitleText();
        }

        void OnIsPlayingChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                m_AudioSource.Play();
                m_PlayPauseImage.sprite = m_PauseSprite;
            }
            else
            {
                m_AudioSource.Pause();
                m_PlayPauseImage.sprite = m_PlaySprite;
            }

            m_PlayPauseToggle.SetIsOnWithoutNotify(newValue);
        }

        /// <summary>
        /// Sets the clip time of the audio source based on the timeline slider value.
        /// </summary>
        /// <param name="value">The new value of the timeline slider.</param>
        void SetClipTime(float value)
        {
            if (!enabled)
                return;

            m_AudioSource.time = Mathf.Clamp(value, .01f, .99f) * m_AudioSource.clip.length;
        }

        [Rpc(SendTo.Server)]
        void GetCurrentSongPercFromServerRpc(RpcParams rpcParams = default)
        {
            SendCurrentSongPercToClientRpc(m_AudioSource.time / m_AudioSource.clip.length, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void SendCurrentSongPercToClientRpc(float perc, RpcParams rpcParams = default)
        {
            m_AudioSource.time = perc * m_AudioSource.clip.length;
            m_TimelineSlider.SetValueWithoutNotify(perc / m_AudioSource.clip.length);
        }

        /// <summary>
        /// Updates the song title text.
        /// </summary>
        void UpdateSongTitleText()
        {
            string songName = m_MusicClips[currentSongId == -1 ? 0 : currentSongId].name;
            string[] songNameSplit = m_MusicClips[currentSongId == -1 ? 0 : currentSongId].name.Split('-');
            if (songNameSplit.Length > 1)
            {
                songName = $"<b>{songNameSplit[0]}</b>\n{songNameSplit[1]}";
            }

            foreach (TMP_Text text in m_CurrentSongText)
            {
                text.text = songName;
            }
        }
    }
}
