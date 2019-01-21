﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using AudioBand.AudioSource;
using NLog;
using ServiceContracts;

namespace AudioSourceHost
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)]
    public class AudioSourceHostService : IAudioSourceHost
    {
        private readonly IAudioSource _audioSource;
        private readonly Logger _logger;
        private readonly Dictionary<string, AudioSourceSetting> _audioSourceSettings = new Dictionary<string, AudioSourceSetting>();
        private List<AudioSourceSetting> _audioSourceSettingsList; // so we can keep the order of the settings.
        private bool _isActive;

        public AudioSourceHostService(IAudioSource audioSource)
        {
            _logger = LogManager.GetLogger($"AudioSourceHostService({audioSource.Name})");

            _audioSource = audioSource;
            _audioSource.SettingChanged += AudioSourceOnSettingChanged;
            _audioSource.TrackInfoChanged += AudioSourceOnTrackInfoChanged;
            _audioSource.TrackPaused += AudioSourceOnTrackPaused;
            _audioSource.TrackPlaying += AudioSourceOnTrackPlaying;
            _audioSource.TrackProgressChanged += AudioSourceOnTrackProgressChanged;

            _audioSourceSettingsList = _audioSource.GetSettings();
            foreach (AudioSourceSetting setting in _audioSourceSettingsList)
            {
                _audioSourceSettings.Add(setting.Attribute.Name, setting);
            }
        }

        private IAudioSourceHostCallback Callback { get; set; }

        public async Task ActivateAsync()
        {
            if (_isActive)
            {
                return;
            }

            _isActive = true;

            try
            {
                await _audioSource.ActivateAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        public async Task DeactivateAsync()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;

            try
            {
                await _audioSource.DeactivateAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        public async Task NextTrackAsync()
        {
            if (!_isActive)
            {
                return;
            }

            try
            {
                await _audioSource.NextTrackAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        public async Task PauseTrackAsync()
        {
            if (!_isActive)
            {
                return;
            }

            try
            {
                await _audioSource.PauseTrackAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        public async Task PlayTrackAsync()
        {
            if (!_isActive)
            {
                return;
            }

            try
            {
                await _audioSource.PlayTrackAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        public async Task PreviousTrackAsync()
        {
            if (!_isActive)
            {
                return;
            }

            try
            {
                await _audioSource.PreviousTrackAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        public string GetName()
        {
            return _audioSource.Name;
        }

        public void OpenCallbackChannel()
        {
            Callback = OperationContext.Current.GetCallbackChannel<IAudioSourceHostCallback>();
        }

        public Task<List<AudioSourceSettingInfo>> GetAudioSourceSettingsAsync()
        {
            return Task.FromResult(_audioSourceSettingsList.Select(s => AudioSourceSettingInfo.From(s.Attribute, s.SettingValue, s.SettingType)).ToList());
        }

        public Task UpdateSettingAsync(string settingName, object value)
        {
            _audioSourceSettings[settingName].SettingValue = value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Try to gracefully close.
        /// </summary>
        /// <returns>Task</returns>
        public async Task Close()
        {
            try
            {
                await DeactivateAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private void AudioSourceOnTrackProgressChanged(object sender, TimeSpan e)
        {
            try
            {
                Callback.TrackProgressChanged(e);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void AudioSourceOnTrackPlaying(object sender, EventArgs e)
        {
            try
            {
                Callback.TrackPlaying();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void AudioSourceOnTrackPaused(object sender, EventArgs e)
        {
            try
            {
                Callback.TrackPaused();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void AudioSourceOnTrackInfoChanged(object sender, TrackInfoChangedEventArgs e)
        {
            try
            {
                Callback.TrackInfoChanged((TrackInfo)e);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void AudioSourceOnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            try
            {
                Callback.SettingChanged((SettingChangedInfo)e);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void HandleException(Exception e)
        {
            // Handle exceptions within wcf context by closing quickly so it can restart.
            _logger.Error(e);
            Program.Exit();
        }
    }
}