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
    public class XYZFeatures
    {
        private float MinX = 0, MinY = 0, MinZ = 0;
        private float MaxX = 0, MaxY = 0, MaxZ = 0;
        private float meanX = 0, meanY = 0, meanZ = 0;
        private float MedianX = 0, MedianY = 0, MedianZ = 0;
        private float DiagonalLength = 0;
        private float StartAngleSinXY = 0;
        private float StartAngleCosXY = 0;
        private float StartAngleSinXZ = 0;
        private float FirstLastAngleSinXY = 0;
        private float FirstLastAngleCosXY = 0;
        private float FirstLastAngleSinXZ = 0;
        private float TotalAngleXY = 0;
        private float TotalAngleXZ = 0;
        private float AbsTotalAngleXY = 0;
        private float AbsTotalAngleXZ = 0;
        private float SquaredTotalAngleXY = 0;
        private float SquaredTotalAngleXZ = 0;
        private float FirstLastDistance = 0;
        private float TotalDistanceTraversed = 0;
        private float MaxAccelerationSquared = 0;
        private float zeroCrossingsX = -1, zeroCrossingsY = -1, zeroCrossingsZ = -1;

        public XYZFeatures(List<Vector3> points)
        {
            Compute(points);
        }

        private void Compute(List<Vector3> pts)
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
        }

        private void ComputeMinMaxMedianMeanDistances(List<Vector3> pts)
        {
            MinX = MinY = MinZ = float.PositiveInfinity;
            MaxX = MaxY = MaxZ = float.NegativeInfinity;
            meanX = meanY = meanZ = TotalDistanceTraversed = 0.0f;
            FirstLastDistance = MedianX = MedianY = MedianZ = DiagonalLength = float.NaN;
            int N = pts.Count;
            float lastSigX = 0, lastSigY = 0, lastSigZ = 0;

            for (int i = 0; i < N; ++i)
            {
                if (pts[i].X > MaxX) MaxX = pts[i].X;
                if (pts[i].Y > MaxY) MaxY = pts[i].Y;
                if (pts[i].Z > MaxZ) MaxZ = pts[i].Z;

                if (pts[i].X < MinX) MinX = pts[i].X;
                if (pts[i].Y < MinY) MinY = pts[i].Y;
                if (pts[i].Z < MinZ) MinZ = pts[i].Z;

                meanX += pts[i].X;
                meanY += pts[i].Y;
                meanZ += pts[i].Z;

                if (i > 0)
                    TotalDistanceTraversed += Distance(pts[i].X, pts[i].Y, pts[i].Z, pts[i - 1].X, pts[i - 1].Y, pts[i - 1].Z);

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
                    if (pts[i].Z != 0 && ((pts[i].Z * lastSigZ) <= 0))
                    {
                        lastSigZ = pts[i].Z;
                        zeroCrossingsZ++;
                    }
                }
            }

            MedianX = pts[N / 2].X;
            MedianY = pts[N / 2].Y;
            MedianZ = pts[N / 2].Z;

            meanX /= (float)N;
            meanY /= (float)N;
            meanZ /= (float)N;

            DiagonalLength = Distance(MinX, MinY, MinZ, MaxX, MaxY, MaxZ);
            FirstLastDistance = Distance(pts[0].X, pts[0].Y, pts[0].Z, pts[N - 1].X, pts[N - 1].Y, pts[N - 1].Z);
        }

        private void ComputeAngles(List<Vector3> pts)
        {
            StartAngleCosXY = StartAngleSinXY = StartAngleSinXZ = float.NaN;
            FirstLastAngleCosXY = FirstLastAngleSinXY = FirstLastAngleSinXZ = float.NaN;
            TotalAngleXY = TotalAngleXZ = 0.0f;
            AbsTotalAngleXY = AbsTotalAngleXZ = 0.0f;
            SquaredTotalAngleXY = SquaredTotalAngleXZ = 0.0f;

            int N = pts.Count;

            float dx, dy, dz;

            for (int i = 1; i < N; ++i)
            {
                dx = pts[i].X - pts[i - 1].X;
                dy = pts[i].Y - pts[i - 1].Y;
                dz = pts[i].Z - pts[i - 1].Z;

                float angleXY = (float)Math.Atan2(dy, dx);
                float angleXZ = (float)Math.Atan2(dz, dx);

                TotalAngleXY += angleXY;
                TotalAngleXZ += angleXZ;

                AbsTotalAngleXY += Math.Abs(angleXY);
                AbsTotalAngleXZ += Math.Abs(angleXZ);

                SquaredTotalAngleXY += (angleXY * angleXY);
                SquaredTotalAngleXZ += (angleXZ * angleXZ);
            }


            //start angle stuff
            dx = pts[2].X - pts[0].X;
            dy = pts[2].Y - pts[0].Y;
            dz = pts[2].Z - pts[0].Z;

            float startAngleXY = (float)Math.Atan2(dy, dx);
            float startAngleXZ = (float)Math.Atan2(dz, dx);

            StartAngleSinXY = (float)Math.Sin(startAngleXY);
            StartAngleCosXY = (float)Math.Cos(startAngleXY);
            StartAngleSinXZ = (float)Math.Sin(startAngleXZ);


            //end angle stuff
            dx = pts[N - 1].X - pts[0].X;
            dy = pts[N - 1].Y - pts[0].Y;
            dz = pts[N - 1].Z - pts[0].Z;

            float firstLastAngleXY = (float)Math.Atan2(dy, dx);
            float firstLastAngleXZ = (float)Math.Atan2(dz, dx);

            FirstLastAngleSinXY = (float)Math.Sin(firstLastAngleXY);
            FirstLastAngleCosXY = (float)Math.Cos(firstLastAngleXY);
            FirstLastAngleSinXZ = (float)Math.Sin(firstLastAngleXZ);
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
        private float Distance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return (float)Math.Sqrt((x1 - x2) * (x1 - x2) +
                                    (y1 - y2) * (y1 - y2) +
                                    (z1 - z2) * (z1 - z2));
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
                    MinX,MinY,MinZ,
                    MaxX,MaxY,MaxZ,
                    meanX,meanY,meanZ,
                    MedianX,MedianY,MedianZ,
                    DiagonalLength,
                    StartAngleSinXY,StartAngleCosXY,StartAngleSinXZ,
                    FirstLastAngleSinXY,FirstLastAngleCosXY,FirstLastAngleSinXZ,
                    TotalAngleXY,TotalAngleXZ,
                    AbsTotalAngleXY,AbsTotalAngleXZ,
                    SquaredTotalAngleXY,SquaredTotalAngleXZ,
                    FirstLastDistance,
                    TotalDistanceTraversed,
                    MaxAccelerationSquared,
                    zeroCrossingsX,
                    zeroCrossingsY,
                    zeroCrossingsZ
                };
            }
        }

        /// <summary>
        /// Feature vector containing features computed from angular velocity values from a MotionPlus attached to a Nintendo Wiimote.
        /// </summary>
        public float[] VelocityFeatures
        {
            get
            {
                return new float[]
                {
                    MinX,MinY,MinZ,
                    MaxX,MaxY,MaxZ,
                    meanX,meanY,meanZ,
                    MedianX,MedianY,MedianZ
                };
            }
        }

    }
}
