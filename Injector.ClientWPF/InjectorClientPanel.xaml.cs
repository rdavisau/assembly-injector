using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Injector.DTO.Discovery;
using Injector.DTO.Messaging;
using Injector.InjectorHost;
using ReactiveUI;

namespace InjectorClientWPF
{
    /// <summary>
    ///     Interaction logic for InjectorClientPanel.xaml
    /// </summary>
    public partial class InjectorClientPanel : UserControl, IViewFor<InjectorClientViewModel>
    {
        public InjectorClientPanel()
        {
            InitializeComponent();

            DiscoveredServices.SelectionChanged += (sender, args) =>
            {
                var svc = DiscoveredServices.SelectedItem as InjectorHostServiceResponse;
                if (svc == null) return;

                ConnectToAddress.Text = svc.RunningAt.First();
                ConnectToPort.Text = svc.Port.ToString();
            };

            ViewModel = new InjectorClientViewModel();

            this.Bind(ViewModel, vm => vm.WatchPath, v => v.WatchPath.Text);
            this.Bind(ViewModel, vm => vm.WatchMask, v => v.WatchMask.Text);

            this.Bind(ViewModel, vm => vm.ConnectToAddress, v => v.ConnectToAddress.Text);
            this.Bind(ViewModel, vm => vm.ConnectToPort, v => v.ConnectToPort.Text);

            this.BindCommand(ViewModel, vm => vm.Connect, v => v.Connect);
            this.BindCommand(ViewModel, vm => vm.Disconnect, v => v.Disconnect);

            this.BindCommand(ViewModel, vm => vm.StartWatching, v => v.StartWatching);
            this.BindCommand(ViewModel, vm => vm.StopWatching, v => v.StopWatching);

            this.OneWayBind(ViewModel, x => x.DiscoveredServiceResponses, x => x.DiscoveredServices.ItemsSource);

            this.OneWayBind(ViewModel, vm => vm.IsWatching, vm => vm.StopWatching.Visibility,
                b => b ? Visibility.Visible : Visibility.Collapsed);
            this.OneWayBind(ViewModel, vm => vm.IsWatching, vm => vm.StartWatching.Visibility,
                b => b ? Visibility.Collapsed : Visibility.Visible);
            this.OneWayBind(ViewModel, vm => vm.IsConnected, vm => vm.Connect.Visibility,
                b => b ? Visibility.Collapsed : Visibility.Visible);
            this.OneWayBind(ViewModel, vm => vm.IsConnected, vm => vm.Disconnect.Visibility,
                b => b ? Visibility.Visible : Visibility.Collapsed);

            ViewModel
                .StartWatching
                .CanExecuteObservable
                .Select(ce => ce ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.IndianRed))
                .BindTo(this, v => v.WatchPath.Background);

            ViewModel.StartFindingServices();
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (InjectorClientViewModel) value; }
        }

        public InjectorClientViewModel ViewModel { get; set; }
    }

    public class MockHost : InjectorHostBase<InjectorClientProxy, Message>
    {
        public MockHost() : base()
        {
        }

        protected override void ProcessNewAssembly(Assembly newAssembly, InjectorClientProxy sender)
        {
            Console.WriteLine("NEW ASSEMBLYH BE HERE");
        }
    }
}