using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalibrationTDOA
{
    struct coord
    {
        public double x, y, z;
    }

    struct coeff
    {
        public double A,B,C,D;
    }

    //Kinect Microphone
    /*speed of sound in air is 345.952 m/s
     * 34595.2 cm/s
     * each bin is one ms
     * centimeters
        In text form, the coordinates (x,y) of each microphone in cm are:
        From center
        channel 1: (11.3, 2) -> (0,0,0)
        channel 2: (-3.6, 2) -> (-14.9,0,0)
        channel 3: (-7.6, 2) -> (-18.9,0,0)
        channel 4: (-11.3, 2) -> (-22.6,0,0)
    */

    /* Black wire is 0, Red is 1, White is 2, Blue is 3
       New measurements using new arrangement
     * channel 1: (0,0,0)
     * channel 2: (9.125,3.875,7.375) -> (23.1775    9.8425   18.7325)
     * channel 3: (9.125,3.875,7.375) -> (22.86    -7.62   0)
     * channel 4: (6.625,-0.5,2.75) -> (16.8275   -1.2700    6.9850)
    */
    class KinectGeometry
    {
        //Kinect microphone array
        coord P1 = new coord() { x = 0, y = 0, z = 0 };
        coord P2 = new coord() { x = -14.9, y = 0, z = 0 };
        coord P3 = new coord() { x = -18.9, y = 0, z = 0 };
        coord P4 = new coord() { x = -22.6, y = 0, z = 0 };

        //speed of sound at 75F in cm/s
        double v = 34595.2;

        List<coeff> coeffMicrophone = new List<coeff>();
        List<coord> speakerCoordinates = new List<coord>();
        public void findNewSpeaker(List<float[]> data, double deltaT)
        {
            /*
            double ti = 67335898; double tk = 86023981; double tj = 78283279; double tl = 75092320;
            double xi = 0; double xj = 0; double xk = -15338349; double xl = -18785564;
            double yi = 26566800; double yj = 6380000; double yk = 15338349; double yl = 18785564;
            double zi = 0; double zj = 25789348; double zk = 15338349; double zl = 0;

            double rij = Math.Abs((100000 * (ti - tj)) / 333564);
            double rik = Math.Abs((100000 * (ti - tk)) / 333564);
            double rkj = Math.Abs((100000 * (tk - tj)) / 333564);
            double rkl = Math.Abs((100000 * (tk - tl)) / 333564);*/
            
            double xi = 0; double xj = -14.9; double xk = -18.9; double xl = -22.6;
            double yi = 0; double yj = 0; double yk = 0; double yl = 0;
            double zi = 0; double zj = 0; double zk = 0; double zl = 0;
            double v = 34595.2;
                                  
            //Working xy-plane code
            double distance = deltaT * v * crosscorrelation(data.ElementAt(3), data.ElementAt(0));
            double angle = (180 / Math.PI) * Math.Asin(Math.Sin(distance / 22.6));
            Console.WriteLine(distance + " " + angle);
            
            /*
             * Black wire is 1, Red is 2, White is 3, Blue is 4
            * channel 1: (0,0,0)
            * channel 2: (23.1775    9.8425   18.7325)
            * 
            * channel 3: (16.8275   -1.2700    6.9850)
            * channel 4: (22.86    -7.62   0)
            */
            /*
            double xi = 0; double xj = 23.1775; double xk = 16.8275; double xl = 22.86;
            double yi = 0; double yj = 9.8425; double yk = -1.2700; double yl = -7.62;
            double zi = 0; double zj = 18.7325; double zk = 6.9850; double zl = 0;
            double v = 34595.2;
            
            double xji = xj-xi; double xki = xk-xi; double xjk=xj-xk; double xlk=xl-xk;
	        double xok = xi-xk; double yji = yj-yi; double yki=yk-yi; double yjk=yj-yk;
	        double ylk = yl-yk; double yik = yi-yk; double zji=zj-zi; double zki=zk-zi;
	        double zik = zi-zk; double zjk = zj-zk; double zlk=zl-zk;

            double rij = v * deltaT * Math.Abs(crosscorrelation(data.ElementAt(1), data.ElementAt(0)));
            double rik = v * deltaT * Math.Abs(crosscorrelation(data.ElementAt(2), data.ElementAt(0)));
            double rkj = v * deltaT * Math.Abs(crosscorrelation(data.ElementAt(2), data.ElementAt(1)));
            double rkl = v * deltaT * Math.Abs(crosscorrelation(data.ElementAt(3), data.ElementAt(2)));
            
            Console.WriteLine(rij);
            Console.WriteLine(rik);
            Console.WriteLine(rkj);
            Console.WriteLine(rkl);         
            
            double s9 = rik * xji - rij * xki; double s10 = rij * yki - rik * yji; double s11 = rik * zji - rij * zki;
            double s12 = (rik * (rij * rij + xi * xi - xj * xj + yi * yi - yj * yj + zi * zi - zj * zj)
                        - rij * (rik * rik + xi * xi - xk * xk + yi * yi - yk * yk + zi * zi - zk * zk)) / 2;

            double s13 = rkl * xjk - rkj * xlk; double s14 = rkj * ylk - rkl * yjk; double s15 = rkl * zjk - rkj * zlk;
            double s16 = (rkl * (rkj * rkj + xk * xk - xj * xj + yk * yk - yj * yj + zk * zk - zj * zj)
                        - rkj * (rkl * rkl + xk * xk - xl * xl + yk * yk - yl * yl + zk * zk - zl * zl)) / 2;

            double a =  s9 / s10; double b = s11 / s10; double c = s12 / s10; double d = s13 / s14;
            double e = s15 / s14; double f = s16 / s14; double g = (e - b) / (a - d); double h = (f - c) / (a - d);
            double i = (a * g) + b; double j = (a * h) + c;
            double k = rik * rik + xi * xi - xk * xk + yi * yi - yk * yk + zi * zi - zk * zk + 2 * h * xki + 2 * j * yki;
            double l = 2 * (g * xki + i * yki + zki);
            double m = 4 * rik * rik * (g * g + i * i + 1) - l * l;
            double n = 8 * rik * rik * (g * (xi - h) + i * (yi - j) + zi) + 2 * l * k;
            double o = 4 * rik * rik * ((xi - h) * (xi - h) + (yi - j) * (yi - j) + zi * zi) - k * k;
            double s28 = n / (2 * m); double s29 = (o / m); double s30 = (s28 * s28) - s29;

            double root = Math.Sqrt(s30);

            Console.WriteLine(s28);
            Console.WriteLine(s29);
            Console.WriteLine(s30);
            Console.WriteLine(root);

            double z1 = s28 + root;
            double z2 = s29 - root;
            double x1 = g * z1 + h;
            double x2 = g * z2 + h;
            double y1 = a * x1 + b * z1 + c;
            double y2 = a * x2 + b * z2 + c;
            Console.WriteLine("Coordinates 1: " + x1 + " " + y1 + " " + z1);
            Console.WriteLine("Coordinates 2: " + x2 + " " + y2 + " " + z2);*/
        }
        public int crosscorrelation(float[] a, float[] b)
        {
            /*if(a.Length != b.Length)
                return 0;*/
            double meanA = a.Average();
            double meanB = b.Average();

            int maxdelay = 20;

            double max = 0;
            int maxIndex=0;
            
            double sx = 0, sy = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sx += (a[i] - meanA) * (a[i] - meanA);
                sy += (b[i] - meanB) * (b[i] - meanB);
            }
            double denom = Math.Sqrt(sx * sy);

            for (int delay = -maxdelay; delay < maxdelay; delay++)
            {
                double sxy = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    int j = i + delay;
                    if (j < 0 || j >= a.Length)
                        continue;
                    else
                        sxy += (a[i] - meanA) * (b[j] - meanB);
                }
                double r = sxy / denom;

                if (r > max)
                {
                    max = r;
                    maxIndex = delay;
                }
                /* r is the correlation coefficient at "delay" */

            }

            return maxIndex;
        }
    }
}
