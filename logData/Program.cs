using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using GestureTests;
using GestureTests.Data;
using GestureTests.Experiment;
using GestureTests.Gesture;
using GestureTests.Types;
using GestureTests.Util;


namespace logData
{
    class Program
    {

        static GestureSample GS;
        static ExperimentControl EC;
        static string filepath = "../../../ilog/";
       
        static void Main(string[] args)
        {
            initialize();           
            FileInfo[] files;
            String searchPattern = "*MapGesture.txt";
            //String searchPattern = "*MediaCenter.txt";
            DirectoryInfo di = new DirectoryInfo(@"..\..\..\dlog\");
            files = di.GetFiles(searchPattern);

            List<float> angles = (new float[] { 55, -55, 0, 0, 110, -110 }).ToList();
            List<float> elevations = (new float[] { -10, -10, -10, 0, 30, 30 }).ToList();
            StreamWriter outfile = new StreamWriter("dma.txt");

            foreach (FileInfo curr in files)
            {
                StreamReader reader = File.OpenText(curr.FullName);
                //Console.WriteLine(curr.FullName);
                float count = 0;
                float correct = 0;
                while (!reader.EndOfStream)
                {
                    string gesture = null;                                       
                    int channels;
                    int length;
                    List<List<int>> velocityData = new List<List<int>>();
                    List<List<int>> iVelocityData = new List<List<int>>();
                    string line = reader.ReadLine();
                    //end of file
                    if (line == null) continue;

                    //skip lines starting with '#' are comments
                    if (line.StartsWith("#")) continue;

                    //ignore empty lines
                    if (line == "") continue;

                    //split the line by the 'space' character
                    string[] tokens = line.Split(" ".ToCharArray());
                        //the first token provides information about the type of data to follow
                    if(tokens[0] == "Detected:")
                    {
                        count++;
                        gesture = tokens[1];
                        line = reader.ReadLine();
                        tokens = line.Split(" ".ToCharArray());
                        channels = Int32.Parse(tokens[1]);
                        line = reader.ReadLine();
                        tokens = line.Split(" ".ToCharArray());
                        length = Int32.Parse(tokens[1]);
                        line = reader.ReadLine();
                        tokens = line.Split(" ".ToCharArray());
                        if (tokens[0] == "Data:")
                        {
                            for (int i = 0; i < channels; i++)
                            {
                                line = reader.ReadLine();
                                tokens = line.Split(" ".ToCharArray());
                                velocityData.Add(new List<int>());
                                iVelocityData.Add(new List<int>());
                                for (int j = 0; j < length; j++)
                                {
                                    velocityData[i].Add(Int32.Parse(tokens[j]));
                                    iVelocityData[i].Add(0);
                                }
                                line = reader.ReadLine();
                            }
                        }
                        /*
                        Console.WriteLine(gesture);
                        Console.WriteLine(channels);
                        Console.WriteLine(length);
                        Console.WriteLine(velocityData[0]);
                        Console.ReadKey();
                            * */
                        float duration = 47 * length;
                        bool righthanded = true;
                        PointCollection points = new PointCollection();
                        Point3DCollection points3D = new Point3DCollection();
                        for (int k = 0; k < length; k++)
                        {
                            Point P = new Point(0, 0);
                            Point3D P3D = new Point3D(0, 0,0);
                            for (int i = 0; i < channels; i++)
                            {
                                P.X += (double)velocityData[i][k] * Math.Sin(angles[i] * Math.PI / 180.0) * Math.Cos(elevations[i] * Math.PI / 180.0);
                                P.Y += (double)velocityData[i][k] * Math.Cos(angles[i] * Math.PI / 180.0) * Math.Cos(elevations[i] * Math.PI / 180.0);
                                P3D.X = P.X;
                                P3D.Y = P.Y;
                                P3D.Z += (double)velocityData[i][k] * Math.Sin(elevations[i] * Math.PI / 180.0);
                            }
                            points.Add(P);
                            points3D.Add(P3D);
                        }

                        StylusPointCollection S = new StylusPointCollection();
                        S.Add(new StylusPoint(180f, 134.34f));
                        for (int i = 0; i < points.Count; i++)
                        {
                            S.Add(new StylusPoint(S[i].X - points[i].X, S[i].Y - points[i].Y));
                        }
                        string result;
                        if (Config.Use3DMode == false)
                            result = ClassifyGesture(duration, righthanded, angles, points, S, velocityData, iVelocityData);
                        else
                            result = ClassifyGesture3D(duration, righthanded, angles, elevations, points3D, velocityData, iVelocityData);

                        if (simpleCompare(result, gesture))
                            correct++;
                        //Console.WriteLine("Expected:\t" + result + "\tDetected:\t" + gesture);
                    }                        
                }
                outfile.WriteLine(curr.Name + ",\t" + correct + ",\t" + count + ",\t" + (correct / count));
            }
            outfile.Flush();
        }

        public static void initialize()
        {        
            EC = new ExperimentControl();
            EC.Initialize();
        }
        
        /// <summary>
        /// Converts a gesture name in text to a 'GestureType'.
        /// </summary>
        /// <param name="gestureName"></param>
        /// <returns></returns>
        private GestureType ReadGestureType(string gestureName)
        {
            switch (gestureName)
            {
                case "swipe_left": return GestureType.swipe_left;
                case "swipe_right": return GestureType.swipe_right;
                case "swipe_up": return GestureType.swipe_up;
                case "swipe_down": return GestureType.swipe_down;
                case "swipe_front": return GestureType.swipe_front;
                case "swipe_back": return GestureType.swipe_back;
                case "tap_left": return GestureType.tap_left;
                case "tap_right": return GestureType.tap_right;
                case "tap_up": return GestureType.tap_up;
                case "tap_down": return GestureType.tap_down;
                case "tap_front": return GestureType.tap_front;
                case "tap_back": return GestureType.tap_back;
                case "scratchout": return GestureType.scratchout;
                case "square": return GestureType.square;
                case "x": return GestureType.x;
                case "c": return GestureType.c;
                case "circle": return GestureType.circle;
                case "two_handed_fb": return GestureType.two_handed_fb;
                case "two_handed_lr": return GestureType.two_handed_lr;
                case "horizontal_circle": return GestureType.horizontal_circle;
                case "vertical_circle": return GestureType.vertical_circle;
                case "spiral": return GestureType.spiral;
                case "arm_lift": return GestureType.arm_lift;
                case "arm_drop": return GestureType.arm_drop;
                case "triangle": return GestureType.triangle;
                case "z": return GestureType.z;
            }

            return GestureType.unknown;
        }

        public static string ClassifyGesture(float duration, bool righthandedness, List<float> SpeakerAngles, PointCollection pointHist, StylusPointCollection S, List<List<int>> hist, List<List<int>> ihist)
        {
            List<Vector2> InterpretedPoints = new List<Vector2>();
            List<Vector2> StylusPoints = new List<Vector2>();
            List<Vector2> VelocityHistory = new List<Vector2>();
            List<Vector2> InverseVelocityHistory = new List<Vector2>();
            
            foreach (Point P in pointHist)
                InterpretedPoints.Add(new Vector2((float)P.X, (float)P.Y));
            foreach (StylusPoint P in S)
                StylusPoints.Add(new Vector2((float)P.X, (float)P.Y));
            for (int i = 0; i < hist[0].Count; i++)
            {
                VelocityHistory.Add(new Vector2(hist[0][i], hist[1][i]));
                InverseVelocityHistory.Add(new Vector2(ihist[0][i], ihist[1][i]));
            }
            GS = new GestureSample(GestureTests.Types.GestureType.unknown, righthandedness, duration, SpeakerAngles, InterpretedPoints, StylusPoints, VelocityHistory, InverseVelocityHistory);
            GS.ComputeFeatures(GestureFeatures.PointsStroke);

            return EC.Recognizer.Classify(GS).ToString();
        }

        public static string ClassifyGesture3D(float duration, bool righthandedness, List<float> SpeakerAngles, List<float> SpeakerElevations, Point3DCollection point3DHist, List<List<int>> hist, List<List<int>> ihist)
        {
            List<Vector3> InterpretedPoints = new List<Vector3>();
            List<Vector3> StylusPoints = new List<Vector3>();
            List<Vector3> VelocityHistory = new List<Vector3>();
            List<Vector3> InverseVelocityHistory = new List<Vector3>();
            foreach (Point3D P in point3DHist)
                InterpretedPoints.Add(new Vector3((float)P.X, (float)P.Y, (float)P.Z));

            Vector3 origin = new Vector3(0, 0, 0);
            StylusPoints.Add(origin);
            for (int i = 0; i < InterpretedPoints.Count(); i++)
            {
                origin.X += InterpretedPoints[i].X;
                origin.Y += InterpretedPoints[i].Y;
                origin.Z += InterpretedPoints[i].Z;
                StylusPoints.Add(origin);
            }

            for (int i = 0; i < hist[0].Count; i++)
            {
                VelocityHistory.Add(new Vector3(hist[0][i], hist[1][i], hist[2][i]));
                InverseVelocityHistory.Add(new Vector3(ihist[0][i], ihist[1][i], ihist[2][i]));
            }
            //GestureSample(GestureType gesture, bool rightHanded, float duration, List<float> angles, List<float> elevations, List<Vector3> interpretedPoints, List<Vector3> strokePoints, List<Vector3> velocities, List<Vector3> inverseVelocities)
            GS = new GestureSample(GestureTests.Types.GestureType.unknown, righthandedness, duration, SpeakerAngles, SpeakerElevations, InterpretedPoints, StylusPoints, VelocityHistory, InverseVelocityHistory);
            GS.ComputeFeatures(GestureFeatures.PointsStroke);

            return EC.Recognizer.Classify(GS).ToString();  
        }

        private List<Vector2> GenerateStroke(List<Vector2> interpretedPoints)
        {
            List<Vector2> S = new List<Vector2>();
            S.Add(new Vector2(180f, 134.34f));
            for (int i = 0; i < interpretedPoints.Count; i++)
            {
                S.Add(new Vector2(S[i].X - interpretedPoints[i].X, S[i].Y - interpretedPoints[i].Y));
            }

            return S;
        }

        private List<Vector3> Generate3DStroke(List<Vector3> interpretedPoints)
        {
            List<Vector3> S = new List<Vector3>();
            S.Add(new Vector3(0, 0, 0));
            for (int i = 0; i < interpretedPoints.Count; i++)
            {
                S.Add(new Vector3(S[i].X + interpretedPoints[i].X, S[i].Y + interpretedPoints[i].Y, S[i].Z + interpretedPoints[i].Z));
            }

            return S;
        }

        static bool simpleCompare(string a, string b)
        {
            if (a == "swipe_left" || a == "swipe_right" || a == "swipe_front" || a == "swipe_back" || a == "swipe_up" || a == "swipe_down")
                a = "swipe";
            if (b == "swipe_left" || b == "swipe_right" || b == "swipe_front" || b == "swipe_back" || b == "swipe_up" || b == "swipe_down")
                b = "swipe";
            if (a == "tap_left" || a == "tap_right" || a == "tap_front" || a == "tap_back" || a == "tap_up" || a == "tap_down")
                a = "tap";
            if (b == "tap_left" || b == "tap_right" || b == "tap_front" || b == "tap_back" || b == "tap_up" || b == "tap_down")
                b = "tap";
            return (a == b);
        }

    }
}
