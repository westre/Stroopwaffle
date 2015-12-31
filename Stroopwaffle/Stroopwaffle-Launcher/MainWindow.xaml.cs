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
using System.Configuration;

namespace Stroopwaffle_Launcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();


            //btnMultiplayer.Click += BtnMultiplayer_Click;
        }

        private void BtnMultiplayer_Click(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void TestButtonClick(object sender, RoutedEventArgs e) {
            ServerEntry serverEntry = new ServerEntry("v0.1", "dev server", 0, 32, -1, "Test script");
            serverEntry.UpdateView();
            spServerList.Children.Add(serverEntry);
        }
    }
}
