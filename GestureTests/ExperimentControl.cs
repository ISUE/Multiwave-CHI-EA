using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GestureTests.Gesture;
using GestureTests.Experiment;
//using GestureTests.Experiment.Dependent;
//using GestureTests.Experiment.Independent;
using GestureTests.Data;
using GestureTests.Types;

/*
 
Author: Salman Cheema
University of Central Florida
 
Email: salmanc@cs.ucf.edu
 
Released as part of the 3D Gesture Database analysed in
 
"Salman Cheema, Michael Hoffman, Joseph J. LaViola Jr., 3D Gesture classification with linear acceleration and angular velocity 
sensing devices for video games, Entertainment Computing, Volume 4, Issue 1, February 2013, Pages 11-24, ISSN 1875-9521, 10.1016/j.entcom.2012.09.002"
 
*/

namespace GestureTests
{
    /// <summary>
    /// Controller class to running the experiments.
    /// </summary>
    public class ExperimentControl
    {
        private List<UserDataSet> dataset;

        Dictionary<GestureType, List<GestureSample>> ToTrain;
        Dictionary<GestureType, List<GestureSample>> ToRecognize_Training;
        public LinearClassifier Recognizer;

        /// <summary>
        /// Entry point for setting up and running the experiments.
        /// </summary>
        public void RunExperiments()
        {           
            //load gesture data and report on the number and type of available samples
            dataset = DataLoader.LoadGestureDataFrom(Config.DataPath);

            int training = 0;
            foreach (UserDataSet user_i in dataset)
            {
                training += user_i.TrainingSamples.Count;
            }
            
            ToTrain = new Dictionary<GestureType, List<GestureSample>>();
            ToRecognize_Training = new Dictionary<GestureType, List<GestureSample>>();
            foreach (GestureType gesture in Config.GesturesToUse)
            {
                ToTrain.Add(gesture, new List<GestureSample>());
                ToRecognize_Training.Add(gesture, new List<GestureSample>());         
            }

            PopulateDataSets();            
            Recognizer = new LinearClassifier(ToTrain);

            Run();           
            Console.WriteLine();
        }

        /// <summary>
        /// Entry point for setting up and running the experiments.
        /// </summary>
        public void Initialize()
        {
            //load gesture data and report on the number and type of available samples
            dataset = DataLoader.LoadGestureDataFrom(Config.DataPath);

            int training = 0;
            foreach (UserDataSet user_i in dataset)
            {
                training += user_i.TrainingSamples.Count;
            }

            ToTrain = new Dictionary<GestureType, List<GestureSample>>();
            foreach (GestureType gesture in Config.GesturesToUse)
            {
                ToTrain.Add(gesture, new List<GestureSample>());
            }

            foreach (UserDataSet uData in dataset)
            {
                //1-construct/prune the 'ToTrain' collection for training the Classifier

                //add all training samples into the 'ToTrain' collection.
                //sort training samples into classes
                foreach (GestureSample sample in uData.TrainingSamples)
                    if (Config.GesturesToUse.Contains(sample.Gesture))
                        ToTrain[sample.Gesture].Add(sample);
            }
            Recognizer = new LinearClassifier(ToTrain);
        }

        private void PopulateDataSets()
        {
            foreach (UserDataSet uData in dataset)
            {
                //1-construct/prune the 'ToTrain' collection for training the Classifier

                //add all training samples into the 'ToTrain' collection.
                //sort training samples into classes
                foreach (GestureSample sample in uData.TrainingSamples)
                    if (Config.GesturesToUse.Contains(sample.Gesture))
                        ToTrain[sample.Gesture].Add(sample);            
            }


            //randomely remove excess samples from the 'ToTrain' collection until it meets the specified setting in the experiment configuration.
            //the removed excess samples are put in the 'ToRecognize_Training' for the experiment.
            Random randGenerator = new Random((int)DateTime.Now.Ticks);
            foreach (GestureType gesture in Config.GesturesToUse)
            {
                while ((ToTrain[gesture].Count - Config.NumTrainingSamples) != 0)
                {
                    int randomIndex = randGenerator.Next() % ToTrain[gesture].Count;
                    ToRecognize_Training[gesture].Add(ToTrain[gesture].ElementAt(randomIndex));
                    ToTrain[gesture].RemoveAt(randomIndex);
                }
            }
        }


        public void Run()
        {
            Accuracy_Correct = DoExperimentOnDataset(ToRecognize_Training, "Training Samples");            
        }

        private Dictionary<GestureType, Result> DoExperimentOnDataset(Dictionary<GestureType, List<GestureSample>> RecognitionSamples,
                                                                        string experimentName)
        {
            Dictionary<GestureType, Result> accuracy = new Dictionary<GestureType, Result>();

            Console.WriteLine("*********************************************************************************");
            Console.WriteLine("Running User Independent Experiment(" + ExperimentNo + ") On :  " + experimentName);

            //do classification on Samples
            foreach (GestureType gType in Config.GesturesToUse)
            {
                List<GestureSample> samplesToRecognize = RecognitionSamples[gType];

                accuracy.Add(gType, new Result());
                accuracy[gType].Total = samplesToRecognize.Count;

                foreach (GestureSample gSample in samplesToRecognize)
                {
                    GestureType classification = Recognizer.Classify(gSample);
                    if (classification == gType)
                        accuracy[gType].Correct++;
                }

                Console.WriteLine("Accuracy[" + gType + "] : " + accuracy[gType].Correct + "/" + accuracy[gType].Total + " ~= " + accuracy[gType].Accuracy);

            }

            //report accuracy
            float average = 0.0f;
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            foreach (GestureType gType in Config.GesturesToUse)
            {
                average += accuracy[gType].Accuracy;
                if (accuracy[gType].Accuracy > max)
                    max = accuracy[gType].Accuracy;
                if (accuracy[gType].Accuracy < min)
                    min = accuracy[gType].Accuracy;
            }

            Console.WriteLine(String.Format("accuracy: {0}, \t[Min: {1}, Max: {2}]", (average / (float)Config.GesturesToUse.Count), min, max));
            Console.WriteLine("*********************************************************************************");

            return accuracy;
        }

        public int ExperimentNo { get; protected set; }

        public Dictionary<GestureType, Result> Accuracy_Correct { get; protected set; }
        public Dictionary<GestureType, Result> Accuracy_InCorrect { get; protected set; }
    }
}