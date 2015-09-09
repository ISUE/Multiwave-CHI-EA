using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureTests.Gesture;
using GestureTests.Util;
using GestureTests.Types;

/*
 
Author: Salman Cheema
University of Central Florida
 
Email: salmanc@cs.ucf.edu
 
Released as part of the 3D Gesture Database analysed in
 
"Salman Cheema, Michael Hoffman, Joseph J. LaViola Jr., 3D Gesture classification with linear acceleration and angular velocity 
sensing devices for video games, Entertainment Computing, Volume 4, Issue 1, February 2013, Pages 11-24, ISSN 1875-9521, 10.1016/j.entcom.2012.09.002"
 
*/

namespace GestureTests.Experiment
{
    /// <summary>
    /// An implementation of Rubine's Linear Classifier, adapted to recognize 3D gestures as per the description by Hoffman & LaViola. 
    /// 
    /// Dean Rubine. 1991. Specifying gestures by example. SIGGRAPH Comput. Graph. 25, 4 (July 1991), 329-337. 
    /// DOI=10.1145/127719.122753 http://doi.acm.org/10.1145/127719.122753
    /// 
    /// Hoffman, M., Varcholik, P., and LaViola, J. "Breaking the Status Quo: Improving 3D Gesture Recognition with Spatially 
    /// Convenient Input Devices", Proceedings of IEEE Virtual Reality 2010, 59-66, March 2010
    /// </summary>
    public class LinearClassifier
    {
        /// <summary>
        /// Linear Weighing functions for each gesture class. Computed at Training Time.
        /// </summary>
        private Dictionary<GestureType, Matrix[]> Weights = new Dictionary<GestureType, Matrix[]>();

        /// <summary>
        /// A copy of the data used to train this classifier.
        /// </summary>
        private Dictionary<GestureType, List<GestureSample>> TrainingData;


        public LinearClassifier(Dictionary<GestureType, List<GestureSample>> trainingData)
        {
            TrainingData = trainingData;
            Train();
        }

        #region Gesture Training

        private void Train()
        {
            //clear weight functions
            Weights.Clear();

            try
            {
                //compute inverse of common covariance matrix for all classes
                Matrix inv = MathUtil.ComputeInverseCommonCovarianceMatrix(TrainingData);

                //compute weights for each gesture class using inverted common covariance
                foreach (GestureType gesture in Config.GesturesToUse)
                    ComputeAndStoreWeightsForGestureClass(TrainingData[gesture], gesture, inv);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private void ComputeAndStoreWeightsForGestureClass(List<GestureSample> samples,
                                                            GestureType gestureClass,
                                                            Matrix common_covariance_inverse)
        {
            //compute average feature vector for all samples in a given gesture class 
            float[] mean = MathUtil.ComputeAverageFeatureVector(samples);

            //populate matrix from mean feature vector
            Matrix means_matrix = new Matrix(Config.NumFeatures, 1, mean);

            //compute weights
            Matrix gesture_weights = (common_covariance_inverse * means_matrix).Transpose;
            Matrix gesture_weights_initial = (gesture_weights * means_matrix) * -0.5f;

            Weights.Add(gestureClass, new Matrix[] { gesture_weights_initial, gesture_weights });
        }

        #endregion


        #region gesture classification code

        /// <summary>
        /// Attempts to classify an unknown gesture sample.
        /// </summary>
        /// <param name="sample">the gesture sample to be classified</param>
        /// <returns>the best guess for the given sample</returns>
        public GestureType Classify(GestureSample sample)
        {
            float classificationScore = float.NegativeInfinity;
            GestureType classification = GestureType.unknown;

            //Compute the linear weighted function for each gesture class with the feature vector as input
            //the class whose function yields the maximum value is the classification

            foreach (GestureType gestureClass in Weights.Keys)
            {
                float classScore = ComputeScoreFor(gestureClass, sample.Features);
                //Console.WriteLine(gestureClass.ToString() + " " + classScore);
                if (classScore > classificationScore)
                {
                    classificationScore = classScore;
                    classification = gestureClass;
                }
            }

            return classification;
        }

        private float ComputeScoreFor(GestureType gestureClass, float[] unknownFeatureVector)
        {
            //Get the weight functions for 'gestureClass'
            Matrix[] m = (Matrix[])Weights[gestureClass];

            //m is 2 matrices, m[0] is a single value, access it by indexing [0,0].
            //this is the constant term for the weight function [i.e. w0 ]
            float score = m[0][0, 0];

            //m[1] is a list of values. Take the dot product of this weight function and the feature vector
            //this is equivalent to doing SUM[ x1*w1 + x2*w2 +......+   xn*wn ], xi = ith feature, wi = ith weight
            Matrix feature_vector = new Matrix(Config.NumFeatures, 1, unknownFeatureVector);
            score += (m[1] * feature_vector)[0, 0];

            return score;
        }

        #endregion

    }
}
