using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;

namespace ActionVisualizer
{
    // KeyFrequency stores the information from each channel, including the extracted bandwidth shift.
    // It also carries out a majority of the calculations specified in Soundwave in classify()
    class KeyFrequency
    {
        // frequency stores the key frequency of the tone.
        // channel stores the channel number (speaker number).
        // radius is the number of bins around the key tone to extract bandwidth shift from.
        // prior is the extracted bandwidth shift from the previous frame
        public int frequency;
        public int channel;
        public int radius;
        public int prior;

        // speakerTheta and speakerAltitude specify the relative angles between the baseline of the 
        // speakers and the microphone. 55/-55 works well enough for laptops. 
        public double speakerTheta;
        public double speakerAltitude;

        // data is a subarray of the full frequency spectrum. The midpoint is key frequency bin.
        public double[] data;

        // for checking for multiple moving objects
        public bool isBoth;

        // state is the major velocity, inverse_state is the smaller velocity.
        public int state { get; set; }
        public int inverse_state { get; set; }

        // Euclidean representation (x,y,z) of the extracted velocity.
        public double x;
        public double y;
        public double z;
       
        public KeyFrequency() { }
        
        // Empty constructor, rarely used.
        public KeyFrequency(int f, int c, int rad, double[] array) {
            this.frequency = f;
            this.channel = c;
            this.radius = rad;
            this.data = new double[radius * 2 + 1];
            Array.Copy(array, this.data, radius * 2 + 1 );

            isBoth = false;
            inverse_state = 0;
            prior = 0;
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

            isBoth = false;
            inverse_state = 0;
            prior = 0;
            state = this.classify();
        }
        

        public KeyFrequency(int f, int c, int rad, double[] indata, int center, int priori)
        {
            this.frequency = f;
            this.channel = c;
            this.radius = rad;
            this.data = new double[radius * 2 + 1];

            //rescale data to 0.0 to 1.0, to remove volume depencies.

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

            isBoth = false;
            inverse_state = 0;

            //semi standard speaker positions that work for a generic laptop and home theatre.
            switch (c)
            {
                case 1:
                    speakerTheta = 55;
                    speakerAltitude = -10;
                    break;
                case 2:
                    speakerTheta = -55;
                    speakerAltitude = -10;
                    break;
                case 3:
                    speakerTheta = 0;
                    speakerAltitude = -10;
                    break;
                case 4:
                    speakerTheta = 0;
                    speakerAltitude = 0;
                    break;
                case 5:
                    speakerTheta = 110;
                    speakerAltitude = 30;
                    break;
                case 6:
                    speakerTheta = -110;
                    speakerAltitude = 30;
                    break;

            }
            this.prior = priori;

            //After creating storing the data, determine the bandwidth shift of the speaker
            state = this.classify();           
        }

        // Conversion from amplitude to decibels.
        public float mag2db(ComplexF y)
        {
            return 20.0f * (float)Math.Log10(Math.Sqrt(y.Re * y.Re + y.Im * y.Im)/.02);
        }

        public override string ToString()
        {
            string sto = "";
            for (int i = 0; i < this.data.Length; i++)
                sto = sto + " " + (i + 1) + ":" + this.data[i];

            return sto;
        }

        // Extracts movement from the data
        public int classify()
        {
            // Empirically determined threshold.
            double thresh = .1;

            // We are looking for bandwidth shifts both towards(-) and away(+) around the key tone. 
            bool towards = false;
            bool away = false;
            int velocityT = 0;
            int velocityA = 0;
            
            // Soundwave states to ignore the first 30-40 hz (~3 bins) because of random noise.
            for (int i = 4; i < radius; i++)
            {    
                // We look in both directions for velocities that exceed our thresh value.
                // We then set the corresponding velocity_ variable to the maximum distance 
                // from the center that exceeds the threshold. 
                if (data[radius - i] > thresh)
                {
                    velocityA = -1*(i - 3);
                    away = true;
                }
                if (data[radius + i] > thresh)
                {
                    velocityT = i - 3;
                    towards = true;
                }   
            }
            // If there is movement in both directions, set the flag to true, 
            // and if the previous major velocity is in the same direction, 
            // assume that current one is still the gesture's intended direction.
            if ((towards == true) && (away == true))
            {
                isBoth = true;
                if (Math.Abs(velocityA) > velocityT)
                {
                    if (prior > 0)
                    {
                        inverse_state = velocityA;
                        updateXY(velocityT);
                        return velocityT;
                    }
                    inverse_state = velocityT;
                    updateXY(velocityA);
                    return velocityA;
                }
                else
                {
                    if (prior < 0)
                    {
                        inverse_state = velocityT;
                        updateXY(velocityA);
                        return velocityA;
                    }
                    inverse_state = velocityA;
                    updateXY(velocityT);
                    return velocityT;
                }
            }
            // If only one direction is detected, return it.
            if (towards)
            {
                updateXY(velocityT);
                return velocityT;
            }
            if (away)
            {
                updateXY(velocityA);
                return velocityA;
            }
            return 0;
        }

        // Calculate Euclidean representation from velocity, as described in paper.
        private void updateXY(int velocity)
        {
            this.x = (double)velocity * Math.Sin(speakerTheta * Math.PI / 180.0) * Math.Cos(speakerAltitude * Math.PI / 180.0);
            this.y = (double)velocity * Math.Cos(speakerTheta * Math.PI / 180.0) * Math.Cos(speakerAltitude * Math.PI / 180.0);
            this.z = (double)velocity * Math.Sin(speakerAltitude * Math.PI / 180.0);
        }
    }
}
