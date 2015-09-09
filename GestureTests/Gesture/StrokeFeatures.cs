using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureTests.Util;

/*
 
 Author: Salman Cheema
 University of Central Florida
 
 Email: salmanc@cs.ucf.edu
 
 Released as part of the 3D Gesture Database analysed in
 
 "Salman Cheema, Michael Hoffman, Joseph J. LaViola Jr., 3D Gesture classification with linear acceleration and angular velocity 
 sensing devices for video games, Entertainment Computing, Volume 4, Issue 1, February 2013, Pages 11-24, ISSN 1875-9521, 10.1016/j.entcom.2012.09.002"
 
 */

namespace GestureTests.Gesture
{
    /// <summary>
    /// Helper class used to compute features given a set of data points from a Nintendo Wiimote and/or MotionPlus.
    /// </summary>
    public class StrokeFeatures
    {
        private float MinX = 0, MinY = 0;
        private float MaxX = 0, MaxY = 0;
        private float meanX = 0, meanY = 0;
        private float MedianX = 0, MedianY = 0;
        private float DiagonalLength = 0;
        private float StartAngleSinXY = 0;
        private float StartAngleCosXY = 0;        
        private float FirstLastAngleSinXY = 0;
        private float FirstLastAngleCosXY = 0;        
        private float TotalAngleXY = 0;        
        private float AbsTotalAngleXY = 0;        
        private float SquaredTotalAngleXY = 0;        
        private float FirstLastDistance = 0;
        private float TotalDistanceTraversed = 0;
        private float MaxAccelerationSquared = 0;
        private float zeroCrossingsX = -1, zeroCrossingsY = -1;

        private float BoundingBoxHeight = 0, BoundingBoxWidth = 0;
        private float SideRatioFirst = 0, SideRatioLast = 0;
        private float TopRatioFirst = 0, TopRatioLast = 0;
        private float AspectRatio = 0;
        private float Regression = 0;

        public StrokeFeatures(List<Vector2> points)
        {
            Compute(points);
        }

        private void Compute(List<Vector2> pts)
        {
            MaxAccelerationSquared = float.NegativeInfinity;

            for (int i = 0; i < pts.Count; ++i)
            {
                float aa = pts[i].LengthSquared();
                if (aa > MaxAccelerationSquared)
                    MaxAccelerationSquared = aa;
            }

            ComputeMinMaxMedianMeanDistances(pts);
            ComputeAngles(pts);
            ComputeStrokeFeatures(pts);
        }

        private void ComputeMinMaxMedianMeanDistances(List<Vector2> pts)
        {
            MinX = MinY = float.PositiveInfinity;
            MaxX = MaxY = float.NegativeInfinity;
            meanX = meanY = TotalDistanceTraversed = 0.0f;
            FirstLastDistance = MedianX = MedianY = DiagonalLength = float.NaN;
            int N = pts.Count;
            float lastSigX = 0, lastSigY = 0;

            for (int i = 0; i < N; ++i)
            {
                if (pts[i].X > MaxX) MaxX = pts[i].X;
                if (pts[i].Y > MaxY) MaxY = pts[i].Y;

                if (pts[i].X < MinX) MinX = pts[i].X;
                if (pts[i].Y < MinY) MinY = pts[i].Y;

                meanX += pts[i].X;
                meanY += pts[i].Y;

                if (i > 0)
                    TotalDistanceTraversed += Distance(pts[i].X, pts[i].Y, pts[i - 1].X, pts[i - 1].Y);
                
                if (i > 0 && i < N - 1)
                {
                    if (pts[i].X != 0 && ((pts[i].X * lastSigX) <= 0))
                    {
                        lastSigX = pts[i].X;
                        zeroCrossingsX++;
                    }
                    if (pts[i].Y != 0 && ((pts[i].Y * lastSigY) <= 0))
                    {
                        lastSigY = pts[i].Y;
                        zeroCrossingsY++;
                    }
                }
            }

            MedianX = pts[N / 2].X;
            MedianY = pts[N / 2].Y;

            meanX /= (float)N;
            meanY /= (float)N;

            DiagonalLength = Distance(MinX, MinY, MaxX, MaxY);
            FirstLastDistance = Distance(pts[0].X, pts[0].Y,  pts[N - 1].X, pts[N - 1].Y);
        }

        private void ComputeAngles(List<Vector2> pts)
        {
            StartAngleCosXY = StartAngleSinXY = float.NaN;
            FirstLastAngleCosXY = FirstLastAngleSinXY = float.NaN;
            TotalAngleXY = 0.0f;
            AbsTotalAngleXY = 0.0f;
            SquaredTotalAngleXY = 0.0f;

            int N = pts.Count;

            float dx, dy;

            for (int i = 1; i < N; ++i)
            {
                dx = pts[i].X - pts[i - 1].X;
                dy = pts[i].Y - pts[i - 1].Y;

                float angleXY = (float)Math.Atan2(dy, dx);

                TotalAngleXY += angleXY;

                AbsTotalAngleXY += Math.Abs(angleXY);

                SquaredTotalAngleXY += (angleXY * angleXY);
            }


            //start angle stuff
            dx = pts[2].X - pts[0].X;
            dy = pts[2].Y - pts[0].Y;

            float startAngleXY = (float)Math.Atan2(dy, dx);

            StartAngleSinXY = (float)Math.Sin(startAngleXY);
            StartAngleCosXY = (float)Math.Cos(startAngleXY);


            //end angle stuff
            dx = pts[N - 1].X - pts[0].X;
            dy = pts[N - 1].Y - pts[0].Y;

            float firstLastAngleXY = (float)Math.Atan2(dy, dx);

            FirstLastAngleSinXY = (float)Math.Sin(firstLastAngleXY);
            FirstLastAngleCosXY = (float)Math.Cos(firstLastAngleXY);
        }

        private void ComputeStrokeFeatures(List<Vector2> pts)
        {           
            BoundingBoxWidth = MaxX - MinX;
            BoundingBoxHeight = MaxY - MinY;
            AspectRatio = BoundingBoxWidth / BoundingBoxHeight;

            SideRatioFirst = (pts.First().X - MinX)/BoundingBoxWidth;
            SideRatioLast = (pts.Last().X - MinX)/BoundingBoxWidth;
            TopRatioFirst = (pts.First().Y - MinY)/BoundingBoxHeight;
            TopRatioLast = (pts.Last().Y - MinY)/BoundingBoxHeight;
            Regression = FitLine(pts);
        }

        private float FitLine(List<Vector2> pts)
        {
            float n = pts.Count;
            float x1 = pts.Sum(x => x.X);
            float y1 = pts.Sum(x => x.Y);
            float x2 = pts.Sum(x => x.X*x.X);
            float y2 = pts.Sum(x => x.Y*x.Y);
            float xy1 = pts.Sum(x => x.X * x.Y);
            float x3 = x2 - (x1 * x1) / n;
            float y3 = y2 - (y1 * y1) / n;
            float xy2 = xy1 - (x1 * y1) / n;
            float radius = (float) Math.Sqrt((x3 - y3) * (x3 - y3) + 4 * xy2 * xy2);
            float error = (x3 + y3 - radius) / 2;
            float rms = (float)Math.Sqrt(error / n);
            
            float a,b,c = 0;
            if (x3 > y3)
            {
                a = -2 * xy2;
                b = x3 - y3 + radius;
            }
            else if (x3 < y3)
            {
                a = x3 - y3 + radius;
                b = -2 * xy2;
            }
            else
            {
                if (xy2 == 0)
                {
                    a = b = c = 0;
                    error = float.PositiveInfinity;
                }
                else
                {
                    a = 1;
                    b = -1;
                }
            }

            float mag = (float)Math.Sqrt(a * a + b * b);
            if (mag != 0)
            {
                c = (-1 * a * x1 - b * y1) / n / mag;
                a = a / mag;
                b = b / mag;
            }
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            foreach (Vector2 p in pts)
            {
                error = a * p.X + b * p.Y + c;
                float pX = p.X - a * error;
                float pY = p.Y - b * error;
                float ploc = -b * pX + b * pY;
                min = Math.Min(min, ploc);
                max = Math.Max(max, ploc);
            } 
            return (100 * rms) / (max - min);
        }

        /// <summary>
        /// Euclidean distance between two 3D points.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        /// <returns></returns>
        private float Distance(double x1, double y1, double x2, double y2)
        {
            return (float)Math.Sqrt((x1 - x2) * (x1 - x2) +
                                    (y1 - y2) * (y1 - y2));
        }

        /// <summary>
        /// Feature vector containing features computed from acceleration values from a Nintendo Wiimote.
        /// </summary>
        public float[] NormalFeatures
        {
            get
            {
                return new float[]
                {
                    MinX,MinY,
                    MaxX,MaxY,
                    meanX,meanY,
                    MedianX,MedianY,
                    DiagonalLength,
                    StartAngleSinXY,StartAngleCosXY,
                    FirstLastAngleSinXY,FirstLastAngleCosXY,
                    TotalAngleXY,
                    AbsTotalAngleXY,
                    SquaredTotalAngleXY,
                    FirstLastDistance,
                    TotalDistanceTraversed,
                    MaxAccelerationSquared,                    
                    //BoundingBoxHeight,
                    //BoundingBoxWidth,
                    SideRatioFirst,
                    SideRatioLast,
                    TopRatioFirst,
                    TopRatioLast,
                    //AspectRatio,
                    //Regression                  
                };
            }
        }
    }
}
