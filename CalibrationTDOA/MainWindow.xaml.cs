using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio;
using NAudio.Wave;
using NAudio.Dsp;
using Exocortex.DSP;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;

namespace CalibrationTDOA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        //private WaveOut waveOut;
        private AsioOut asioOut;
        private WaveIn waveIn;

        public int waveOutChannels;
        
        public int hz = 50;
        public int inSampleRate = 16000;
        public int bits = 16;
        public int channels = 4;
        public int buffersize;
        public int[] bin;
        public List<float[]> sampledata;        
        
        double[] priori;

        EnumerableDataSource<int> bins;
        List<EnumerableDataSource<float>> rawIn;

        int selectedChannels = 1;
        List<int> frequencies;
        List<int> centerbins;

        StreamWriter svmOutStream = null;
        //List<StreamWriter> Foutstream;

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
            waveIn.BufferMilliseconds = 1000/hz;
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormatExtensible(inSampleRate, bits, channels);
            waveIn.DataAvailable += waveIn_DataAvailable;
            buffersize = waveIn.WaveFormat.AverageBytesPerSecond / (bits/8) / hz;           

            try
            {
                waveIn.StartRecording();
            }
            catch(NAudio.MmException e)
            {
                Console.WriteLine(e.ToString() + "\nPlug in a microphone!");
            }

            bin = new int[buffersize / channels];                        
            for (int j = 0; j < buffersize/channels; j++)
            {
                bin[j] = j;            
            }
            
            chart1.Viewport.Visible = new DataRect(0, -1.0, buffersize/channels, 2.0);
            
            bins = new EnumerableDataSource<int>(bin);
            bins.SetXMapping(x => x);

            rawIn = new List<EnumerableDataSource<float>>();
            sampledata = new List<float[]>();
            List<CompositeDataSource> comp1 = new List<CompositeDataSource>(channels);
            for(int i = 0; i < channels; i++)
            {
                sampledata.Add(new float[buffersize / channels]);            
                rawIn.Add(new EnumerableDataSource<float>(sampledata.ElementAt(i)));
                rawIn.ElementAt(i).SetYMapping(y => y);
                comp1.Add(new CompositeDataSource(bins, rawIn.ElementAt(i)));
                chart1.AddLineGraph(comp1.ElementAt(i));
            }
               
        }

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (bits == 32)
            {
                for (int index = 0; index < buffersize; index++)
                {
                    int sample = (int)((e.Buffer[index * 4 + 3] << 24) |
                                            (e.Buffer[index * 4 + 2] << 16) |
                                            (e.Buffer[index * 4 + 1] << 8) |
                                            (e.Buffer[index * 4 + 0]));
                    //Console.WriteLine(sample);
                    float sample32 = sample / 2147483648f;
                    sampledata[index%channels][index/channels] = sample32;
                }
            }
            else
            {
                for (int index = 0; index < buffersize; index++)
                {
                    short sample = (short)((e.Buffer[index * 2 + 1] << 8) |
                                            (e.Buffer[index * 2 + 0]));                    
                    float sample16 = sample / 32768f;
                    sampledata[index%channels][index / channels] = sample16;
                }
            }
            //if (asioOut != null)
            //    Console.WriteLine(KinectGeometry.crosscorrelation(sampledata.ElementAt(0), sampledata.ElementAt(1)));

            if (graphing)
            {
                bins = new EnumerableDataSource<int>(bin);
                bins.SetXMapping(x => x);

                chart1.Children.RemoveAll<LineGraph>();
                List<CompositeDataSource> comp1 = new List<CompositeDataSource>(channels);
                for (int i = 0; i < channels; i++)
                {
                    rawIn[i] = new EnumerableDataSource<float>(sampledata.ElementAt(i));
                    rawIn[i].SetYMapping(y => y);
                    comp1.Add(new CompositeDataSource(bins, rawIn[i]));
                    chart1.AddLineGraph(comp1.ElementAt(i),selectColor(i));
                }                
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartStopSineWave();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            KinectGeometry K = new KinectGeometry();
            K.findNewSpeaker(sampledata,(double) 1.0/inSampleRate);
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
                    if (c != (selectedChannels - 1))
                    {
                        inputs.Add(new SineWaveProvider32(0, 0.25f, 44100, 1));
                        frequencies.Add(0);
                        centerbins.Add((int)Math.Round((0) / 10.768));
                    }
                    else
                    {
                        inputs.Add(new SineWaveProvider32(600 , 0.25f, 44100, 1));
                        frequencies.Add(300);
                        centerbins.Add((int)Math.Round((600 ) / 10.768));
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
                //waveOut = new WaveOut();
                //waveOut.Init(sineWaveProvider);                    
                //waveOut.Init(splitter);

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
                //Foutstream.Clear();
            }
        }

        public float mag2db(ComplexF y)
        {
            return 20.0f * (float)Math.Log10(Math.Sqrt(y.Re * y.Re + y.Im * y.Im));
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
                        //Console.WriteLine(i);
                    }
                }
                priori[i] = outdata[i];
            }
            //Console.WriteLine("Mean: " + mean + " Min: " + min);
            return outdata;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (svmOutStream != null)
            {
                svmOutStream.Close();
                svmOutStream.Dispose();
            }
        }
        
        private void channelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedChannels = (sender as ComboBox).SelectedIndex + 1;
        }

        private Color selectColor(int i)
        {
            switch (i)
            {
                case 0:
                    return Colors.Red;
                case 1:
                    return Colors.Blue;
                case 2:
                    return Colors.Green;
                case 3:
                    return Colors.Orange;
                default:
                    return Colors.Black;
            }
        }
    }
}
