using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Globalization;

namespace ram_cpu_emulator
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CancellationTokenSource tokenSource;
        public Label cpuLabel;
        public Label ramLabel;
        public MainWindow()
        {
            tokenSource = new CancellationTokenSource();
            InitializeComponent();

        }
        private void startgather_Click(object sender, RoutedEventArgs e)
        {
            var gatherer = new DataGathering(tokenSource.Token, this, new string[] { "HD-Player", "Bluestacks", "HD-Agent" }, "Bluestacks");
            Task.Run(gatherer.Start);
            gatheringStatus.Content = "running";
            gatheringStatus.Foreground = new SolidColorBrush(Colors.Green);
            startgather.IsEnabled = false;
            stopgather.IsEnabled = true;
        }

        private void stopgather_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
            gatheringStatus.Content = "stopped";
            gatheringStatus.Foreground = new SolidColorBrush(Colors.Red);
            startgather.IsEnabled = true;
            stopgather.IsEnabled = false;
        }
    }

    public class DataGathering
    {
        private readonly CancellationToken _token;
        private readonly List<PerformanceCounter> _ramCounter;
        private readonly List<PerformanceCounter> _cpuCounter;
        private readonly string _emuName;
        private readonly MainWindow _window;

        public DataGathering(CancellationToken pToken, MainWindow pWindow, string[] emulators, string emuName)
        {
            _token = pToken;
            _window = pWindow;
            _emuName = emuName;
            //_emuName = Check_which_emu_is_running();
            //if (string.IsNullOrWhiteSpace(emu)) return;
            _ramCounter = new List<PerformanceCounter>();
            _cpuCounter = new List<PerformanceCounter>();
            // HONK! >(° 
            foreach (var emu in emulators)
            {
                Process p = Process.GetProcessesByName(emu)[0];
                _ramCounter.Add(new PerformanceCounter("Process", "Private Bytes", p.ProcessName));
                _cpuCounter.Add(new PerformanceCounter("Process", "% Processor Time", p.ProcessName));
            }

        }

        private string Check_which_emu_is_running()
        {
            string[] emulators_list = new string[] { "LeapdroidVM", "LdVBoxHeadless", "MEmuHeadless", "nox", };
            foreach (Process process in Process.GetProcesses())
            {
                foreach (string emu in emulators_list)
                {
                    if (process.ProcessName == emu)
                    {
                        Console.WriteLine(emu);
                        return emu;
                    }
                }
            }
            throw new Exception("Emulator not found");
        }


        public void Start()
        {
            int timeGather = 0;
            string file = _emuName + "__data.csv";
            while (!_token.IsCancellationRequested)
            {
                timeGather++;
                double data_ram = (Math.Round((_ramCounter.Sum(x => x.NextValue()) / 1024 / 1024), 2));
                double data_cpu = (Math.Truncate(100 * _cpuCounter.Sum(x => x.NextValue()) / 100));

                //Console.WriteLine("RAM: " + data_ram + " MB; CPU: " + data_cpu + " %");
                _window.cpu_amount.Dispatcher.Invoke(() =>
                {
                    _window.timeGather.Content = timeGather;
                    _window.cpu_amount.Content = data_cpu;
                    _window.ram_amount.Content = data_ram;
                    _window.Title = _emuName;
                });
                var records = new Dataramcpu { TimeGather = timeGather, Ram = data_ram, Cpu = data_cpu };
                File.AppendAllText(file, records.ToString() + Environment.NewLine);
                Thread.Sleep(1000);
            }
        }
    }

    public struct Dataramcpu
    {
        public double Ram { get; set; }
        public double Cpu { get; set; }
        public int TimeGather { get; set; }

        public override string ToString()
        {
            return $"{TimeGather.ToString()},{Ram.ToString(new CultureInfo("en-US"))},{Cpu.ToString(new CultureInfo("en-US"))}";
        }
    }
}