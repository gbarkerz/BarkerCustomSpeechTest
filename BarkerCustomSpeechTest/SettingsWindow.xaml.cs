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
using System.Windows.Shapes;

namespace BarkerCustomSpeechTest
{
    public partial class SettingsWindow : Window
    {
        private MainWindow mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;

            SubscriptionUriEditor.Text = mainWindow.subscriptionEndpointId;
            SubscriptionKeyEditor.Text = mainWindow.subscriptionKey;
            RegionEditor.Text = mainWindow.region;

            ShowConfidenceToggleButton.IsChecked = mainWindow.showConfidenceWithResults;

            SubscriptionUriEditor.Focus();
        }

        private void CancelButton_Clicked(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Clicked(object sender, EventArgs e)
        {
            mainWindow.subscriptionEndpointId = SubscriptionUriEditor.Text;
            mainWindow.subscriptionKey = SubscriptionKeyEditor.Text;
            mainWindow.region = RegionEditor.Text;

            mainWindow.showConfidenceWithResults = (bool)ShowConfidenceToggleButton.IsChecked;

            mainWindow.SaveSettings();

            this.Close();
        }
    }
}
