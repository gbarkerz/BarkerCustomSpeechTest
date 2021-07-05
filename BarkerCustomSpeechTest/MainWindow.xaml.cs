using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace BarkerCustomSpeechTest
{
    public partial class MainWindow : Window
    {
        // Barker: Wrap up these as properties.
        public string subscriptionEndpointId = "";
        public string subscriptionKey = "";
        public string region = "";
        public bool showConfidenceWithResults = false;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();
        }

        private void SpeakButton_Clicked(object sender, EventArgs e)
        {
            SpeakButton.IsEnabled = false;

            RecoSpeechLabel.Text = "Speak now...";

            GetSpeechResult();
        }

        private async void GetSpeechResult()
        {
            var result = await GetSpeechInputDefault(
                subscriptionEndpointId,
                subscriptionKey,
                region,
                showConfidenceWithResults);

            RecoSpeechLabel.Text = result;

            SpeakButton.IsEnabled = true;
        }

        private void SettingsButton_Clicked(object sender, EventArgs e)
        {
            RecoSpeechLabel.Text = "";

            var settingsWindow = new SettingsWindow(this);
            settingsWindow.Show();
        }

        public async Task<string> GetSpeechInputDefault(
            string subscriptionEndpointId,
            string subscriptionKey,
            string region,
            bool showConfidence)
        {
            string speechInput = "";

            try
            {
                var config = SpeechConfig.FromSubscription(subscriptionKey, region);
                config.EndpointId = subscriptionEndpointId;

                if (showConfidence)
                {
                    config.OutputFormat = OutputFormat.Detailed;
                }

                using (var recognizer = new SpeechRecognizer(config))
                {
                    var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                    // Have we got any recognized speech?
                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        if (showConfidence)
                        {
                            double highestConfidence = 0;

                            var detailedResults = result.Best();
                            foreach (var item in detailedResults)
                            {
                                if (item.Confidence > highestConfidence)
                                {
                                    highestConfidence = item.Confidence;

                                    speechInput = item.Text;
                                }
                            }

                            speechInput += "\r\n\r\n(Confidence is " +
                                (int)(highestConfidence * 100) +
                                "%)";
                        }
                        else
                        {
                            speechInput = result.Text;
                        }

                        Debug.WriteLine("Recognized speech: \" + speechInput + \", Duration: "
                            + result.Duration);
                    }
                    else
                    {
                        speechInput = "Sorry, I couldn't get any text from the speech.\r\n\r\n" +
                            "Reason: \"" + result.Reason.ToString() + "\"";

                        Debug.WriteLine("No recognized speech available. Reco status is: " +
                            result.Reason.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                speechInput = "Sorry, I couldn't get any text from the speech. " +
                    "Please check the required details have been supplied through the Settings button.\r\n\r\n" +
                    "Details: \"" + ex.Message + "\"";

                Debug.WriteLine("Attempt to recognize speech failed. " + ex.Message);
            }

            return speechInput;
        }

        private void LoadSettings()
        {
            var settings = new Settings1();

            subscriptionEndpointId = settings.EndpointID;
            subscriptionKey = settings.SubscriptionKey;
            region = settings.Region;
            showConfidenceWithResults = settings.ShowConfidence;
        }

        public void SaveSettings()
        {
            var settings = new Settings1();

            settings.EndpointID = subscriptionEndpointId;
            settings.SubscriptionKey = subscriptionKey;
            settings.Region = region;
            settings.ShowConfidence = showConfidenceWithResults;

            settings.Save();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var recognizedText = RecoSpeechLabel.Text;
            if (!String.IsNullOrWhiteSpace(recognizedText))
            {
                Clipboard.SetText(recognizedText);
            }
        }
    }
}
