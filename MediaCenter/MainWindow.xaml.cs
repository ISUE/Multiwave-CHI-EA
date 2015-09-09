using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Exocortex.DSP;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Windows.Threading;
using System.Speech.Recognition;
using System.IO;

namespace MediaCenter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Probably an X or something that is not easily replicable - Key gesture to enable gesture recognition? would solve people standing and sitting
    /// Use Tap and then the maximum amplitude - Swipe/Tap towards each SPEAKER to select channels 
    /// Changing system volume by swiping up or down
    /// Circle followed by swipe Change balance/fade by gesturing and then sending pointing in a direction
    /// Move through the selected media in a two dimensional fashion
    /// Mute gesture (X? tap down?)

    /// Also demonstrate sending multiple sounds to different speakers for a sound engineering test by putting in
    /// some audio provider mixing that will allow for each speaker to have interesting stuff playing on it.

    /// </summary>
    public partial class MainWindow : Window
    {
        private AsioOut asioOut;
        private WaveIn waveIn;

        public int waveOutChannels;

        public int minFrequency = 17500;
        public int frequencyStep = 500;
        public int idle_thresh = 7;
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

        bool isMusicPlaying = false;
        List<Mp3FileReader> BGMusicList;
        List<WaveChannel32> BG32Channels;
        List<WaveMixerStream32> MixerList;

        int fileIndex = 0;
        FileInfo[] files;

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
            
            /*
            sr = new SpeechRecognizer();
            Choices options = new Choices();
            options.Add(new string[] { "go home", "draw route", "clear map", "stop" });

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(options);

            // Create the Grammar instance.
            Grammar g = new Grammar(gb);
            sr.LoadGrammar(g);
            sr.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sr_SpeechRecognized);
            */
            String searchPattern = "*.mp3";

            DirectoryInfo di = new DirectoryInfo(@".\music\");
            files = di.GetFiles(searchPattern);
            foreach (FileInfo fi in files)
                TrackList.Items.Add(fi.Name);

            gesture_history = new ComplexGesture();
            gesture_history3D = new ComplexGesture3D();

            Log = new GestureTests.Logger("MediaCenter");

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

            if (isMusicPlaying)
            {
                for (int c = 0; c < BG32Channels.Count(); c++)
                {
                    if(BG32Channels[c].Position >= BG32Channels[c].Length) 
                    {
                        BG32Channels[c].Position = 0;                        
                    }
                }
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
                    if (idle_count > 6)
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
                    if (idle_count > idle_thresh)
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
                
                //Selectable number of channelsplp;[
                BGMusicList = new List<Mp3FileReader>();
                BG32Channels = new List<WaveChannel32>();
                MixerList = new List<WaveMixerStream32>();

                for (int c = 0; c < selectedChannels; c++)
                {
                    BGMusicList.Add(new Mp3FileReader(files[fileIndex].FullName));
                    BG32Channels.Add(new WaveChannel32(BGMusicList[c]));
                    BG32Channels[c].PadWithZeroes = false;
                    BG32Channels[c].Volume = .03f;

                    MixerList.Add(new WaveMixerStream32());
                    MixerList[c].AutoStop = true;
                    MixerList[c].AddInputStream(BG32Channels[c]);
                }
                selectedFile.Text = files[fileIndex].Name;
                
                /*
                //music testing code
                var backgroundMusic = new Mp3FileReader("tecmo.mp3");                               
                var back32 = new WaveChannel32(backgroundMusic);
                back32.PadWithZeroes = false;
                back32.Volume = .01f;

                var mixer = new WaveMixerStream32();
                mixer.AutoStop = true;
                mixer.AddInputStream(back32);

                var BM2 = new Mp3FileReader("tecmo.mp3");
                var back321 = new WaveChannel32(BM2);
                back321.PadWithZeroes = false;
                back321.Volume = .01f;                
                
                var mixer2 = new WaveMixerStream32();
                mixer2.AutoStop = true;
                mixer2.AddInputStream(back321); 
               */

                for (int c = 0; c < selectedChannels; c++)
                {
                    if (c >= 5)
                    {
                        //Ignore center channel
                        var temp = new SineWaveProvider32((minFrequency + (c - 1) * frequencyStep), 0.8f, 44100, 2);
                        MixerList[c].AddInputStream(new WaveProviderToWaveStream(temp, BG32Channels[c]));
                        inputs.Add(MixerList[c]);
                        frequencies.Add(minFrequency + (c - 1) * frequencyStep);
                        centerbins.Add((int)Math.Round((minFrequency + (c - 1) * frequencyStep) / 10.768));
                    }
                    else if (c >= 3)
                    {
                        //Ignore center channel
                        var temp = new SineWaveProvider32((minFrequency + (c - 1) * frequencyStep), 0.5f, 44100, 2);
                        MixerList[c].AddInputStream(new WaveProviderToWaveStream(temp, BG32Channels[c]));
                        inputs.Add(MixerList[c]);
                        frequencies.Add(minFrequency + (c - 1) * frequencyStep);
                        centerbins.Add((int)Math.Round((minFrequency + (c - 1) * frequencyStep) / 10.768));
                    }
                    //music Testing code
                    else// if (c == 0)
                    {
                        var temp = new SineWaveProvider32(minFrequency + c * frequencyStep, 0.5f, 44100, 2);
                        MixerList[c].AddInputStream(new WaveProviderToWaveStream(temp, BG32Channels[c]));
                        inputs.Add(MixerList[c]);
                        frequencies.Add(minFrequency + c * frequencyStep);
                        centerbins.Add((int)Math.Round((minFrequency + c * frequencyStep) / 10.768));
                    }
                    /*
                    else
                    {
                        inputs.Add(new SineWaveProvider32(18000 + c * 700, 0.5f, 44100, 1));
                        frequencies.Add(18000 + c * 700);
                        centerbins.Add((int)Math.Round((18000 + c * 700) / 10.768));
                    }*/
                }

                var splitter = new MultiplexingWaveProvider(inputs, selectedChannels);
                for(int i = 0; i < selectedChannels; i++)
                {
                    //if(i%2 == 0)
                        splitter.ConnectInputToOutput(i*2, i);
                    //if (i % 1 == 0)
                        //splitter.ConnectInputToOutput((i * 2)+1, i);
                }
                try
                {
                    oldVolume = new List<float>() { 0.03f, 0.03f, 0.03f, 0.03f, 0.03f, 0.03f };
                    asioOut.Init(splitter);
                    asioOut.Play();

                    isMusicPlaying = true;
                    Volume1.Value = 3;
                    Volume2.Value = 3;
                    Volume3.Value = 3;
                    Volume4.Value = 3;
                    Volume5.Value = 3;
                    Volume6.Value = 3;

                    TrackList.SelectedIndex = fileIndex;
                    
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

                isMusicPlaying = false;

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
            if (isMuted)
            {
                oldVolume.Clear();
                isMuted = false;
            }
            if (isPaused)
            {
                pauseVolume.Clear();
                pausePosition.Clear();
            }

            if (asioOut != null)
                StartStopSineWave();

            selectedChannels = (sender as ComboBox).SelectedIndex + 1;
            if (selectedChannels == 3)
                selectedChannels = 6;

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
            speakerSelectorContentChanged();

            switch (selectedChannels)
            {
                case 6:
                    Volume1.Visibility = System.Windows.Visibility.Visible;
                    Volume2.Visibility = System.Windows.Visibility.Visible;
                    Volume3.Visibility = System.Windows.Visibility.Visible;
                    Volume4.Visibility = System.Windows.Visibility.Visible;
                    Volume5.Visibility = System.Windows.Visibility.Visible;
                    Volume6.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 2:
                    Volume1.Visibility = System.Windows.Visibility.Visible;
                    Volume2.Visibility = System.Windows.Visibility.Visible;
                    Volume3.Visibility = System.Windows.Visibility.Hidden;
                    Volume4.Visibility = System.Windows.Visibility.Hidden;
                    Volume5.Visibility = System.Windows.Visibility.Hidden;
                    Volume6.Visibility = System.Windows.Visibility.Hidden;
                    break;
                case 1:
                    Volume1.Visibility = System.Windows.Visibility.Visible;
                    Volume2.Visibility = System.Windows.Visibility.Hidden;
                    Volume3.Visibility = System.Windows.Visibility.Hidden;
                    Volume4.Visibility = System.Windows.Visibility.Hidden;
                    Volume5.Visibility = System.Windows.Visibility.Hidden;
                    Volume6.Visibility = System.Windows.Visibility.Hidden;
                    break;
            }

        }

        private void speakerSelectorContentChanged()
        {
            speakerSelector.Items.Clear();
            speakerSelector.Items.Add("1");
            speakerSelector.SelectedIndex = 0;
            if (selectedChannels == 2)
                speakerSelector.Items.Add("2");
            else if (selectedChannels == 6)
            {
                speakerSelector.Items.Add("2");
                speakerSelector.Items.Add("3");
                speakerSelector.Items.Add("4");
                speakerSelector.Items.Add("5");
                speakerSelector.Items.Add("6");
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

            if (ignoreFrames <= ignore_threshold)
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

            if (gesture_started && ignoreFrames > ignore_threshold && motion_free > motion_threshold && selectedChannels == 6)
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
                    List<float> speakers = new List<float>();
                    List<float> elevations = new List<float>();
                    foreach(KeyFrequency K in KF)
                    {
                        speakers.Add((float)K.speakerTheta);
                        elevations.Add((float)K.speakerAltitude);
                    }
                    //Call function to find features and test with weka machine
                    gestureDetected.Text = WekaHelper.Classify3D(false, pointHist.Count() * waveIn.BufferMilliseconds,
                        true, speakers,elevations, point3DHist, history, inverse_history);
                    
                    //All the parameters to be passed to ComplexGesture are passed here.
                    double deltaX = S.Last().X - S.First().X;
                    double deltaY = S.Last().Y - S.First().Y;

                    double magnitude = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    double angle = Math.Atan2(deltaY, deltaX);

                    double duration = pointHist.Count() * waveIn.BufferMilliseconds;
                    double deltaZ = (from p3d in point3DHist select p3d.Z).Sum();

                    gesture_history3D.addGesture(angle, magnitude, Math.Atan2(deltaZ, magnitude) /*elevation*/, duration, calculateTotalAngleXY(S), gestureDetected.Text);
                    string gest3D = gesture_history3D.updateDetectedGesture();
                    complexGestureDetected.Text = gest3D;
                    if (ComplexGesture3D.isComplex(gest3D))
                    {
                        ignoreFrames = 0;
                    }

                    gestureHandler((gest3D == "unknown") ? gestureDetected.Text : gest3D, selectedSpeaker());
                    Log.Log(gest3D,"detect", history);
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


        bool isMuted = false;
        bool isPaused = false;
        List<float> oldVolume;
        List<float> pauseVolume;
        List<long> pausePosition;
        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                readyforgesture = true;
                colorBox.Background = new SolidColorBrush(Colors.Green);
            }
            if (e.Key == Key.C)
            {
                if (BG32Channels == null) return;
                int chan = speakerSelector.SelectedIndex;
                var tempfile = new Mp3FileReader(files[fileIndex].FullName);
                var temp = new WaveChannel32(tempfile);
                temp.PadWithZeroes = false;
                temp.Volume = oldVolume[chan];
                MixerList[chan].RemoveInputStream(BG32Channels[chan]);
                BG32Channels[chan] = temp;
                BGMusicList[chan] = tempfile;
                MixerList[chan].AddInputStream(temp);
                MixerList[chan].Seek(0, 0);
            }
            if (e.Key == Key.OemPlus)
            {
                if (BG32Channels == null) return;
                int chan = speakerSelector.SelectedIndex;
                if (BG32Channels[chan].Volume <= 0.3f)
                    BG32Channels[chan].Volume += .03f;
                if (BG32Channels[chan].Volume > .3f)
                    BG32Channels[chan].Volume = 0.3f;
                
            }
            if (e.Key == Key.OemMinus)
            {
                if (BG32Channels == null) return;
                int chan = speakerSelector.SelectedIndex;
                if (BG32Channels[chan].Volume >= 0.03f)                    
                    BG32Channels[chan].Volume -= .03f;
                if (BG32Channels[chan].Volume < .03f)
                    BG32Channels[chan].Volume = 0.00000000001f;

            }
            if (e.Key == Key.P)
            {
                if (BG32Channels == null) return;
                if (isPaused)
                {
                    for (int i = 0; i < BG32Channels.Count(); i++)
                    {
                        BG32Channels[i].Volume = pauseVolume[i];
                        BG32Channels[i].Position = pausePosition[i];
                    }
                    isPaused = false;
                }
                else
                {
                    pauseVolume = new List<float>();
                    pausePosition = new List<long>();
                    for (int i = 0; i < BG32Channels.Count(); i++)
                    {
                        pausePosition.Add(BG32Channels[i].Position);
                        pauseVolume.Add(BG32Channels[i].Volume);
                        BG32Channels[i].Volume = 0;
                    }
                    isPaused = true;
                }
            }
            if (e.Key == Key.M)
            {
                if (BG32Channels == null) return;
                if (isMuted)
                {
                    for (int i = 0; i < BG32Channels.Count(); i++ )
                        BG32Channels[i].Volume = oldVolume[i];
                    isMuted = false;
                }
                else
                {
                    oldVolume = new List<float>();
                    for (int i = 0; i < BG32Channels.Count(); i++)
                    {
                        oldVolume.Add(BG32Channels[i].Volume);
                        BG32Channels[i].Volume = 0;
                    }
                    isMuted = true;
                }                
            }
            if (e.Key == Key.OemPeriod)
            {
                if (BG32Channels == null || asioOut == null) return;
                fileIndex++;
                if (fileIndex >= files.Count())
                    fileIndex = 0;

                TrackList.SelectedIndex = fileIndex;

                asioOut.Pause();
                for (int i = 0; i < selectedChannels; i++)
                {
                    oldVolume[i] = BG32Channels[i].Volume;
                    var tempfile = new Mp3FileReader(files[fileIndex].FullName);
                    var temp = new WaveChannel32(tempfile);
                    temp.PadWithZeroes = false;
                    temp.Volume = oldVolume[i];
                    MixerList[i].RemoveInputStream(BG32Channels[i]);
                    BG32Channels[i] = temp;
                    BGMusicList[i] = tempfile;
                    MixerList[i].AddInputStream(temp);
                    MixerList[i].Seek(0, 0);
                }
                selectedFile.Text = files[fileIndex].Name;
                asioOut.Play();
            }
            if (e.Key == Key.OemComma)
            {
                if (BG32Channels == null || asioOut == null) return;
                fileIndex--;
                if (fileIndex < 0)
                    fileIndex = files.Count()-1;

                TrackList.SelectedIndex = fileIndex;

                asioOut.Pause();
                for (int i = 0; i < selectedChannels; i++)
                {
                    oldVolume[i] = BG32Channels[i].Volume;
                    var tempfile = new Mp3FileReader(files[fileIndex].FullName);
                    var temp = new WaveChannel32(tempfile);
                    temp.PadWithZeroes = false;
                    temp.Volume = oldVolume[i];
                    MixerList[i].RemoveInputStream(BG32Channels[i]);
                    BG32Channels[i] = temp;
                    BGMusicList[i] = tempfile;
                    MixerList[i].AddInputStream(temp);
                    MixerList[i].Seek(0, 0);
                }
                selectedFile.Text = files[fileIndex].Name;
                asioOut.Play();
            }

            if (e.Key == Key.D1)
                speakerSelector.SelectedIndex = 0;
            if (e.Key == Key.D2)
                speakerSelector.SelectedIndex = 1;
            if (e.Key == Key.D3)
                speakerSelector.SelectedIndex = 2;
            if (e.Key == Key.D4)
                speakerSelector.SelectedIndex = 3;
            if (e.Key == Key.D5)
                speakerSelector.SelectedIndex = 4;
            if (e.Key == Key.D6)
                speakerSelector.SelectedIndex = 5;


            if (e.Key == Key.NumPad3)
            {                
                BG32Channels[0].Volume = 0.04f;
                BG32Channels[1].Volume = 0.04f;
                BG32Channels[2].Volume = 0.06f;
                BG32Channels[3].Volume = 0.06f;
                BG32Channels[4].Volume = 0.02f;
                BG32Channels[5].Volume = 0.02f;
            }
            if (e.Key == Key.NumPad1)
            {
                BG32Channels[0].Volume = 0.06f;
                BG32Channels[1].Volume = 0.02f;
                BG32Channels[2].Volume = 0.04f;
                BG32Channels[3].Volume = 0.04f;
                BG32Channels[4].Volume = 0.04f;
                BG32Channels[5].Volume = 0.02f;
            }
            if (e.Key == Key.NumPad2)
            {
                BG32Channels[0].Volume = 0.02f;
                BG32Channels[1].Volume = 0.06f;
                BG32Channels[2].Volume = 0.04f;
                BG32Channels[3].Volume = 0.04f;
                BG32Channels[4].Volume = 0.02f;
                BG32Channels[5].Volume = 0.04f;
            }
            if (e.Key == Key.NumPad4)
            {
                BG32Channels[0].Volume = 0.04f;
                BG32Channels[1].Volume = 0.02f;
                BG32Channels[2].Volume = 0.02f;
                BG32Channels[3].Volume = 0.02f;
                BG32Channels[4].Volume = 0.06f;
                BG32Channels[5].Volume = 0.04f;
            }
            if (e.Key == Key.NumPad5)
            {
                BG32Channels[0].Volume = 0.02f;
                BG32Channels[1].Volume = 0.04f;
                BG32Channels[2].Volume = 0.02f;
                BG32Channels[3].Volume = 0.02f;
                BG32Channels[4].Volume = 0.04f;
                BG32Channels[5].Volume = 0.06f;
            }
            if (e.Key == Key.NumPad0)
            {
                BG32Channels[0].Volume = 0.03f;
                BG32Channels[1].Volume = 0.03f;
                BG32Channels[2].Volume = 0.03f;
                BG32Channels[3].Volume = 0.03f;
                BG32Channels[4].Volume = 0.03f;
                BG32Channels[5].Volume = 0.03f;
            }
        
            UpdateVolumeSliders();
            Log.Log(e.Key.ToString());
        }
        
        private void UpdateVolumeSliders()
        {
            if (BG32Channels == null) return;

            switch (selectedChannels)
            {
                case 6:
                    Volume1.Value = BG32Channels[0].Volume * 100;
                    Volume2.Value = BG32Channels[1].Volume * 100;
                    Volume3.Value = BG32Channels[2].Volume * 100;
                    Volume4.Value = BG32Channels[3].Volume * 100;
                    Volume5.Value = BG32Channels[4].Volume * 100;
                    Volume6.Value = BG32Channels[5].Volume * 100;
                    break;
                case 2:
                    Volume1.Value = BG32Channels[0].Volume * 100;
                    Volume2.Value = BG32Channels[1].Volume * 100;
                    break;
                case 1:
                    Volume1.Value = BG32Channels[0].Volume * 100;
                    break;
            }
        }

        void gestureHandler(string detected, Point displacement)
        {
            //scratchout, circle, spiral, x, square, tap_up 
            if (previous_event == "tap_up" || previous_event == "tap_down" && (Math.Abs(displacement.Y) < Math.Abs(displacement.X)))
            {
                if ((detected == "swipe_left" || detected == "swipe_right" || detected == "swipe_up" || detected == "swipe_front" || detected == "swipe_up" || detected == "swipe_back") && displacement.X > 0)
                {
                    if (BG32Channels == null || asioOut == null) return;
                    fileIndex--;
                    if (fileIndex < 0)
                        fileIndex = files.Count() - 1;

                    TrackList.SelectedIndex = fileIndex;

                    asioOut.Pause();
                    for (int i = 0; i < selectedChannels; i++)
                    {
                        oldVolume[i] = BG32Channels[i].Volume;
                        var tempfile = new Mp3FileReader(files[fileIndex].FullName);
                        var temp = new WaveChannel32(tempfile);
                        temp.PadWithZeroes = false;
                        temp.Volume = oldVolume[i];
                        MixerList[i].RemoveInputStream(BG32Channels[i]);
                        BG32Channels[i] = temp;
                        BGMusicList[i] = tempfile;
                        MixerList[i].AddInputStream(temp);
                        MixerList[i].Seek(0, 0);
                    }
                    selectedFile.Text = files[fileIndex].Name;
                    asioOut.Play();
                }

                else if ((detected == "swipe_left" || detected == "swipe_right" || detected == "swipe_up" || detected == "swipe_back" || detected == "swipe_front" || detected == "swipe_down") && displacement.X < 0)
                {
                    if (BG32Channels == null || asioOut == null) return;
                    fileIndex++;
                    if (fileIndex >= files.Count())
                        fileIndex = 0;

                    TrackList.SelectedIndex = fileIndex;

                    asioOut.Pause();
                    for (int i = 0; i < selectedChannels; i++)
                    {
                        oldVolume[i] = BG32Channels[i].Volume;
                        var tempfile = new Mp3FileReader(files[fileIndex].FullName);
                        var temp = new WaveChannel32(tempfile);
                        temp.PadWithZeroes = false;
                        temp.Volume = oldVolume[i];
                        MixerList[i].RemoveInputStream(BG32Channels[i]);
                        BG32Channels[i] = temp;
                        BGMusicList[i] = tempfile;
                        MixerList[i].AddInputStream(temp);
                        MixerList[i].Seek(0, 0);
                    }
                    selectedFile.Text = files[fileIndex].Name;
                    asioOut.Play();
                }
            }

            if (detected == "vertical_circle")
            {
                if (BG32Channels == null) return;
                if (isMuted)
                {
                    for (int i = 0; i < BG32Channels.Count(); i++)
                        BG32Channels[i].Volume = oldVolume[i];
                    isMuted = false;
                }
                else
                {
                    oldVolume = new List<float>();
                    for (int i = 0; i < BG32Channels.Count(); i++)
                    {
                        oldVolume.Add(BG32Channels[i].Volume);
                        BG32Channels[i].Volume = 0;
                    }
                    isMuted = true;
                }                
            }

            if (detected == "horizontal_circle")
            {
                if (BG32Channels == null) return;
                if (isPaused)
                {
                    for (int i = 0; i < BG32Channels.Count(); i++)
                    {
                        BG32Channels[i].Volume = pauseVolume[i];
                        BG32Channels[i].Position = pausePosition[i];
                    }
                    isPaused = false;
                }
                else
                {
                    pauseVolume = new List<float>();
                    pausePosition = new List<long>();
                    for (int i = 0; i < BG32Channels.Count(); i++)
                    {
                        pausePosition.Add(BG32Channels[i].Position);
                        pauseVolume.Add(BG32Channels[i].Volume);
                        BG32Channels[i].Volume = 0;
                    }
                    isPaused = true;
                }
            }

            if (previous_event == "scratchout" || previous_event == "vertical_x" || previous_event == "horizontal_x" || readyforgesture)
            {
                if(detected == "swipe_left" || detected == "swipe_right" || detected == "swipe_up" || detected == "swipe_back" || detected == "swipe_down" || detected == "swipe_front")
                {
                    if (Math.Abs(displacement.Y) > Math.Abs(displacement.X) && displacement.Y > 0)
                    {
                        BG32Channels[0].Volume = 0.04f;
                        BG32Channels[1].Volume = 0.04f;
                        BG32Channels[2].Volume = 0.06f;
                        BG32Channels[3].Volume = 0.06f;
                        BG32Channels[4].Volume = 0.02f;
                        BG32Channels[5].Volume = 0.02f;
                    }
                    else if (Math.Abs(displacement.X) >= Math.Abs(displacement.Y) && displacement.Y > 4.5)
                    {
                        if (displacement.X > 0)
                        {
                            BG32Channels[0].Volume = 0.06f;
                            BG32Channels[1].Volume = 0.02f;
                            BG32Channels[2].Volume = 0.04f;
                            BG32Channels[3].Volume = 0.04f;
                            BG32Channels[4].Volume = 0.04f;
                            BG32Channels[5].Volume = 0.02f;
                        }
                        else
                        {
                            BG32Channels[0].Volume = 0.02f;
                            BG32Channels[1].Volume = 0.06f;
                            BG32Channels[2].Volume = 0.04f;
                            BG32Channels[3].Volume = 0.04f;
                            BG32Channels[4].Volume = 0.02f;
                            BG32Channels[5].Volume = 0.04f;
                        }
                    }
                    else 
                    {
                        if (displacement.X > 0)
                        {
                            BG32Channels[0].Volume = 0.04f;
                            BG32Channels[1].Volume = 0.02f;
                            BG32Channels[2].Volume = 0.02f;
                            BG32Channels[3].Volume = 0.02f;
                            BG32Channels[4].Volume = 0.06f;
                            BG32Channels[5].Volume = 0.04f;
                        }
                        else
                        {
                            BG32Channels[0].Volume = 0.02f;
                            BG32Channels[1].Volume = 0.04f;
                            BG32Channels[2].Volume = 0.02f;
                            BG32Channels[3].Volume = 0.02f;
                            BG32Channels[4].Volume = 0.04f;
                            BG32Channels[5].Volume = 0.06f;
                        }
                    }                    

                }
            }
            readyforgesture = false;
            colorBox.Background = new SolidColorBrush(Colors.Red);
            previous_event = detected;
            UpdateVolumeSliders();

        }

        public Point selectedSpeaker()
        {
            double sumX=0, sumY=0;
            for (int i = 0; i < history[0].Count; i++)
            {
                sumX += (double)history[0][i]*Math.Sin(KF[0].speakerTheta * Math.PI / 180.0);
                sumX += (double)history[1][i] * Math.Sin(KF[1].speakerTheta * Math.PI / 180.0);
                sumY += (double)history[0][i] * Math.Cos(KF[0].speakerTheta * Math.PI / 180.0);
                sumY += (double)history[1][i] * Math.Cos(KF[1].speakerTheta * Math.PI / 180.0);               
            }
            double angle = 180 * Math.Atan2(sumY, sumX) / Math.PI;
            Console.WriteLine("Angles " + angle + " X " + sumX + " Y " + sumY);

            return new Point(sumX, sumY);
        }

        private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender.Equals(Volume1))
                BG32Channels[0].Volume = Math.Max(0.000001f,(float)e.NewValue / 100);
            if (sender.Equals(Volume2))
                BG32Channels[1].Volume = Math.Max(0.000001f, (float)e.NewValue / 100);
            if (sender.Equals(Volume3))
                BG32Channels[2].Volume = Math.Max(0.000001f, (float)e.NewValue / 100);
            if (sender.Equals(Volume4))
                BG32Channels[3].Volume = Math.Max(0.000001f, (float)e.NewValue / 100);
            if (sender.Equals(Volume5))
                BG32Channels[4].Volume = Math.Max(0.000001f, (float)e.NewValue / 100);
            if (sender.Equals(Volume6))
                BG32Channels[5].Volume = Math.Max(0.000001f,(float)e.NewValue / 100);
        }     

    }
}
