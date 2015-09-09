using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionVisualizer
{
    class SimpleGesture
    {
        public double angle { get; set; }
        public double magnitude { get; set; }
        public double duration { get; set; }
        public double total_stroke_angle { get; set; }
        public String gesture_name { get; set; }

        public SimpleGesture()
        {
            angle = 0;
            magnitude = 0;
            duration = 0;
            gesture_name = "unknown";
        }

        public SimpleGesture(double a, double m, double d, String name)
        {
            angle = a;
            magnitude = m;
            duration = d;
            gesture_name = name;
            total_stroke_angle = 0;
        }

        public SimpleGesture(double a, double m, double d, double tsa, String name)
        {
            angle = a;
            magnitude = m;
            duration = d;
            gesture_name = name;
            total_stroke_angle = tsa;
        }
    }

    class SimpleGesture3D
    {
        public double angle { get; set; }
        public double elevation { get; set; }
        public double magnitude { get; set; }
        public double duration { get; set; }
        public double total_stroke_angle { get; set; }
        public String gesture_name { get; set; }

        public SimpleGesture3D()
        {
            angle = 0;
            elevation = 0;
            duration = 0;
            magnitude = 0;
            gesture_name = "unknown";
        }

        public SimpleGesture3D(double a, double m, double e, double d, String name)
        {
            angle = a;
            magnitude = m;
            duration = d;
            elevation = e;
            gesture_name = name;
            total_stroke_angle = 0;
        }

        public SimpleGesture3D(double a, double m, double e, double d, double tsa, String name)
        {
            angle = a;
            magnitude = m;
            elevation = e;
            duration = d;
            gesture_name = name;
            total_stroke_angle = tsa;
        }
    }

    class ComplexGesture
    {
        private List<SimpleGesture> gestures;
        public String last_detected_gesture;
        public String last_detected_complex_gesture;
        public ComplexGesture()
        {
            gestures = new List<SimpleGesture>();
            last_detected_gesture = "unknown";
            last_detected_complex_gesture = "unknown";
       } 

        public void addGesture(double a, double m, double d, String name)
        {
            gestures.Add(new SimpleGesture(a, m, d, name));
        }

        public void addGesture(double a, double m, double d, double tsa, String name)
        {
            gestures.Add(new SimpleGesture(a, m, d, tsa, name));
        }

        public void clearGestureHistory()
        {
            last_detected_gesture = "unknown";
            gestures.Clear();
        }

        public String updateDetectedGesture()
        {
            //L
            //Square (is this gesture even useful?)
            //Circle (very hard to do as two gestures. Would need to pass more of the features and recognize as a single stroke (could just look at the length of the gesture))
            //X 
            //Scratchout
			//Need to add in a "deadzone" following the completion of a gesture that will keep users from adding spurious swipes.

            Console.WriteLine("Gesture duration = " + gestures.Last<SimpleGesture>().duration + "\nMagnitude = " + gestures.Last<SimpleGesture>().magnitude);

            int gcount = gestures.Count();
            if (gcount < 1)
                return last_detected_gesture;

            if (gestures.Last<SimpleGesture>().duration > 1200)
            {
                Console.WriteLine("Detected: Circle Gesture");
                clearGestureHistory();
                return last_detected_gesture = last_detected_complex_gesture = "circle";
            }

            if (gcount == 1)
                return last_detected_gesture = gestures.First<SimpleGesture>().gesture_name;

            if (gcount >= 2)
            {
                //double angle_between = Math.Abs((gestures.ElementAt<SimpleGesture>(0).angle + gestures.ElementAt<SimpleGesture>(1).angle) % Math.PI);
                //Console.WriteLine(angle_between);
                /*
                if (Math.Abs(Math.PI / 2 - angle_between) < (Math.PI / 12))
                {
                    Console.WriteLine("Detected: L Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = "L";                    
                }*/
                String first = gestures.ElementAt<SimpleGesture>(gcount - 2).gesture_name, second = gestures.ElementAt<SimpleGesture>(gcount - 1).gesture_name;
                double first_total_angle = gestures.ElementAt<SimpleGesture>(gcount-2).total_stroke_angle, second_total_angle = gestures.ElementAt<SimpleGesture>(gcount-1).total_stroke_angle;

                Console.WriteLine("Total Angle 1: " + first_total_angle);
                Console.WriteLine("Total Angle 2: " + second_total_angle);
                
                bool is_side_to_side = (first == "swipe_right" && second == "swipe_left") || (first == "swipe_left" && second == "swipe_right"); 
                bool is_up_to_down = (first == "swipe_up" && second == "swipe_down") || (second == "swipe_up" && first == "swipe_down");
                bool is_circular = (Math.Abs(first_total_angle) + Math.Abs(second_total_angle)) > 40;
                
                bool is_x = (first == "swipe_left" || first == "swipe_right") && (second == "tap_down" || second == "tap_up");
                is_x = is_x || ((first == "swipe_up" || first == "swipe_down") && (second == "tap_left" || second == "tap_right"));
                is_x = is_x || (second == "swipe_left" || second == "swipe_right") && (first == "tap_down" || first == "tap_up");
                is_x = is_x || ((second == "swipe_up" || second == "swipe_down") && (first == "tap_left" || first == "tap_right"));

                bool is_L = (first == "swipe_up" || first == "swipe_down") && (second == "swipe_left" || second == "swipe_right");
                bool is_sideL = (second == "swipe_up" || second == "swipe_down") && (first == "swipe_left" || first == "swipe_right");
                if (is_L || is_sideL)
                {
                    clearGestureHistory();
                    if (last_detected_complex_gesture == "L")
                    {
                        Console.WriteLine("Detected: Square Gesture");
                        return last_detected_gesture = last_detected_complex_gesture = "square";
                    }
                    else
                    {
                        Console.WriteLine("Detected: L Gesture");
                        return last_detected_gesture = last_detected_complex_gesture = "L";
                    }
                }
                /*
                if ((is_side_to_side || is_up_to_down) && is_circular)
                {
                    Console.WriteLine("Detected: Circle Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "circle";
                }
                */
                if(is_x)
                {
                    Console.WriteLine("Detected: X Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "x";
                }
            }

            if (gcount >= 3)
            {
                String first = gestures.ElementAt<SimpleGesture>(gcount - 3).gesture_name, second = gestures.ElementAt<SimpleGesture>(gcount - 2).gesture_name, third = gestures.ElementAt<SimpleGesture>(gcount - 1).gesture_name;
                bool is_first_side_swipe = (first == "tap_left" || first == "tap_right" || first == "swipe_left" || first == "swipe_right");
                bool is_second_side_swipe = (second == "tap_left" || second == "tap_right" || second == "swipe_left" || second == "swipe_right");
                bool is_third_side_swipe = (third == "tap_left" || third == "tap_right" || third == "swipe_left" || third == "swipe_right");
                if( is_first_side_swipe && is_second_side_swipe && is_third_side_swipe )
                {
                    Console.WriteLine("Detected: Scratchout Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "scratchout";
                }                
            }
            //clearGestureHistory(); 
            return "unknown";
        }

        public static bool isComplex(string s)
        {
            if (s == "circle" || s == "x" || s == "square" || s == "scratchout")
                return true;
            return false;
        }
    }

    class ComplexGesture3D
    {
        private List<SimpleGesture3D> gestures;
        public String last_detected_gesture;
        public String last_detected_complex_gesture;
        public ComplexGesture3D()
        {
            gestures = new List<SimpleGesture3D>();
            last_detected_gesture = "unknown";
            last_detected_complex_gesture = "unknown";
        }

        public void addGesture(double a, double m, double e, double d, String name)
        {
            gestures.Add(new SimpleGesture3D(a, m, e, d, name));
        }

        public void addGesture(double a, double m, double e, double d, double tsa, String name)
        {
            gestures.Add(new SimpleGesture3D(a, m, e, d, tsa, name));
        }

        public void clearGestureHistory()
        {
            last_detected_gesture = "unknown";
            gestures.Clear();
        }

        public String updateDetectedGesture()
        {
            //L
            //Square
            //Circle
            //X
            //Scratchout
			//Vertical version of all these needs to be written. perhaps that is sufficient?
		
            int gcount = gestures.Count();
            if (gcount < 1)
                return last_detected_gesture;

            if (gestures.Last<SimpleGesture3D>().duration > 1200)
            {
                string temp = gestures.Last<SimpleGesture3D>().gesture_name;
                if (temp == "swipe_up" || temp == "swipe_down" || temp == "tap_up" || temp =="tap_down")//gestures.Last<SimpleGesture3D>().elevation > Math.PI/4)
                {
                    Console.WriteLine("Detected: Vertical Circle Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "vertical_circle";
                }
                else
                {
                    Console.WriteLine("Detected: Horizontal Circle Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "horizontal_circle";
                }

            }

            if (gcount == 1)
                return last_detected_gesture = gestures.First<SimpleGesture3D>().gesture_name;

            if (gcount >= 2)
            {
                //double angle_between = Math.Abs((gestures.ElementAt<SimpleGesture>(0).angle + gestures.ElementAt<SimpleGesture>(1).angle) % Math.PI);
                //Console.WriteLine(angle_between);
                /*
                if (Math.Abs(Math.PI / 2 - angle_between) < (Math.PI / 12))
                {
                    Console.WriteLine("Detected: L Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = "L";                    
                }*/
                String first = gestures.ElementAt<SimpleGesture3D>(gcount - 2).gesture_name, second = gestures.ElementAt<SimpleGesture3D>(gcount - 1).gesture_name;
                double first_total_angle = gestures.ElementAt<SimpleGesture3D>(gcount - 2).total_stroke_angle, second_total_angle = gestures.ElementAt<SimpleGesture3D>(gcount - 1).total_stroke_angle;

                Console.WriteLine("Total Angle 1: " + first_total_angle);
                Console.WriteLine("Total Angle 2: " + second_total_angle);

                bool is_side_to_side = (first == "swipe_right" && second == "swipe_left") || (first == "swipe_left" && second == "swipe_right");
                bool is_up_to_down = (first == "swipe_up" && second == "swipe_down") || (second == "swipe_up" && first == "swipe_down");
                bool is_front_to_back = (first == "swipe_front" && second == "swipe_back") || (second == "swipe_front" && first == "swipe_back");
                bool is_circular = (Math.Abs(first_total_angle) + Math.Abs(second_total_angle)) > 40;

                bool is_x = (first == "swipe_left" || first == "swipe_right") && (second == "tap_back" || second == "tap_front");
                is_x = is_x || ((first == "swipe_front" || first == "swipe_back") && (second == "tap_left" || second == "tap_right"));
                is_x = is_x || (second == "swipe_left" || second == "swipe_right") && (first == "tap_back" || first == "tap_front");
                is_x = is_x || ((second == "swipe_front" || second == "swipe_back") && (first == "tap_left" || first == "tap_right"));

                bool is_vertical_x = ((first == "swipe_left" || first == "swipe_right") && (second == "tap_down" || second == "tap_up"));
                is_vertical_x = is_vertical_x || ((first == "swipe_up" || first == "swipe_down") && (second == "tap_left" || second == "tap_right"));
                is_vertical_x = is_vertical_x || (second == "swipe_left" || second == "swipe_right") && (first == "tap_down" || first == "tap_up");
                is_vertical_x = is_vertical_x || ((second == "swipe_up" || second == "swipe_down") && (first == "tap_left" || first == "tap_right"));


                bool is_vertical_L = ((first == "swipe_up" || first == "swipe_down") && (second == "swipe_left" || second == "swipe_right")) ||
                                     ((second == "swipe_up" || second == "swipe_down") && (first == "swipe_left" || first == "swipe_right"));
                bool is_horizontal_L = ((first == "swipe_front" || first == "swipe_back") && (second == "swipe_left" || second == "swipe_right")) ||
                                    ((first == "swipe_front" || first == "swipe_back") && (second == "swipe_left" || second == "swipe_right")); 

                if (is_horizontal_L)
                {
                    clearGestureHistory();
                    if (last_detected_complex_gesture == "horizontal_L")
                    {
                        Console.WriteLine("Detected: Horizontal Square Gesture");
                        return last_detected_gesture = last_detected_complex_gesture = "horizontal_square";
                    }
                    else
                    {
                        Console.WriteLine("Detected: Horizontal L Gesture");
                        return last_detected_gesture = last_detected_complex_gesture = "horizontal_L";
                    }
                }

                if (is_vertical_L)
                {
                    clearGestureHistory();
                    if (last_detected_complex_gesture == "vertical_L")
                    {
                        Console.WriteLine("Detected: Vertical Square Gesture");
                        return last_detected_gesture = last_detected_complex_gesture = "vertical_square";
                    }
                    else
                    {
                        Console.WriteLine("Detected: Vertical L Gesture");
                        return last_detected_gesture = last_detected_complex_gesture = "vertical_L";
                    }
                }
                /*
                if ((is_side_to_side || is_up_to_down) && is_circular)
                {
                    Console.WriteLine("Detected: Circle Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "circle";
                }*/

                if (is_x)
                {
                    Console.WriteLine("Detected: Horizontal X Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "horizontal_x";
                }
                if (is_vertical_x)
                {
                    Console.WriteLine("Detected: Vertical X Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "vertical_x";
                }
            }

            if (gcount >= 3)
            {
                String first = gestures.ElementAt<SimpleGesture3D>(gcount - 3).gesture_name, second = gestures.ElementAt<SimpleGesture3D>(gcount - 2).gesture_name, third = gestures.ElementAt<SimpleGesture3D>(gcount - 1).gesture_name;
                bool is_first_side_swipe = (first == "tap_left" || first == "tap_right" || first == "swipe_left" || first == "swipe_right");
                bool is_second_side_swipe = (second == "tap_left" || second == "tap_right" || second == "swipe_left" || second == "swipe_right");
                bool is_third_side_swipe = (third == "tap_left" || third == "tap_right" || third == "swipe_left" || third == "swipe_right");
                if (is_first_side_swipe && is_second_side_swipe && is_third_side_swipe)
                {
                    Console.WriteLine("Detected: Scratchout Gesture");
                    clearGestureHistory();
                    return last_detected_gesture = last_detected_complex_gesture = "scratchout";
                }
            }
            //clearGestureHistory(); 
            return "unknown";
        }

        public static bool isComplex(string s)
        {
            if (s == "horizontal_circle" || s == "horizontal_x" || s == "horizontal_square" || s == "scratchout")
                return true;
            if (s == "vertical_circle" || s == "vertical_x" || s == "vertical_square")
                return true;
            return false;
        }
    }
}