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
using System.Windows.Threading;

namespace BarkerCustomSpeechTest
{
    public partial class MainWindow : Window
    {
        // Barker: Wrap up these as properties.
        public string subscriptionEndpointId = "";
        public string subscriptionKey = "";
        public string region = "";
        public bool showConfidenceWithResults = false;

        private bool isListening = false;
        private SpeechRecognizer recognizer;
        private DispatcherTimer timeoutTimer;
        private int timeoutTimerTimeout = 30;
        private DateTime timeOfLastRecognition;

        public MainWindow()
        {
            InitializeComponent();

            this.Closed += MainWindow_Closed;

            // Create a timeout timer, so that if there's no speech recognized 
            // for a long time, give up listening.

            timeoutTimer = new DispatcherTimer();
            timeoutTimer.Tick += TimeoutTimer_Tick;
            timeoutTimer.Interval = new TimeSpan(0, 0, timeoutTimerTimeout);

            LoadSettings();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            StopRecognition();
        }

        private void SpeakButton_Clicked(object sender, EventArgs e)
        {
            if (!isListening)
            {
                // We're about to start listening, so kick off the timeout timer
                // so that we can cancel the listening if there's no speech for 
                // a long time.

                // The timeout timer is started on the UI thread.
                timeOfLastRecognition = DateTime.Now;
                timeoutTimer.Start();

                SpeakButton.Content = "_Stop speaking";

                RecoSpeechLabel.Text = "Speak now...";

                isListening = true;

                // Now give of the listening on a background thread.
                Task.Run(() => {
                    GetSpeechResult();
                });
            }
            else
            {
                // Stop any in-progress listening.
                StopRecognition();
            }
        }

        private void StopRecognition()
        {
            // Let the recognizer running on the background thread know 
            // that it should stop listening
            if (recognizer != null)
            {
                recognizer.StopContinuousRecognitionAsync();
            }

            // We no longer need a timeout timer running.
            timeoutTimer.Stop();

            // Reverr the UI to its not-listening state.
            SpeakButton.Content = "_Speak";

            isListening = false;
        }

        // GetSpeechResult() is running on a background thread.
        private async Task GetSpeechResult()
        {
            await GetSpeechInputDefault(
                subscriptionEndpointId,
                subscriptionKey,
                region,
                showConfidenceWithResults);
        }

        public async Task GetSpeechInputDefault(
            string subscriptionEndpointId,
            string subscriptionKey,
            string region,
            bool showConfidence)
        {
            string speechInput = "";

            // This is all running on a background thread.
            try
            {
                var config = SpeechConfig.FromSubscription(subscriptionKey, region);
                config.EndpointId = subscriptionEndpointId;

                if (showConfidence)
                {
                    config.OutputFormat = OutputFormat.Detailed;
                }

                recognizer = new SpeechRecognizer(config);

                var stopRecognition = new TaskCompletionSource<int>();

                // We're not interested at the moment in these events.
                //recognizer.Recognizing += (s, e) =>
                //{
                //    Debug.WriteLine("Recognizing.");
                //};

                //recognizer.Canceled += (s, e) =>
                //{
                //    Debug.WriteLine("Canceled.");
                //};

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        timeOfLastRecognition = DateTime.Now;

                        if (!IsSpecialCommand(e.Result.Text, ref speechInput))
                        {
                            Debug.WriteLine("Recognized: " + e.Result.Text);

                            speechInput += (speechInput.Length == 0 ? "" : " ") + e.Result.Text;
                        }

                        // Continually build up the entire recognized sentence.
                        Application.Current.Dispatcher.Invoke(
                            new Action(() => { RecoSpeechLabel.Text = speechInput; }));
                    }
                    // ResultReason.NoMatch only seems to get raised when speech occurs
                    // after a pause.
                    //else if (e.Result.Reason == ResultReason.NoMatch)
                    //{
                    //    Debug.WriteLine("NoMatch.");
                    //}
                };

                // This event is raised following the Stop Speaking button being pressed.
                recognizer.SessionStopped += (s, e) =>
                {
                    Debug.WriteLine("Session stopped.");

                    stopRecognition.TrySetResult(0);
                };

                await recognizer.StartContinuousRecognitionAsync();

                // This returns once the Stop Speaking button's been raised.
                Task.WaitAny(new[] { 
                    stopRecognition.Task });
            }
            catch (Exception ex)
            {
                speechInput = "Sorry, I couldn't get any text from the speech. " +
                    "Please check the required details have been supplied through the Settings button.\r\n\r\n" +
                    "Details: \"" + ex.Message + "\"";

                Debug.WriteLine("Attempt to recognize speech failed. " + ex.Message);
            }

            Debug.WriteLine("Final recognized phrase is: " + speechInput);
        }

        private bool IsSpecialCommand(string utterance, ref string speechInput)
        {
            bool isSpecialCommand = false;

            if (utterance.Trim().ToLower().StartsWith("join last sentence"))
            {
                StringBuilder sb = new StringBuilder(speechInput);
                if (sb.Length > 1)
                {
                    // Ignore the last character in the string. We never want
                    // to change that character, regardless of what it is.
                    for (int i = sb.Length - 2; i > 0; --i)
                    {
                        if (sb[i] == '.')
                        {
                            sb[i] = ',';

                            // If the next character is upper case, change it to lower case.
                            for (int j = i + 1; j < sb.Length - 2; ++j)
                            {
                                if (sb[j] != ' ')
                                {
                                    if ((sb[j] >= 65 ) && (sb[j] <= 90))
                                    {
                                        sb[j] = (char)(sb[j] + 32); 
                                    }

                                    break;
                                }
                            }

                            break;
                        }
                    }

                    speechInput = sb.ToString();
                }

                isSpecialCommand = true;
            }

            return isSpecialCommand;
        }

        private void TimeoutTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan span = DateTime.Now - timeOfLastRecognition;

            if (span.TotalSeconds > 30)
            {
                StopRecognition();
            }
        }

        private void SettingsButton_Clicked(object sender, EventArgs e)
        {
            StopRecognition();

            RecoSpeechLabel.Text = "";

            var settingsWindow = new SettingsWindow(this);
            settingsWindow.Show();
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
            StopRecognition();

            var recognizedText = RecoSpeechLabel.Text;
            if (!String.IsNullOrWhiteSpace(recognizedText))
            {
                Clipboard.SetText(recognizedText);
            }
        }
    }
}
