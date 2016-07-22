using Exocortex.DSP;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MultichannelAudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveOut waveOut;
        private WaveIn waveIn;

        public int waveOutChannels;        

        public int buffersize = 2048;
        public int[] bin;
        public float[] sampledata;
        public float[] inbetween;
        bool init_inbetween = true;
        ComplexF[] indata;

        double[] filteredindata;
        double[] priori;

        EnumerableDataSource<int> bins;
        EnumerableDataSource<float> rawIn;
        EnumerableDataSource<double> fftIn;

        int[] channelLabel;
        int[] velocity;
           

        int selectedChannels = 1;
        List<int> frequencies;
        List<int> centerbins;

        List<KeyFrequency> KF;
  
        private bool graphing = true;        

        public MainWindow()
        {         
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            int waveInDevices = WaveIn.DeviceCount;            
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);                
                Console.WriteLine("Device {0}: {1}, {2} channels",
                    waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);           
            }
            
            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 47*buffersize/2048;
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormat(44100, 32, 1);
            waveIn.DataAvailable += waveIn_DataAvailable;

            try
            {
                waveIn.StartRecording();
            }
            catch(NAudio.MmException e)
            {
                Console.WriteLine(e.ToString() + "\nPlug in a microphone!");
            }

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

            chart1.Viewport.Visible = new DataRect(0, -1.0, buffersize * 2, 2.0);            
            chart2.Viewport.Visible = new DataRect(1620, 0, 280, 110);   
                                 
            bins = new EnumerableDataSource<int>(bin);
            bins.SetXMapping(x => x);

            rawIn = new EnumerableDataSource<float>(sampledata);
            rawIn.SetYMapping(y => y);

            CompositeDataSource comp1 = new CompositeDataSource(bins, rawIn);
            chart1.AddLineGraph(comp1);

            CompositeDataSource comp2 = new CompositeDataSource(bins, rawIn);
            chart2.AddLineGraph(comp2);
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
                                        (e.Buffer[index*4 + 0]));
                
                float sample32 = sample / 2147483648f;
                
                if (index >= (buffersize - inbetween.Length))
                    sampledata[index] = inbetween[index - buffersize + inbetween.Length];
                else
                    sampledata[index] = sampledata[index+buffersize+inbetween.Length]; 
                sampledata[index+buffersize] = sample32;
               
            }

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
            if (graphing)
            {
                bins = new EnumerableDataSource<int>(bin);
                bins.SetXMapping(x => x);

                rawIn = new EnumerableDataSource<float>(sampledata);
                rawIn.SetYMapping(y => y);
                chart1.Children.RemoveAll<LineGraph>();
                CompositeDataSource comp1 = new CompositeDataSource(bins, rawIn);
                chart1.AddLineGraph(comp1, Colors.Red);

                fftIn = new EnumerableDataSource<double>(filteredindata);
                fftIn.SetYMapping(y => y);

                chart2.Children.RemoveAll<LineGraph>();
                CompositeDataSource comp2 = new CompositeDataSource(bins, fftIn);
                chart2.AddLineGraph(comp2, Colors.Red);
            }

            if (waveOut != null)
            {         
                
                KF = new List<KeyFrequency>();                
                for (int i = 0; i < frequencies.Count; i++)
                {                    
                    KF.Add(new KeyFrequency(frequencies.ElementAt(i), i + 1, 33, filteredindata, centerbins.ElementAt(i)));                    
                    velocity[i] = KF.ElementAt(i).state;

                }

            }


        }

        public void bufferFFT()
        {
            indata = new ComplexF[buffersize * 2];
            for (int i = 0; i < buffersize * 2; i++)
            {
                indata[i].Re = sampledata[i] *(float)FastFourierTransform.HammingWindow(i, buffersize * 2);
                indata[i].Im = 0;
            }
            //FastFourierTransform.FFT(true, 11, indata);
            Exocortex.DSP.Fourier.FFT(indata, buffersize * 2, Exocortex.DSP.FourierDirection.Forward);
            filteredindata = filterMean(indata,1.3);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartStopSineWave();
        }     

        private void StartStopSineWave()
        {
            if (waveOut == null)
            {
                button1.Content = "Stop Sound";
                //string str = channelSelector.Text;
                //selectedChannels = Int32.Parse(str);
                Console.WriteLine("User Selected Channels: " + selectedChannels);
                WaveOutCapabilities outdeviceInfo = WaveOut.GetCapabilities(0);                
                waveOutChannels = outdeviceInfo.Channels;
                waveOut = new WaveOut();

                int waveOutDevices = WaveOut.DeviceCount;
                for(int i = 0; i< waveOutDevices; i++)
                {
                    outdeviceInfo = WaveOut.GetCapabilities(i);
                    Console.WriteLine("Device {0}: {1}, {2} channels",
                            i, outdeviceInfo.ProductName, outdeviceInfo.Channels);
                }

                List<IWaveProvider> inputs = new List<IWaveProvider>();
                frequencies = new List<int>();
                centerbins = new List<int>();
                //Foutstream = new List<StreamWriter>();
                for (int c = 0; c < selectedChannels; c++)
                {
                    inputs.Add(new SineWaveProvider32(18000+c*700, 0.25f, 44100, 1));
                    frequencies.Add(18000+c*700);
                    centerbins.Add((int)Math.Round((18000 + c * 700) / 10.768));                  
                    
                    Console.WriteLine(centerbins.ElementAt(c));                    
                }

                var splitter = new MultiplexingWaveProvider(inputs, selectedChannels);
                try
                {
                    waveOut.Init(splitter);
                    waveOut.Play();
                }
                catch(System.ArgumentException)
                {
                    Console.WriteLine("Invalid audio channel count. Please select a lower number of audio channels");
                }           
            }
            else
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
                button1.Content = "Generate Sound";

                frequencies.Clear();
                centerbins.Clear();
                //Foutstream.Clear();
            }
        }

        public float mag2db(ComplexF y)
        {
            return 20.0f * (float)Math.Log10(Math.Sqrt(y.Re * y.Re + y.Im * y.Im)/.02);
        }

        public double[] filterMean(ComplexF[] data, double factor)
        {
            double[] outdata = new double[data.Length];
            double min = Double.PositiveInfinity;
            
            for (int i = 0; i < data.Length; i++)
            {
                outdata[i] = mag2db(data[i]);
                min = Math.Min(outdata[i], min);
            }

            return outdata;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
        
        private void channelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedChannels = (sender as ComboBox).SelectedIndex + 1;            
            channelLabel = new int[selectedChannels];
            velocity = new int[selectedChannels];

            for (int i = 0; i < selectedChannels; i++)
            {
                channelLabel[i] = i + 1;
                velocity[i] = 0;
            }

        }  
    }
}
