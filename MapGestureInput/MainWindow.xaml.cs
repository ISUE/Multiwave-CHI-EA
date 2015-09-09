using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Exocortex.DSP;
using Microsoft.Maps.MapControl.WPF;
using NAudio.Dsp;
using NAudio.Wave;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;
using GestureTests;

namespace MapGestureInput
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AsioOut asioOut;
        private WaveIn waveIn;

        public int waveOutChannels;

        public int idle_thresh = 6;
        public int minFrequency = 18300;
        public int frequencyStep = 600;
        public int buffersize = 2048;
        public int[] bin;
        public float[] sampledata;
        public float[] inbetween;
        bool init_inbetween = true;
        ComplexF[] indata;

        double[] filteredindata;
        double[] priori;

        int[] channelLabel;
        int[] velocity;
 
        int selectedChannels = 1;
        List<int> frequencies;
        List<int> centerbins;

        List<KeyFrequency> KF;

        List<List<int>> history;
        List<List<int>> inverse_history;
        PointCollection pointHist;
        Point3DCollection point3DHist;
        StylusPointCollection S;

        ComplexGesture gesture_history;
        ComplexGesture3D gesture_history3D;
        bool readyforgesture = false;
        bool gesture_started = false;
        int motion_free = 0;
        int idle_count = 0;
        int ignoreFrames = 0;

        DispatcherTimer dispatcherTimer;
        String previous_event = "unknown";
        double deltaX;
        double deltaY;

        SpeechRecognizer sr;

        GestureTests.Logger Log;

        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;            
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);

            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels",
                    waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }

            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 47 * buffersize / 2048;
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormat(44100, 32, 1);
            waveIn.DataAvailable += waveIn_DataAvailable;

            try
            {
                waveIn.StartRecording();
            }
            catch (NAudio.MmException e)
            {
                Console.WriteLine(e.ToString() + "\nPlug in a microphone!");
            }

            history = new List<List<int>>();
            inverse_history = new List<List<int>>();
            pointHist = new PointCollection();
            point3DHist = new Point3DCollection();

            bin = new int[buffersize * 2];
            sampledata = new float[buffersize * 2];
            priori = new double[buffersize * 2];

            channelLabel = new int[1];
            channelLabel[0] = 1;
            velocity = new int[1];
            velocity[0] = 0;


            for (int i = 0; i < buffersize * 2; i++)
            {
                bin[i] = i;
                sampledata[i] = 0;
                priori[i] = 0;

            }
            history.Add(new List<int> { 0 });
            inverse_history.Add(new List<int> { 0 });

            WekaHelper.initialize();
            
            sr = new SpeechRecognizer();
            Choices options = new Choices();
            options.Add(new string[] { "go home", "draw route", "clear map", "stop" });

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(options);

            // Create the Grammar instance.
            Grammar g = new Grammar(gb);
            sr.LoadGrammar(g);
            sr.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sr_SpeechRecognized);

            gesture_history = new ComplexGesture();
            gesture_history3D = new ComplexGesture3D();

            Log = new Logger("MapGesture");
        }

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {           
            if (init_inbetween)
            {
                inbetween = new float[e.BytesRecorded / 4 - buffersize];
                for (int i = 0; i < e.BytesRecorded / 4 - buffersize; i++)
                    inbetween[i] = 0;
                init_inbetween = false;
            }

            for (int index = 0; index < buffersize; index++)
            {
                int sample = (int)((e.Buffer[index * 4 + 3] << 24) |
                                        (e.Buffer[index * 4 + 2] << 16) |
                                        (e.Buffer[index * 4 + 1] << 8) |
                                        (e.Buffer[index * 4 + 0]));

                float sample32 = sample / 2147483648f;

                if (index >= (buffersize - inbetween.Length))
                    sampledata[index] = inbetween[index - buffersize + inbetween.Length];
                else
                    sampledata[index] = sampledata[index + buffersize + inbetween.Length];
                sampledata[index + buffersize] = sample32;

            }

            if (e.BytesRecorded / 4 - buffersize < 0)
                return;
            inbetween = new float[e.BytesRecorded / 4 - buffersize];

            for (int i = buffersize; i < e.BytesRecorded / 4; i++)
            {
                int sample = (int)((e.Buffer[i * 4 + 3] << 24) |
                                        (e.Buffer[i * 4 + 2] << 16) |
                                        (e.Buffer[i * 4 + 1] << 8) |
                                        (e.Buffer[i * 4 + 0]));

                float sample32 = sample / 2147483648f;
                inbetween[i - buffersize] = sample32;
            }

            bufferFFT();            
            if ((asioOut != null))
            {
                KF = new List<KeyFrequency>();
                //gestureDetected.Text = "";
                for (int i = 0; i < frequencies.Count; i++)
                {
                    if (history[i].Count > 0)
                        KF.Add(new KeyFrequency(frequencies.ElementAt(i), i + 1, frequencyStep/30, filteredindata, centerbins.ElementAt(i), history[i].Last()));
                    else
                        KF.Add(new KeyFrequency(frequencies.ElementAt(i), i + 1, frequencyStep/30, filteredindata, centerbins.ElementAt(i), 0));

                    velocity[i] = KF.ElementAt(i).state;                                       

                    history[i].Add(velocity[i]);
                    inverse_history[i].Add(KF[i].inverse_state);
                    //if (history[i].Count > 20)
                    //    history[i].RemoveAt(0);
                }
                detectGestures();
            }
        }

        private void detectGestures()
        {
            ignoreFrames++;
            if (selectedChannels == 1)
            {
                foreach (List<int> subList in history)
                {
                    int signChanges = 0, bandwidth = 0, step = 0, lastSig = 0;
                    for (int i = 1; i < subList.Count; i++)
                    {
                        step++;
                        if (subList[i - 1] != 0)
                            lastSig = subList[i - 1];
                        if (subList[i] * lastSig < 0)
                        {
                            signChanges++;
                            bandwidth += step;
                            step = 0;
                        }
                    }
                    /*
                    if (KF[0].isBoth && KF[0].inverse_state > 5)
                        gestureDetected.Text = "Two Handed ";
                    else if (signChanges == 0 && (lastSig != 0))
                        gestureDetected.Text = "Scrolling ";
                    else if (signChanges == 2 || signChanges == 1)
                        gestureDetected.Text = "SingleTap ";
                    else if (signChanges >= 3)
                        gestureDetected.Text = "DoubleTap ";*/
                }
            }
            else if (selectedChannels == 2)
            {
                double tot_X = 0, tot_Y = 0;
                foreach (KeyFrequency now in KF)
                {
                    tot_X += now.x;
                    tot_Y += now.y;
                }
                //gestureDetected.Text += (Math.Round(tot_X,2) + " " + Math.Round(tot_Y,2));
                pointHist.Add(new Point(tot_X, tot_Y));
                //if (pointHist.Count > 20)
                //    pointHist.RemoveAt(0);
                if (!gesture_started && tot_X == 0 && tot_Y == 0)
                {
                    idle_count++;

                    pointHist.Clear();
                    foreach (List<int> sublist in history)
                        sublist.Clear();
                    foreach (List<int> sublist in inverse_history)
                        sublist.Clear();


                    //TODO, Evaluate the proper length of the deadzone. (seems to be 6 frames)
                    if (idle_count > idle_thresh)
                    {
                        idle_count = 0;
                        gesture_history.clearGestureHistory();
                        //gestureDetected.Text = "none";
                    }
                }
                if (gesture_started && tot_X == 0 && tot_Y == 0)
                    motion_free++;
                if (tot_X != 0 || tot_Y != 0)
                {
                    gesture_started = true;
                    motion_free = 0;
                    idle_count = 0;
                }

                generateStroke(pointHist);
            }
            else if (selectedChannels >= 3)
            {
                double tot_X = 0, tot_Y = 0, tot_Z = 0;
                foreach (KeyFrequency now in KF)
                {

                    tot_X += now.x;
                    tot_Y += now.y;
                    tot_Z += now.z;
                    //For Graphing
                    //tot_X -= now.x;
                    //tot_Y += now.z;
                    //tot_Z -= now.y;
                }
                //gestureDetected.Text += (Math.Round(tot_X, 2) + " " + Math.Round(tot_Y, 2) + " " + Math.Round(tot_Z, 2));
                pointHist.Add(new Point(tot_X, tot_Y));
                point3DHist.Add(new Point3D(tot_X, tot_Y, tot_Z));
                //if (pointHist.Count > 20)
                //    pointHist.RemoveAt(0);
                //if (point3DHist.Count > 20)
                //    point3DHist.RemoveAt(0);
                if (!gesture_started && tot_X == 0 && tot_Y == 0 && tot_Z == 0)
                {
                    idle_count++;
                    pointHist.Clear();
                    point3DHist.Clear();
                    foreach (List<int> sublist in history)
                        sublist.Clear();
                    foreach (List<int> sublist in inverse_history)
                        sublist.Clear();

                    //TODO, Evaluate the proper length of the deadzone. 
                    if (idle_count > 6)
                    {
                        idle_count = 0;
                        gesture_history3D.clearGestureHistory();
                        //gestureDetected.Text = "none";
                    }
                }
                if (gesture_started && tot_X == 0 && tot_Y == 0 && tot_Z == 0)
                    motion_free++;
                if (tot_X != 0 || tot_Y != 0 || tot_Z != 0)
                {
                    gesture_started = true;
                    motion_free = 0;
                    idle_count = 0;
                }

                generateStroke(pointHist);
                //addPoints(point3DHist);
            }
            
            
            gestureCompleted();
        }        

        public void generateStroke(PointCollection pointHist)
        {
            S = new StylusPointCollection();
            S.Add(new StylusPoint(180, 134.333333333333));
            for (int i = 0; i < pointHist.Count; i++)
            {
                S.Add(new StylusPoint(S[i].X - pointHist[i].X, S[i].Y - pointHist[i].Y));
            }
        }

        public void bufferFFT()
        {
            indata = new ComplexF[buffersize * 2];
            for (int i = 0; i < buffersize * 2; i++)
            {
                indata[i].Re = sampledata[i] * (float)FastFourierTransform.HammingWindow(i, buffersize * 2);
                indata[i].Im = 0;
            }
            Exocortex.DSP.Fourier.FFT(indata, buffersize * 2, Exocortex.DSP.FourierDirection.Forward);
            filteredindata = filterMean(indata, 1.3);
        }

        private void StartStopSineWave()
        {
            if (asioOut == null)
            {                
                button1.Content = "Stop Sound";
                Console.WriteLine("User Selected Channels: " + selectedChannels);
                WaveOutCapabilities outdeviceInfo = WaveOut.GetCapabilities(0);
                waveOutChannels = outdeviceInfo.Channels;
                asioOut = new AsioOut(0);
                int waveOutDevices = WaveOut.DeviceCount;
                for (int i = 0; i < waveOutDevices; i++)
                {
                    outdeviceInfo = WaveOut.GetCapabilities(i);
                    Console.WriteLine("Device {0}: {1}, {2} channels",
                            i, outdeviceInfo.ProductName, outdeviceInfo.Channels);
                }

                List<IWaveProvider> inputs = new List<IWaveProvider>();
                frequencies = new List<int>();
                centerbins = new List<int>();

                for (int c = 0; c < selectedChannels; c++)
                {
                    if (c == 5 || c == 6)
                    {
                        //Ignore center channel
                        inputs.Add(new SineWaveProvider32(22000, 0.0f, 44100, 1));
                        frequencies.Add(22000);
                        centerbins.Add((int)Math.Round(22000 / 10.768));
                    }
                    else
                    {
                        inputs.Add(new SineWaveProvider32(minFrequency + c * frequencyStep, 0.5f, 44100, 1));
                        frequencies.Add(minFrequency + c * frequencyStep);
                        centerbins.Add((int)Math.Round((minFrequency + c * frequencyStep) / 10.768));
                    }
                }

                var splitter = new MultiplexingWaveProvider(inputs, selectedChannels);
                try
                {
                    asioOut.Init(splitter);
                    asioOut.Play();
                }
                catch (System.ArgumentException)
                {
                    Console.WriteLine("Invalid audio channel count. Please select a lower number of audio channels");
                }

                Console.WriteLine("Number of Channels: " + asioOut.NumberOfOutputChannels);
                Console.WriteLine("Playback Latency: " + asioOut.PlaybackLatency);
            }
            else
            {
                asioOut.Stop();
                asioOut.Dispose();
                asioOut = null;
                button1.Content = "Start Sound";

                frequencies.Clear();
                centerbins.Clear();
            }
        }

        public float mag2db(ComplexF y)
        {
            return 20.0f * (float)Math.Log10(Math.Sqrt(y.Re * y.Re + y.Im * y.Im) / .02);
        }

        public double[] filterMean(ComplexF[] data, double factor)
        {
            double[] outdata = new double[data.Length];

            double min = Double.PositiveInfinity;
            double mean = 0;
            for (int i = 0; i < data.Length; i++)
            {
                outdata[i] = mag2db(data[i]);
                min = Math.Min(outdata[i], min);
            }

            for (int i = 0; i < data.Length; i++)
            {
                outdata[i] -= min;
                mean += (outdata[i]);
            }
            mean /= data.Length;
            for (int i = 0; i < data.Length; i++)
                if (outdata[i] < (mean * factor))
                    outdata[i] = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if ((i > 0) && (i < (data.Length - 1)))
                {
                    if ((outdata[i] > 0) && (priori[i] == 0) && (outdata[i - 1] == 0) && (outdata[i + 1] == 0))
                    {
                        outdata[i] = 0;
                    }
                }
                priori[i] = outdata[i];
            }
            
            return outdata;
        }

        private void channelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (asioOut != null)
                StartStopSineWave();

            selectedChannels = (sender as ComboBox).SelectedIndex + 1;

            history.Clear();
            inverse_history.Clear();

            channelLabel = new int[selectedChannels];
            velocity = new int[selectedChannels];
            
            for (int i = 0; i < selectedChannels; i++)
            {
                history.Add(new List<int> { 0 });
                inverse_history.Add(new List<int> { 0 });
                channelLabel[i] = i + 1;
                velocity[i] = 0;
            }

        }
        
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            StartStopSineWave();
        }        

        public void gestureCompleted()
        {
            int motion_threshold = 3; //originally 5         
            int ignore_threshold = 20;
             
            if(ignoreFrames <= ignore_threshold)                          
            {
                motion_free = 0;
                readyforgesture = false;
                colorBox.Background = new SolidColorBrush(Colors.Red);
                gesture_started = false;
                //Clear the buffers
                foreach (List<int> sublist in history)
                    sublist.Clear();
                foreach (List<int> sublist in inverse_history)
                    sublist.Clear();
                pointHist.Clear();
                point3DHist.Clear();
            }

            if (gesture_started && ignoreFrames > ignore_threshold && motion_free > motion_threshold && selectedChannels >= 2)
            {
                pointHist = new PointCollection(pointHist.Reverse().Skip(motion_threshold).Reverse());
                point3DHist = new Point3DCollection(point3DHist.Reverse().Skip(motion_threshold).Reverse());
                S = new StylusPointCollection(S.Reverse().Skip(motion_threshold).Reverse());               
                for (int i = 0; i < history.Count; i++)
                {
                    history[i] = new List<int>(history[i].Reverse<int>().Skip(motion_threshold).Reverse<int>());
                    inverse_history[i] = new List<int>(inverse_history[i].Reverse<int>().Skip(motion_threshold).Reverse<int>());
                }

                if (pointHist.Count > 2)
                {
                    float[] speakers = { (float)KF[0].speakerTheta, (float)KF[1].speakerTheta };
                    gestureDetected.Text = WekaHelper.Classify(false, pointHist.Count() * waveIn.BufferMilliseconds,
                        true, new List<float>(speakers), pointHist, S, history, inverse_history);

                    //All the parameters to be passed to ComplexGesture are passed here.
                    double deltaX = S.Last().X - S.First().X;
                    double deltaY = S.Last().Y - S.First().Y;

                    double magnitude = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    double angle = Math.Atan2(deltaY,deltaX);

                    double duration = pointHist.Count() * waveIn.BufferMilliseconds;

                    gesture_history.addGesture(angle, magnitude, duration, calculateTotalAngleXY(S), gestureDetected.Text);
                    string gest = gesture_history.updateDetectedGesture();
                    complexGestureDetected.Text = gest;
                    if (ComplexGesture.isComplex(gest))
                    {
                        ignoreFrames = 0;
                    }

                    gestureHandler(gest);
                    Log.Log(gestureDetected.Text, "detect", history);                    
                }
                //Clear the buffers
                foreach (List<int> sublist in history)
                    sublist.Clear();
                foreach (List<int> sublist in inverse_history)
                    sublist.Clear();
                pointHist.Clear();
                point3DHist.Clear();

                //prepare for next gesture (might need a button press)
                readyforgesture = false;
                colorBox.Background = new SolidColorBrush(Colors.Red);
                gesture_started = false;
                motion_free = 0;
            }
        }

        public double calculateTotalAngleXY(StylusPointCollection points)
        {
            double TotalAngleXY = 0.0;

            int N = points.Count;

            double dx, dy;

            for (int i = 1; i < N; ++i)
            {
                dx = points[i].X - points[i - 1].X;
                dy = points[i].Y - points[i - 1].Y;

                float angleXY = (float)Math.Atan2(dy, dx);

                TotalAngleXY += angleXY * angleXY;
            }

            return TotalAngleXY;
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                readyforgesture = true;
                colorBox.Background = new SolidColorBrush(Colors.Green);
            }

            if (e.Key == Key.X)
            {
                var pin = new Pushpin();
                pin.Location = _map.Center;
                _map.Children.Add(pin);
            }

            if (e.Key == Key.C)
            {
                var pins = _map.Children.OfType<Pushpin>();
                double minDistance = Double.PositiveInfinity;
                Pushpin closest = null;
                foreach (Pushpin p in pins)
                {
                    double distance = Math.Sqrt(Math.Pow(p.Location.Latitude - _map.Center.Latitude, 2) + Math.Pow(p.Location.Longitude - _map.Center.Longitude, 2));
                    if (distance < minDistance)
                    {
                        closest = p;
                        minDistance = distance;
                    }
                }
                _map.Children.Remove(closest);
            }

            if (e.Key == Key.X && dispatcherTimer != null && dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop(); 
        }

        void gestureHandler(string detected)
        {
            /*if (previous_event == "swipe_up" || previous_event == "swipe_down" || previous_event == "swipe_left" || previous_event == "swipe_right" || previous_event == "swipe_front" || previous_event == "swipe_back")
            {
                if (detected == "swipe_up" || detected == "swipe_down" || detected == "swipe_left" || detected == "swipe_right" || detected == "swipe_front" || detected == "swipe_back")
                {
                    previous_event = "unknown";
                    return;
                }
            }*/
            if (dispatcherTimer != null && dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop();            

            if (detected == "x")
            {
                var pin = new Pushpin();
                pin.Location = _map.Center;
                _map.Children.Add(pin);
            }

            if (detected == "circle")
            {
                var pins = _map.Children.OfType<Pushpin>();
                double minDistance = Double.PositiveInfinity;
                Pushpin closest = null;
                foreach (Pushpin p in pins)
                {
                    double distance = Math.Sqrt(Math.Pow(p.Location.Latitude - _map.Center.Latitude, 2) + Math.Pow(p.Location.Longitude - _map.Center.Longitude, 2));
                    if (distance < minDistance)
                    {
                        closest = p;
                        minDistance = distance;
                    }
                }
                _map.Children.Remove(closest);
            }

            if (detected == "tap_up" || detected == "tap_front")
            {
                _map.ZoomLevel += 1;                
                return;
            }

            if (detected == "tap_down" || detected == "tap_back")
            {
                _map.ZoomLevel -= 1;
                return;
            }

            if (detected == "scratchout")
            {
                _map.SetView(new Location(28.6126, -81.2005), 15.0);
                if (dispatcherTimer != null && dispatcherTimer.IsEnabled)
                    dispatcherTimer.Stop();
            }

            deltaX = S.Last().X - S.First().X;
            deltaY = S.Last().Y - S.First().Y;
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += delegate
            {                
                //double angle = 180 * Math.Atan2(deltaY,deltaX) / Math.PI;
                double magnitude = Math.Sqrt(deltaX * deltaX + deltaY * deltaY) / 150;
                double angle = Math.Atan2(deltaY,deltaX);
                
                Microsoft.Maps.MapControl.WPF.Location c = new Microsoft.Maps.MapControl.WPF.Location(_map.Center);
                if (detected == "swipe_up" || detected == "swipe_down" || detected == "swipe_left" || detected == "swipe_right" || detected == "swipe_front" || detected == "swipe_back") 
                {
                    c.Latitude -= magnitude*(.01 / _map.ZoomLevel) * Math.Sin(angle);
                    c.Longitude += magnitude*(.01 / _map.ZoomLevel) * Math.Cos(angle);
                }
                
                _map.SetView(c, _map.ZoomLevel);
            };
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 25);
            dispatcherTimer.Start();
            previous_event = detected;
        }
        
        void sr_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //Play with this...
            if (e.Result.Text == "go home")
            {
                _map.SetView(new Location(28.6126, -81.2005), 15.0);
                if (dispatcherTimer != null && dispatcherTimer.IsEnabled)
                    dispatcherTimer.Stop();
            }
            if (e.Result.Text == "draw route")
            {
                /*
                MapPolyline polyline = new MapPolyline();
                polyline.Stroke = new SolidColorBrush(Colors.Blue);
                polyline.StrokeThickness = 5;
                polyline.Opacity = 0.7;
                polyline.Locations = new LocationCollection();
                foreach (Pushpin p in _map.Children.OfType<Pushpin>())
                {
                    polyline.Locations.Add(p.Location);
                }
                _map.Children.Add(polyline);
                 */
                DrawPath();
            }
            if (e.Result.Text == "clear map")
            {
                _map.Children.Clear();
                if (dispatcherTimer != null && dispatcherTimer.IsEnabled)
                    dispatcherTimer.Stop();
            }
            if (e.Result.Text == "stop")
            {
                if (dispatcherTimer != null && dispatcherTimer.IsEnabled)
                    dispatcherTimer.Stop();
            }
        }
        
        private async void DrawPath()
        {
            var pins = _map.Children.OfType<Pushpin>();
            string waypoints = "";
            if (pins.Count() < 2)
                return;
            for (int i = 0; i < pins.Count(); i++)
            {
                waypoints = waypoints + "&wp." + i + "=" +
                           string.Format("{0},{1}",
                         pins.ElementAt(i).Location.Latitude,
                            pins.ElementAt(i).Location.Longitude);
            }
            var url = "http://dev.virtualearth.net/REST/V1/Routes/Driving?o=json" + 
                        waypoints +        
                        "&optmz=distance&rpo=Points&key=" + "AjrzidIz-z5PmuA_DtnaT2qbeAZ-0jC52wMnYC7zFN7dXQM6UCk90IJImLs6DTCz";

            Uri geocodeRequest = new Uri(url);
            BingMapHelper.Response r = await GetResponse(geocodeRequest);
            if (r.StatusCode != 404)
            {
                var lines = _map.Children.OfType<MapPolyline>().ToList();
                for (int i = 0; i < lines.Count(); i++)
                    _map.Children.Remove(lines[i]);

                MapPolyline routeLine = new MapPolyline();
                routeLine.Locations = new LocationCollection();
                routeLine.Stroke = new SolidColorBrush(Colors.DarkBlue);
                routeLine.StrokeThickness = 3;
                routeLine.Opacity = 0.7;

                int bound = ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).
                    RoutePath.Line.Coordinates.GetUpperBound(0);
                /*
                double sourceLatitude = ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).
                    RoutePath.Line.Coordinates[0][0];
                double sourceLongitude = ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).
                    RoutePath.Line.Coordinates[0][1];

                double destinationLatitude = ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).
                    RoutePath.Line.Coordinates[bound][0];
                double destinationLongitude = ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).
                    RoutePath.Line.Coordinates[bound][1];

                var sourcePin = new Pushpin();
                var sourceLocation = new Location(sourceLatitude, sourceLongitude);
                MapLayer.SetPosition(sourcePin, sourceLocation);
                _map.Children.Add(sourcePin);


                var destinationLocation = new Location(destinationLatitude, destinationLongitude);
                MapLayer.SetPosition(pin, destinationLocation);
                _map.Children.Add(pin);
                */
                _map.SetView(pins.First().Location, _map.ZoomLevel);
                _directions.Text = "Directions\n";
                for (int i = 0; i < bound; i++)
                {
                    routeLine.Locations.Add(new Location
                    {
                        Latitude = ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).
                        RoutePath.Line.Coordinates[i][0],
                        Longitude = ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).
                        RoutePath.Line.Coordinates[i][1]
                    });                    
                }
                _map.Children.Add(routeLine);
                for (int i = 0; i < ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).RouteLegs[0].ItineraryItems.Count(); i++)
                {
                    _directions.Text += (i + 1) + ". " + ((BingMapHelper.Route)(r.ResourceSets[0].Resources[0])).RouteLegs[0].ItineraryItems[i].Instruction.Text + "\n";
                }
            }
            else
            {
                Console.WriteLine(r.errorDetails[0], "Route Path");                
            }
        }

        private async Task<BingMapHelper.Response> GetResponse(Uri uri)
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            var response = await client.GetAsync(uri);
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BingMapHelper.Response));
                return ser.ReadObject(stream) as BingMapHelper.Response;
            }
        }
    }
}
