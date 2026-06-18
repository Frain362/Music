using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private bool _isDraggingSlider = false;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Связываем ViewModel с MediaElement
            _viewModel.PlayAction = () => Player.Play();
            _viewModel.PauseAction = () => Player.Pause();
            _viewModel.LoadSongAction = (path) => Player.Source = new Uri(path);

            // Связываем громкость
            _viewModel.VolumeChanged = (volume) =>
            {
                Player.Volume = volume;
            };

            // Устанавливаем начальную громкость
            Player.Volume = _viewModel.Volume;

            // Таймер для обновления ползунка
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += (s, e) =>
            {
                if (Player.Source != null && Player.NaturalDuration.HasTimeSpan && !_isDraggingSlider)
                {
                    _viewModel.CurrentPosition = Player.Position.TotalSeconds / Player.NaturalDuration.TimeSpan.TotalSeconds;
                }
            };
            timer.Start();

            // Автоматически проигрывать первый трек при запуске
            if (_viewModel.Playlist.Count > 0 && _viewModel.CurrentSong != null)
            {
                _viewModel.LoadSongAction?.Invoke(_viewModel.CurrentSong.FilePath);
                _viewModel.PlayAction?.Invoke();
                _viewModel.IsPlaying = true;
            }
        }

        private void Player_MediaOpened(object sender, EventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                _viewModel.CurrentSong.Duration = Player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
            }
        }

        private void Player_MediaEnded(object sender, EventArgs e)
        {
            _viewModel.NextCommand.Execute(null);
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show($"Ошибка воспроизведения: {e.ErrorException.Message}", "Ошибка");
        }

        // Перетаскивание файлов
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (file.EndsWith(".mp3") || file.EndsWith(".wav") || file.EndsWith(".wma") || file.EndsWith(".m4a"))
                    {
                        _viewModel.Playlist.Add(new Song { FilePath = file });
                    }
                }
                _viewModel.SavePlaylist(); // Сохраняем после добавления
            }
        }

        // Обработка ползунка прогресса
        private void Slider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = false;
            if (Player.Source != null && Player.NaturalDuration.HasTimeSpan)
            {
                var position = _viewModel.CurrentPosition * Player.NaturalDuration.TimeSpan.TotalSeconds;
                Player.Position = TimeSpan.FromSeconds(position);
            }
        }

        // Горячие клавиши
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                _viewModel.PlayCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                if (Player.Source != null && Player.NaturalDuration.HasTimeSpan)
                {
                    var newPos = Player.Position + TimeSpan.FromSeconds(5);
                    if (newPos < Player.NaturalDuration.TimeSpan)
                    {
                        Player.Position = newPos;
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                if (Player.Source != null)
                {
                    var newPos = Player.Position - TimeSpan.FromSeconds(5);
                    if (newPos > TimeSpan.Zero)
                        Player.Position = newPos;
                    else
                        Player.Position = TimeSpan.Zero;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.N)
            {
                _viewModel.NextCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.P)
            {
                _viewModel.PreviousCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                _viewModel.Volume = Math.Min(1, _viewModel.Volume + 0.1);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                _viewModel.Volume = Math.Max(0, _viewModel.Volume - 0.1);
                e.Handled = true;
            }
            else if (e.Key == Key.M)
            {
                _viewModel.MuteCommand.Execute(null);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
    }
}