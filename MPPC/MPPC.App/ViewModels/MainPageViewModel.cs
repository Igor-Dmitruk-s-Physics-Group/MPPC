using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPPC.App.ViewModels
{
    public class MainPageViewModel : ViewModelBase, INotifyPropertyChanged
    {
        List<Timer> timers = new List<Timer>();
        bool isCollectingData = false;

        public MainPageViewModel()
        {

            timers.Add(new Timer(new TimerCallback((obj) =>
            {
                var vm = obj as MainPageViewModel;

                if (!vm.isConnected)
                {
                    vm.GetAvailableComPorts();
                }

            }), this, 0, 3000));


            timers.Add(new Timer(new TimerCallback((obj) =>
            {
                var vm = obj as MainPageViewModel;

                if (!vm.isConnected || isCollectingData || !displayingLiveData)
                    return;
                isCollectingData = true;
                ShowLiveData();
                isCollectingData = false;

            }), this, 0, 300));

            OnPlotDataReady += (sender, args) =>
            {
                DataX = args.dataX.Select(x => (double)x).ToArray();
                DataY = args.dataY.Select(y => (double)y).ToArray();
            };

            _serialLogger = new SerialLogger();
        }

        void ShowLiveData()
        {
            SendCommand(Command.Clear).Wait();
            SendCommand(Command.Start).Wait();
            Thread.Sleep(1000);
            SendCommand(Command.Finish).Wait();
            SendCommand(Command.Read).Wait();
        }

        SerialLogger _serialLogger;
        bool isConnected = false;
        SerialPort port;


        #region properties
        private double[] dataX = new double[0];
        public double[] DataX
        {
            get { return dataX; }
            set { dataX = value; OnPropertyChanged("DataX"); }
        }

        private double[] dataY = new double[0];
        public double[] DataY
        {
            get { return dataY; }
            set { dataY = value; OnPropertyChanged("DataY"); }
        }

        private string[] ports;
        public string[] Ports { get => ports; set { ports = value; OnPropertyChanged("Ports"); } }

        private string selectedPort;
        public string SelectedPort { get => selectedPort ?? ports.FirstOrDefault(); set { selectedPort = value; OnPropertyChanged("SelectedPort"); } }

        private string connectButtonText = "Connect";

        public string ConnectButtonText
        {
            get { return connectButtonText; }
            set { connectButtonText = value; OnPropertyChanged("ConnectButtonText"); }
        }


        private bool isControlsVisible;

        public bool IsControlsVisible
        {
            get { return isControlsVisible; }
            set { isControlsVisible = value; OnPropertyChanged("IsControlsVisible"); }
        }

        private byte delayValue;

        public byte DelayValue
        {
            get { return delayValue; }
            set { delayValue = value; OnPropertyChanged("DelayValue"); }
        }


        private string logText;
        #endregion
        internal async Task ExportData()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < DataX.Length; i++)
            {
                sb.Append((int)DataX[i]);
                sb.Append("\t");
                sb.Append((int)DataY[i]);
                sb.Append(Environment.NewLine);
            }

            Directory.CreateDirectory("output");

            string path = $"output\\{DateTime.Now:yyyy-MM-dd_HH-mm}.txt";
            File.WriteAllText(path, sb.ToString());
            WriteLogLine($"Data saved in file {Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), path)}");
        }

        public string LogText
        {
            get { return logText; }
            set { logText = value; OnPropertyChanged("LogText"); }
        }

        private bool connectEnabled = true;

        public bool ConnectEnabled { get => connectEnabled; set { connectEnabled = value; OnPropertyChanged("ConnectEnabled"); } }

        private int cutLevel;

        public int CutLevel
        {
            get { return cutLevel; }
            set { cutLevel = value; }
        }

        private bool displayingLiveData = false;
        public bool DisplayingLiveData
        {
            get => displayingLiveData;
            set
            {
                displayingLiveData = value;
                OnPropertyChanged("DisplayingLiveData");
            }
        }

        public void WriteLogLine(string message)
        {
            message += Environment.NewLine;
            message += LogText;
            LogText = message.Substring(0, Math.Min(message.Length, 5000));
        }

        void GetAvailableComPorts()
        {
            Ports = SerialPort.GetPortNames();
        }

        private async Task Connect()
        {

            port = new SerialPort(SelectedPort, 115200, Parity.None, 8, StopBits.One);
            port.Open();

            int timeout = 3;
            var cts = new CancellationTokenSource(timeout * 1000);

            var sw = new Stopwatch();
            sw.Start();
            try
            {
                ConnectEnabled = false;
                await Task.Run(async () =>
                {
                    if (cts.IsCancellationRequested)
                        return;
                    string cmd = port.ReadExisting();

                    ConnectButtonText = "Disconnect";
                    isConnected = true;
                    ToggleControls(true);
                    WriteLogLine("Successfuly connected to hardware");
                    //OnCommandResponse += (sender, args) => WriteLogLine(args.Response);
                    OnCommandResponse += MainPageViewModel_OnCommandResponse;
                    //_serialLogger.BeginListen(port, WriteLogLine);

                }, cts.Token);
            }
            catch (TaskCanceledException cancel)
            {
                WriteLogLine($"ACK message was not received from {port.PortName} in {sw.Elapsed.TotalSeconds} sec");
                port.Dispose();
            }
            finally
            {
                ConnectEnabled = true;
            }

        }



        private void MainPageViewModel_OnCommandResponse(object sender, OnCommandResponseEventArgs e)
        {
            WriteLogLine(e.Response);
            if (e.Command != Command.Read)
                return;

            try
            {
                var lines = e.Response.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                var rightParts = lines.Select(x => x.Split(": ")[1]);
                var dataY = rightParts.SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Skip(1)
                    .Reverse()
                    .Skip(1)
                    .Select(int.Parse)
                    .ToArray();


                var dataYCleaned = dataY
                    .Select(x => (int?)x)
                    .Select(x => (cutLevel == 0 || x < cutLevel) ? x : null)
                    .ToArray();

                int xIndex = 0;
                var dataX = new int[dataYCleaned.Where(x => x != null).Count()];
                for (int i = 0; i < dataY.Length; i++)
                {
                    try
                    {

                        var yVal = dataYCleaned[i];

                        if (yVal != null)
                        {
                            dataX[xIndex++] = i;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }


                var dataYFinal = dataYCleaned.Where(x => x.HasValue).Select(x => x.Value).ToArray();

                if (dataX.Length != dataYFinal.Length)
                    throw new InvalidDataException();
                OnPlotDataReady?.Invoke(this, (dataX, dataYFinal));
            }
            catch (Exception ex)
            {
                WriteLogLine(ex.Message);
            }
        }

        public event EventHandler<(int[] dataX, int[] dataY)> OnPlotDataReady;

        private void ToggleControls(bool isVisible)
        {
            IsControlsVisible = isVisible;
        }

        public async Task ToggleConnection()
        {
            if (isConnected)
            {
                Disconnect();
            }
            else
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    WriteLogLine("Port was not selected");
                    return;
                }
                await Connect();
            }

            await Task.FromResult<object>(null);
        }

        private void Disconnect()
        {
            isConnected = false;
            _serialLogger.StopListen();
            port.Close();
            ConnectButtonText = "Connect";
            ToggleControls(false);
            GetAvailableComPorts();
        }

        public async Task SendCommand(Command command)
        {
            var message = await SendCommand((byte)command);
            if (!string.IsNullOrEmpty(message) && message.Length > 2)
            {
                Debug.WriteLine(message);
                OnCommandResponse?.Invoke(this, new OnCommandResponseEventArgs
                {
                    Command = command,
                    Response = message
                });
            }
        }
        public async Task<string> SendCommand(byte command)
        {
            if (port == null || !port.IsOpen)
                return null;
            port.Write(new[] { (byte)command }, 0, 1);
            await Task.FromResult<object>(null);

            await Task.Delay(500);

            return port.ReadExisting();

        }

        public event EventHandler<OnCommandResponseEventArgs> OnCommandResponse;

    }

    public class OnCommandResponseEventArgs : EventArgs
    {
        public string Response { get; set; }
        public Command Command { get; set; }
    }
}
