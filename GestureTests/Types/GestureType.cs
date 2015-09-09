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
    /// An enumeration of the different types of supported gestures.
    /// </summary>

    public enum GestureType
    {
        swipe_left = 1,
        swipe_right = 2,
        swipe_up = 3,
        swipe_down = 4,
        swipe_front = 5,
        swipe_back = 6,
        tap_left = 7,
        tap_right = 8,
        tap_up = 9,
        tap_down = 10,
        tap_front = 11,
        tap_back = 12,
        scratchout = 13,
        circle = 14,
        square = 15,
        x = 16,
        c = 17,
        two_handed_fb = 18,
        two_handed_lr = 19,
        horizontal_circle = 20,
        vertical_circle = 21,        
        spiral = 22,
        arm_lift = 23,
        arm_drop = 24,
        triangle = 25,
        z = 26,
        unknown = 27
    };
    public enum GestureType2D
    {
        swipe_left = 1,
        swipe_right = 2,
        swipe_up = 3,
        swipe_down = 4,
        tap_left = 5,
        tap_right = 6,
        tap_up = 7,
        tap_down = 8,
        scratchout = 9,
        circle = 10,
        square = 11,
        x = 12,
        c = 13,
        two_handed_fb = 14,
        two_handed_lr = 15,
        unknown = 16
    };
}
