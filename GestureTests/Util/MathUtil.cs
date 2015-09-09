using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureTests.Gesture;
using GestureTests.Types;

namespace GestureTests.Util
{
    /// <summary>
    /// Helper class to aid training of the LinearClassifier. 
    /// </summary>
    public class MathUtil
    {
        /// <summary>
        /// Computes the average feature vector from a list of gesture samples.
        /// </summary>
        /// <param name="samples">list of samples.</param>
        /// <returns>An array of size 29 or 41 depending on whether only Wiimote features are to be used or both Wiimote and MotionPlus features are to be used.</returns>
        public static float[] ComputeAverageFeatureVector(List<GestureSample> samples)
        {
            int N = samples.Count;

            //initialize the mean vector to zero
            float[] avg = new float[Config.NumFeatures];
            for (int j = 0; j < Config.NumFeatures; ++j)
                avg[j] = 0.0f;

            //add up values
            for (int i = 0; i < N; ++i)
                for (int j = 0; j < Config.NumFeatures; ++j)
                    avg[j] += samples[i].Features[j];

            //divide by number of samples and return average
            for (int j = 0; j < Config.NumFeatures; ++j)
                avg[j] /= (float)N;

            return avg;
        }

        /// <summary>
        /// Computes the common covariance matrix of a series of gesture classes and returns its inverse.
        /// </summary>
        /// <param name="classes">a dataset containing training data corresponding to several gesture classes.</param>
        /// <returns>A square matrix of size 29x29 or 41x41 depending on whether only Wiimote features were used or both Wiimote and MotionPlus features were used.
        /// If the common covariance matrix is singular, then this method will return a square matrix containing all NaN values.</returns>
        public static Matrix ComputeInverseCommonCovarianceMatrix(Dictionary<GestureType, List<GestureSample>> classes)
        {
            //initialize the common covariance matrix to zero
            Matrix common_covariance_matrix = new Matrix(Config.NumFeatures);
            common_covariance_matrix.Set(0);

            int totalSamples = 0, numClasses = 0;

            foreach (GestureType classType in classes.Keys)
            {
                //get the training data for each gesture class
                List<GestureSample> ithClass = classes[classType];
                if (ithClass.Count > 0)
                {
                    //construct the covariance matrix for current gesture class and add it to the commmon matrix
                    Matrix class_covariance = ComputeCovarianceMatrix(ithClass);

                    common_covariance_matrix = common_covariance_matrix + class_covariance;
                    totalSamples += ithClass.Count;
                    numClasses++;
                }
            }

            //normalize the common covariance matrix and return its inverse
            common_covariance_matrix = common_covariance_matrix / (float)(totalSamples - numClasses);
            return common_covariance_matrix.Inverse;
        }

        /// <summary>
        /// Computes the covariance matrix for a list of gesture samples. Ideally, all samples should belong to a single gesture class
        /// but this is not a neccessary pre-requisite of calling this method.
        /// </summary>
        /// <param name="samples">list of gesture samples.</param>
        /// <returns>A square matrix of size 29x29 or 41x41, depending of whether only Wiimote features are used or both MotionPlus and Wiimote features are used.</returns>
        public static Matrix ComputeCovarianceMatrix(List<GestureSample> samples)
        {
            float[] mean_feature_vector = ComputeAverageFeatureVector(samples);
            int N = samples.Count;

            Matrix covariance = new Matrix(Config.NumFeatures);
            for (int i = 0; i < Config.NumFeatures; ++i)
            {
                //compute only the upper triangular part. M_ij can be used to populate M_ji as covariance matrices are symmetric
                for (int j = i; j < Config.NumFeatures; ++j)
                {
                    float cov_ij = 0.0f;
                    foreach (GestureSample sample in samples)
                    {
                        //distance of sample feature vector from the mean feature vector
                        float[] sample_feature_vector = sample.Features;
                        cov_ij += (sample_feature_vector[i] - mean_feature_vector[i]) * (sample_feature_vector[j] - mean_feature_vector[j]);
                    }
                    cov_ij /= (float)(N - 1);

                    covariance[i, j] = cov_ij;
                    if (i != j)
                        covariance[j, i] = cov_ij;
                }
            }

            return covariance;
        }
    }
}
