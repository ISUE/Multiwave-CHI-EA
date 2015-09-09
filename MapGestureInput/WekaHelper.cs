using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GestureTests;
using GestureTests.Data;
using GestureTests.Experiment;
using GestureTests.Gesture;
using GestureTests.Types;
using GestureTests.Util;
using weka.classifiers;
using weka.core;


namespace MapGestureInput
{
    class WekaHelper
    {
        static GestureSample GS;
        static Classifier cls;

        static ExperimentControl EC;

        public static void initialize()
        {
            //read classifier
            cls = (Classifier)weka.core.SerializationHelper.read(Config.Classifier2D);           
            EC = new ExperimentControl();
            EC.Initialize();
        }

        public static string Classify(bool useRubine, float duration, bool righthandedness, List<float> SpeakerAngles, PointCollection pointHist, StylusPointCollection S, List<List<int>> hist, List<List<int>> ihist)
        {
            List<Vector2> InterpretedPoints = new List<Vector2>();
            List<Vector2> StylusPoints = new List<Vector2>();
            List<Vector2> VelocityHistory = new List<Vector2>();
            List<Vector2> InverseVelocityHistory = new List<Vector2>();
            foreach(Point P in pointHist)
                InterpretedPoints.Add(new Vector2((float)P.X,(float)P.Y));
            foreach(StylusPoint P in S)
                StylusPoints.Add(new Vector2((float)P.X,(float)P.Y));
            for (int i = 0; i < hist[0].Count; i++)
            {
                VelocityHistory.Add(new Vector2(hist[0][i], hist[1][i]));
                InverseVelocityHistory.Add(new Vector2(ihist[0][i], ihist[1][i]));
            }
            GS = new GestureSample(GestureTests.Types.GestureType.unknown, righthandedness,duration,SpeakerAngles,InterpretedPoints,StylusPoints,VelocityHistory,InverseVelocityHistory);
            GS.ComputeFeatures(GestureFeatures.PointsStroke);

            if (useRubine)
                return EC.Recognizer.Classify(GS).ToString();
            WriteARFF();

            Instances test = new Instances(new java.io.FileReader("outfile.arff"));
            //Instances test = new Instances(new java.io.FileReader("../../../data_arff/u001-training.arff"));
            test.setClassIndex(0);

            double clsLabel = cls.classifyInstance(test.instance(0));
            test.instance(0).setClassValue(clsLabel);

            return ((GestureType2D)((int)clsLabel+1)).ToString();
        }

        private static void WriteARFF()
        {
            //create file
            StreamWriter file = File.CreateText("outfile.arff");

            //write arff header
            file.Write("@RELATION " + "TestSample" + "\r\r");

            //file.Write("@ATTRIBUTE username STRING\r");
            file.Write("@ATTRIBUTE GestureType  {swipe_left,swipe_right,swipe_up,swipe_down,tap_left,tap_right,tap_up,tap_down,scratchout,circle,square,x,c,two_handed_fb,two_handed_lr,unknown}\r");
            //file.Write("@ATTRIBUTE Angle1 NUMERIC\r");
            //file.Write("@ATTRIBUTE Angle2 NUMERIC\r");
            file.Write("@ATTRIBUTE MinX NUMERIC\r");
            file.Write("@ATTRIBUTE MinY NUMERIC\r");
            file.Write("@ATTRIBUTE MaxX NUMERIC\r");
            file.Write("@ATTRIBUTE MaxY NUMERIC\r");
            file.Write("@ATTRIBUTE MeanX NUMERIC\r");
            file.Write("@ATTRIBUTE MeanY NUMERIC\r");
            file.Write("@ATTRIBUTE MedianX NUMERIC\r");
            file.Write("@ATTRIBUTE MedianY NUMERIC\r");
            file.Write("@ATTRIBUTE DiagonalLength NUMERIC\r");
            file.Write("@ATTRIBUTE StartAngleSinXY NUMERIC\r");
            file.Write("@ATTRIBUTE StartAngleCosXY NUMERIC\r");
            file.Write("@ATTRIBUTE FirstLastAngleSinXY NUMERIC\r");
            file.Write("@ATTRIBUTE FirstLastAngleCosXY NUMERIC\r");
            file.Write("@ATTRIBUTE TotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE AbsoluteTotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE SquaredTotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE FirstLastDistance NUMERIC\r");
            file.Write("@ATTRIBUTE TotalDistance NUMERIC\r");
            file.Write("@ATTRIBUTE MaxAccelerationSquared NUMERIC\r");
            file.Write("@ATTRIBUTE ZeroCrossingsX NUMERIC\r");
            file.Write("@ATTRIBUTE ZeroCrossingsY NUMERIC\r");
            /*file.Write("@ATTRIBUTE Inverse_MinX NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_MinY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_MaxX NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_MaxY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_MeanX NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_MeanY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_MedianX NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_MedianY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_DiagonalLength NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_StartAngleSinXY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_StartAngleCosXY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_FirstLastAngleSinXY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_FirstLastAngleCosXY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_TotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_AbsoluteTotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_SquaredTotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_FirstLastDistance NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_TotalDistance NUMERIC\r");
            file.Write("@ATTRIBUTE Inverse_MaxAccelerationSquared NUMERIC\r");/*
            /*file.Write("@ATTRIBUTE Velocity_MinX NUMERIC\r");
            file.Write("@ATTRIBUTE Velocity_MinY NUMERIC\r");
            file.Write("@ATTRIBUTE Velocity_MaxX NUMERIC\r");
            file.Write("@ATTRIBUTE Velocity_MaxY NUMERIC\r");
            file.Write("@ATTRIBUTE Velocity_MeanX NUMERIC\r");
            file.Write("@ATTRIBUTE Velocity_MeanY NUMERIC\r");
            file.Write("@ATTRIBUTE Velocity_MedianX NUMERIC\r");
            file.Write("@ATTRIBUTE Velocity_MedianY NUMERIC\r");
            file.Write("@ATTRIBUTE InverseVelocity_MinX NUMERIC\r");
            file.Write("@ATTRIBUTE InverseVelocity_MinY NUMERIC\r");
            file.Write("@ATTRIBUTE InverseVelocity_MaxX NUMERIC\r");
            file.Write("@ATTRIBUTE InverseVelocity_MaxY NUMERIC\r");
            file.Write("@ATTRIBUTE InverseVelocity_MeanX NUMERIC\r");
            file.Write("@ATTRIBUTE InverseVelocity_MeanY NUMERIC\r");
            file.Write("@ATTRIBUTE InverseVelocity_MedianX NUMERIC\r");
            file.Write("@ATTRIBUTE InverseVelocity_MedianY NUMERIC\r");*/
            file.Write("@ATTRIBUTE Stroke_MinX NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_MinY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_MaxX NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_MaxY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_MeanX NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_MeanY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_MedianX NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_MedianY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_DiagonalLength NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_StartAngleSinXY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_StartAngleCosXY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_FirstLastAngleSinXY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_FirstLastAngleCosXY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_TotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_AbsoluteTotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_SquaredTotalAngleXY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_FirstLastDistance NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_TotalDistance NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_MaxAccelerationSquared NUMERIC\r");
            /*file.Write("@ATTRIBUTE Stroke_ZeroCrossingsX NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_ZeroCrossingsY NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_BoundingBoxHeight NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_BoundingBoxWidth NUMERIC\r");*/
            file.Write("@ATTRIBUTE Stroke_SideRatioFirst NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_SideRatioLast NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_TopRatioFirst NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_TopRatioLast NUMERIC\r");
            /*file.Write("@ATTRIBUTE Stroke_AspectRatio NUMERIC\r");
            file.Write("@ATTRIBUTE Stroke_Regression NUMERIC\r");*/
            file.Write("@ATTRIBUTE Duration NUMERIC\r");


            //write out the feature vector for this sample
            file.Write("\r@DATA\r");

            string datum = GS.Gesture.ToString();
            for (int i = 0; i < GS.Features.Length; ++i)
                datum += "," + GS.Features[i];
            file.Write(datum + "\r");
            file.Flush();
            file.Close();
        }
    }
}
