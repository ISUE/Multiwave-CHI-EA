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

namespace GestureTests.Types
{
    /// <summary>
    /// An enumeration of the different types of features that are used for training/recognition.
    /// </summary>
    public enum GestureFeatures
    {
        Points,
        PointsStroke,
        PointsStrokeInverse,
        PointsVelocities,
        PointsVelocitiesInverseVelocities,
        PointsStrokesVelocitiesInverseVelocities
    }
}