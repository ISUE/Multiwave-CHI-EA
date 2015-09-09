using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GestureTests.Gesture;
using GestureTests.Data;

namespace GestureTests
{
    class Program
    {
        static void Main(string[] args)
        {
            ExperimentControl exp = new ExperimentControl();
            exp.RunExperiments();
            //Console.Read(); 
            DataLoader.ExportFeaturesAsARFF(Config.DataPath, Config.WekaOutputPath);
        }       
    }
}
