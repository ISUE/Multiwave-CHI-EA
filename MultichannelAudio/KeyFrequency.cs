using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;

namespace MultichannelAudio
{
    class KeyFrequency
    {
        public int frequency;
        public int channel;
        public int radius;
        public double speakerTheta;
        public double[] data;

        public int state { get; set; }

        public KeyFrequency() { }

        public KeyFrequency(int f, int c, int rad, double[] array) {
            this.frequency = f;
            this.channel = c;
            this.radius = rad;
            this.data = new double[radius * 2 + 1];
            Array.Copy(array, this.data, radius * 2 + 1 );
        }

        public KeyFrequency(int f, int c, int rad, ComplexF[] indata, int center)
        {
            this.frequency = f;
            this.channel = c;
            this.radius = rad;
            this.data = new double[radius * 2 + 1];
            
            //rescale numbers >=0

            double min = Double.PositiveInfinity;
            double max = Double.NegativeInfinity;
            for (int i = 0; i < this.data.Length; i++)
            {
                this.data[i] = mag2db(indata[center-rad+i]);
                if (this.data[i] < min)
                    min = this.data[i];
            }
            for (int i = 0; i < this.data.Length; i++)
            {
                this.data[i] -= min;
                if (this.data[i] > max)
                    max = this.data[i];
            }
            for (int i = 0; i < this.data.Length; i++)
                this.data[i] /= max;

            state = this.classify();
        }
        
        public KeyFrequency(int f, int c, int rad, double[] indata, int center)
        {
            this.frequency = f;
            this.channel = c;
            this.radius = rad;
            this.data = new double[radius * 2 + 1];

            //rescale numbers >=0

            double min = Double.PositiveInfinity;
            double max = Double.NegativeInfinity;
            for (int i = 0; i < this.data.Length; i++)
            {
                this.data[i] = indata[center - rad + i];
                if (this.data[i] < min)
                    min = this.data[i];
            }
            for (int i = 0; i < this.data.Length; i++)
            {
                this.data[i] -= min;
                if (this.data[i] > max)
                    max = this.data[i];
            }
            for (int i = 0; i < this.data.Length; i++)
                this.data[i] /= max;

            state = this.classify();
        }

        public float mag2db(ComplexF y)
        {
            return 20.0f * (float)Math.Log10(Math.Sqrt(y.Re * y.Re + y.Im * y.Im));
        }

        public override string ToString()
        {
            string sto = "";
            for (int i = 0; i<this.data.Length; i++)
                sto = sto + " " + (i+1) + ":" + this.data[i];

            return sto;
        }

        //Heuristics classification
        public int classify()
        {
            double thresh = .5;
            bool towards = false;
            bool away = false;
            int velocity = 0; ;
            for (int i = 4; i < radius; i++)
            {
                if (data[radius - i] > thresh)
                {
                    velocity = i - 3;
                    away = true;
                }
                if (data[radius + i] > thresh)
                {
                    velocity = i - 3;
                    towards = true;
                }
            }
            if (towards == away)
                return 0;
            if (towards)
                return 1*velocity;
            if (away)
                return -1*velocity;
            return 0;
        }
    }
}
