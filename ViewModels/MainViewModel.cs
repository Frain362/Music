using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const string PLAYLIST_FILE = "playlist.json";

        public ObservableCollection<Song> Playlist { get; set; }

        private Song _currentSong;
        public Song CurrentSong
        {
            get => _currentSong;
            set
            {
                _currentSong = value;
                OnPropertyChanged(nameof(CurrentSong));
                if (value != null)
                {
                    PlayCommand?.Execute(null);
                }
                SavePlaylist();
            }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                OnPropertyChanged(nameof(IsPlaying));
            }
        }

        private double _currentPosition;
        public double CurrentPosition
        {
            get => _currentPosition;
            set
            {
                _currentPosition = value;
                OnPropertyChanged(nameof(CurrentPosition));
            }
        }

        private double _volume = 0.5;
        public double Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0, 1);
                OnPropertyChanged(nameof(Volume));
                VolumeChanged?.Invoke(_volume);
                SaveSettings();
            }
        }

        public ICommand PlayCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand AddFilesCommand { get; }
        public ICommand RemoveSongCommand { get; }
        public ICommand MuteCommand { get; }
        public ICommand ClearPlaylistCommand { get; }

        public Action PlayAction { get; set; }
        public Action PauseAction { get; set; }
        public Action<string> LoadSongAction { get; set; }
        public Action<double> VolumeChanged { get; set; }

        private double _previousVolume = 0.5;

        public MainViewModel()
        {
            Playlist = new ObservableCollection<Song>();

            LoadPlaylist();
            LoadSettings();

            PlayCommand = new RelayCommand(() =>
            {
                if (CurrentSong == null) return;

                if (IsPlaying)
                {
                    PauseAction?.Invoke();
                }
                else
                {
                    LoadSongAction?.Invoke(CurrentSong.FilePath);
                    PlayAction?.Invoke();
                }
                IsPlaying = !IsPlaying;
            });

            NextCommand = new RelayCommand(() =>
            {
                if (Playlist.Count == 0) return;
                int index = Playlist.IndexOf(CurrentSong);
                if (index < Playlist.Count - 1)
                    CurrentSong = Playlist[index + 1];
                else
                    CurrentSong = Playlist[0];

                LoadSongAction?.Invoke(CurrentSong.FilePath);
                PlayAction?.Invoke();
                IsPlaying = true;
            });

            PreviousCommand = new RelayCommand(() =>
            {
                if (Playlist.Count == 0) return;
                int index = Playlist.IndexOf(CurrentSong);
                if (index > 0)
                    CurrentSong = Playlist[index - 1];
                else
                    CurrentSong = Playlist[Playlist.Count - 1];

                LoadSongAction?.Invoke(CurrentSong.FilePath);
                PlayAction?.Invoke();
                IsPlaying = true;
            });

            AddFilesCommand = new RelayCommand(() =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "Аудио файлы|*.mp3;*.wav;*.wma;*.m4a|Все файлы|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    foreach (var file in dialog.FileNames)
                    {
                        Playlist.Add(new Song { FilePath = file });
                    }
                    SavePlaylist();
                }
            });

            RemoveSongCommand = new RelayCommand(() =>
            {
                if (CurrentSong != null)
                {
                    var songToRemove = CurrentSong;
                    Playlist.Remove(songToRemove);
                    if (Playlist.Count > 0)
                        CurrentSong = Playlist[0];
                    else
                        CurrentSong = null;
                    SavePlaylist();
                }
            });

            MuteCommand = new RelayCommand(() =>
            {
                if (Volume > 0)
                {
                    _previousVolume = Volume;
                    Volume = 0;
                }
                else
                {
                    Volume = _previousVolume > 0 ? _previousVolume : 0.5;
                }
            });

            ClearPlaylistCommand = new RelayCommand(() =>
            {
                if (Playlist.Count > 0)
                {
                    var result = MessageBox.Show(
                        "Вы уверены, что хотите очистить плейлист?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Playlist.Clear();
                        CurrentSong = null;
                        SavePlaylist();
                    }
                }
            });
        }

        public void SavePlaylist()
        {
            try
            {
                var songs = new List<SongSaveData>();
                foreach (var song in Playlist)
                {
                    songs.Add(new SongSaveData { FilePath = song.FilePath });
                }

                // Явно указываем Newtonsoft.Json.Formatting
                string json = JsonConvert.SerializeObject(songs, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(PLAYLIST_FILE, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения плейлиста: {ex.Message}");
            }
        }

        private void LoadPlaylist()
        {
            try
            {
                if (File.Exists(PLAYLIST_FILE))
                {
                    string json = File.ReadAllText(PLAYLIST_FILE);
                    var songs = JsonConvert.DeserializeObject<List<SongSaveData>>(json);

                    if (songs != null)
                    {
                        Playlist.Clear();
                        foreach (var songData in songs)
                        {
                            if (File.Exists(songData.FilePath))
                            {
                                Playlist.Add(new Song { FilePath = songData.FilePath });
                            }
                        }

                        if (Playlist.Count > 0)
                        {
                            CurrentSong = Playlist[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки плейлиста: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new SettingsData { Volume = Volume };
                // Явно указываем Newtonsoft.Json.Formatting
                string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText("settings.json", json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists("settings.json"))
                {
                    string json = File.ReadAllText("settings.json");
                    var settings = JsonConvert.DeserializeObject<SettingsData>(json);

                    if (settings != null)
                    {
                        Volume = settings.Volume;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class SongSaveData
    {
        public string FilePath { get; set; }
    }

    public class SettingsData
    {
        public double Volume { get; set; }
    }
}