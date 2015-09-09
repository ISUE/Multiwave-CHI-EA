using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    /// A result of an experiment measuring recognition accuracy within some dataset.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Number of available data points.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Number of data points that were correctly classified.
        /// </summary>
        public int Correct { get; set; }

        /// <summary>
        /// Measured Accuracy. Computed as 'Correct x 100 / Total'. If 'Total' is zero, this is set to 100 % accuracy.
        /// </summary>
        public float Accuracy
        {
            get { return (Total > 0 ? (float)Correct * 100.0f / (float)Total : 100.0f); }
        }

        public Result()
        {
            Total = Correct = 0;
        }

        public static Result operator +(Result right, Result left)
        {
            Result newR = new Result();
            newR.Total = right.Total + left.Total;
            newR.Correct = right.Correct + left.Correct;

            return newR;
        }
    }
}
