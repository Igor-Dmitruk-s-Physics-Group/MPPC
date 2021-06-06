using MPPC.App.ViewModels;
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
using ScottPlot;
using System.Diagnostics;

namespace MPPC.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainPageViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel = ViewModelResolver.GetViewModel<MainPageViewModel>();

            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += (sender, args) =>
            {
                if (viewModel.DataX.Length == 0 || viewModel.DataY.Length == 0)
                    return;
                Plot1.plt.Clear();
                Plot1.plt.PlotScatter(viewModel.DataX, viewModel.DataY, System.Drawing.Color.Red);
                Plot1.plt.AxisAuto();
                Plot1.Render();
            };
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();


        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await viewModel.ToggleConnection();
            }
            catch (Exception ex)
            {
                viewModel.WriteLogLine(ex.Message);
            }
        }

        private void PortSelectionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e?.AddedItems.Count > 0)
            {
                var selectedItem = e?.AddedItems[0] as string;
                viewModel.SelectedPort = selectedItem;
            }
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.SendCommand(Command.Clear);
        }

        private async void Finish_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.SendCommand(Command.Finish);
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.SendCommand(Command.Start);
        }

        private async void Delay_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.SendCommand(Command.Delay);
            viewModel.WriteLogLine(await viewModel.SendCommand(viewModel.DelayValue));
        }

        private async void Read_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.SendCommand(Command.Read);
        }

        private async void Visualize_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.ExportData();
        }

        
        private void Visualize_Checked(object sender, RoutedEventArgs e)
        {
            viewModel.DisplayingLiveData = !viewModel.DisplayingLiveData;
            Clear.IsEnabled = Finish.IsEnabled = Start.IsEnabled = Read.IsEnabled = false; 
        }

        private void Visualize_Unchecked(object sender, RoutedEventArgs e)
        {
            Clear.IsEnabled = Finish.IsEnabled = Start.IsEnabled = Read.IsEnabled = true;
        }
    }
}
