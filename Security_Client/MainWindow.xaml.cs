using System;
using System.Collections.ObjectModel;
using System.Diagnostics; // Do otwierania folderów (Process)
using System.IO;
using System.Media;       // Do dźwięków systemowych
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace VisionGuard.Client
{
    public class AlertData
    {
        public bool alert { get; set; }
        public double confidence { get; set; }
        public string timestamp { get; set; }
        public string image_path { get; set; } // Odbieramy ścieżkę do zdjęcia
    }

    public class LogEntry
    {
        public string Time { get; set; }
        public string Message { get; set; }
        public string Confidence { get; set; }
    }

    public partial class MainWindow : Window
    {
        private UdpClient udpServer;
        private bool isRunning = true;
        public ObservableCollection<LogEntry> EventLogs { get; set; }

        // Ścieżka do folderu z dowodami (Dostosuj jeśli trzeba)
        // Zakładamy, że folder 'dowody' jest w folderze Pythona obok.
        // Ta ścieżka próbuje wyjść z folderu C# i wejść do Pythona.
        private string evidencePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Python\dowody"));

        public MainWindow()
        {
            InitializeComponent();
            EventLogs = new ObservableCollection<LogEntry>();
            LogList.ItemsSource = EventLogs;
            StartListening();
        }

        private async void StartListening()
        {
            try
            {
                udpServer = new UdpClient(5005);
                AddLog("INFO", "System uzbrojony. Port 5005.", "-");

                while (isRunning)
                {
                    var result = await udpServer.ReceiveAsync();
                    string jsonString = Encoding.UTF8.GetString(result.Buffer);
                    ProcessAlert(jsonString);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd sieci: " + ex.Message);
            }
        }

        private void ProcessAlert(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<AlertData>(json);

                if (data != null && data.alert)
                {
                    // 1. Zmiana wyglądu
                    StatusBorder.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47)); 
                    TxtStatus.Text = "WYKRYTO INTRUZA!";
                    TxtIcon.Text = "🚨";
                    string confPercent = $"{(int)(data.confidence * 100)}%";
                    TxtInfo.Text = $"Zapisano dowód: {data.timestamp}";

                    // 2. DŹWIĘK ALARMU (NOWOŚĆ)
                    // Odtwarza systemowy dźwięk "Hand" (krytyczny błąd) - jest głośny
                    SystemSounds.Hand.Play(); 

                    // 3. Logi
                    AddLog(data.timestamp, "Wykryto obiekt: CZŁOWIEK", confPercent);
                    
                    // Jeśli dostaliśmy ścieżkę od Pythona, możemy zaktualizować naszą zmienną evidencePath
                    // (Opcjonalne, ale pomocne)
                    if (!string.IsNullOrEmpty(data.image_path))
                    {
                        // Próba ustalenia folderu na podstawie pliku
                         string folder = Path.GetDirectoryName(data.image_path);
                         if (!string.IsNullOrEmpty(folder)) evidencePath = folder;
                    }

                    ResetTimer();
                }
            }
            catch { }
        }

        private void BtnOpenEvidence_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Jeśli ścieżka jest względna lub Python jej nie przysłał,
                // spróbujmy otworzyć folder domyślny lub ten ustalony z alertu.
                
                if (!Directory.Exists(evidencePath))
                {
                    // Jeśli C# nie może znaleźć folderu (bo np. jest w innym miejscu na dysku),
                    // otwieramy po prostu folder bieżący aplikacji, żeby użytkownik sobie poszukał.
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = AppDomain.CurrentDomain.BaseDirectory,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                    MessageBox.Show($"Nie znaleziono folderu: {evidencePath}\nOtwarto folder aplikacji.");
                    return;
                }

                // Otwieranie folderu w Eksploratorze Windows
                Process.Start(new ProcessStartInfo()
                {
                    FileName = evidencePath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się otworzyć folderu: " + ex.Message);
            }
        }

        private void AddLog(string time, string msg, string conf)
        {
            Dispatcher.Invoke(() => 
            {
                EventLogs.Insert(0, new LogEntry { Time = time, Message = msg, Confidence = conf });
            });
        }

        private async void ResetTimer()
        {
            await Task.Delay(3000); // Alarm trwa 3 sekundy
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)); 
            TxtStatus.Text = "SYSTEM BEZPIECZNY";
            TxtIcon.Text = "🛡️";
            TxtInfo.Text = "Monitorowanie obszaru...";
        }
    }
}