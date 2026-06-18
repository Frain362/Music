using System.ComponentModel;

namespace WpfApp1
{
    public class Song : INotifyPropertyChanged
    {
        private string _duration;

        public string FilePath { get; set; }
        public string Name => System.IO.Path.GetFileNameWithoutExtension(FilePath);

        public string Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(nameof(Duration)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}