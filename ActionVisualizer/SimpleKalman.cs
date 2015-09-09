using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;

namespace VerySimpleKalman
{
    public class VDKalman
    {
        //private static double Q = 0.000001;
        //private static double R = 0.01;
        //private static double P = 1, X = 0, K;

        //private static void measurementUpdate()
        //{
        //    K = (P + Q) / (P + Q + R);
        //    P = R * (P + Q) / (R + P + Q);
        //}

        //public static double update(double measurement)
        //{
        //    measurementUpdate();
        //    double result = X + (measurement - X) * K;
        //    X = result;
        //    return result;
        //}
        
		/*from https://code.google.com/p/efficient-java-matrix-library/wiki/KalmanFilterExamples */
		
		
        private Vector H; //Constant Measurement Vector
        private Vector K; //Kalman gain/blending factor
        private Matrix Q; //Constant process noise covariance
        private Matrix R; //Constant measurement noise covariance

        //Measurement vector is zk (don't forget)
        private Matrix F; //time matrix [1 dt; 0 1] (fundamental matrix)
        public Vector x_priori; //(p)state estimate
        private Matrix P_priori; //(p)error covariance
        
		public VDKalman(int ccount)
        {            
            R = new DenseMatrix(1);
            H = new DenseVector(2);
            Q = new DenseMatrix(2);
            F = new DenseMatrix(2);
            K = new DenseVector(2);

            P_priori = new DenseMatrix(2);
            x_priori = new DenseVector(2);
        }

        public void initialize(double speed, double dt, double np, double nq)
        {
            H[0] = 1.0;
            R[0, 0] = np;

            F[0, 0] = F[1, 1] = 1.0;
            F[0, 1] = dt;

            Q[0, 0] = Math.Pow(dt, 3) / 3.0 * speed;
            Q[0, 1] = Q[1, 0] = ((dt * dt) / 2.0) * speed;
            Q[1, 1] = dt * speed;


            P_priori[0, 0] = P_priori[1, 1] = 100;
            x_priori[0] = x_priori[1] = 0.0;
            /*for (int i = 0; i < 3; ++i) {
              _PP[i].setZeros();
              _PP[i](0,0) = _PP[i](1,1) = INITVAR;
              // zero out the state vectors
              _state[i](0,0) = _state[i](1,0) = 0.0;
            }           */
        }

        public void time_Update()
        {
            x_priori = (Vector) F.Multiply(x_priori);
            P_priori[0, 0] = F[0, 0] * (F[0, 0] * P_priori[0, 0] + F[0, 1] * P_priori[1, 0]) +
                                F[0, 1] * (F[0, 0] * P_priori[0, 1] + F[0, 1] * P_priori[1, 1]);
            P_priori[0, 1] = F[1, 0] * (F[0, 0] * P_priori[0, 0] + F[0, 1] * P_priori[1, 0]) +
                                F[1, 1] * (F[0, 0] * P_priori[0, 1] + F[0, 1] * P_priori[1, 1]);
            P_priori[1, 0] = F[0, 0] * (F[1, 0] * P_priori[0, 0] + F[1, 1] * P_priori[1, 0]) +
                                F[0, 1] * (F[1, 0] * P_priori[0, 1] + F[1, 1] * P_priori[1, 1]);
            P_priori[1, 1] = F[1, 0] * (F[1, 0] * P_priori[0, 0] + F[1, 1] * P_priori[1, 0]) +
                                F[1, 1] * (F[1, 0] * P_priori[0, 1] + F[1, 1] * P_priori[1, 1]);            

        }
		
		public Vector measurement_Update(double zk/*measure*/)
		{		
            
			// y = z - H x (residual)
            double y = zk - (H[0]*x_priori[0] + H[1]*x_priori[1]);              
			// S = H P H' + R
            double S = H[0] * H[0] * P_priori[0,0] + H[0] * H[1] *
                        (P_priori[0, 1] + P_priori[1, 0]) + P_priori[1, 1] * H[1] * H[1] + R[0,0];
			// K = PH'S^(-1)
            K[0] = P_priori[0, 0] * H[0] / S + P_priori[0,1] * H[1] / S;
            K[1] = P_priori[1, 0] * H[1] / S + P_priori[1,1] * H[1] / S;		
			// x = x + Ky
            x_priori[0] = x_priori[0] + K[0] * y;
            x_priori[1] = x_priori[1] + K[1] * y;	
	
			// P = (I-kH)P = P - KHP
            P_priori[0, 0] = ((1 - K[0] * H[0]) * P_priori[0, 0]) + ((0 - K[0] * H[1]) * P_priori[1,0]);
            P_priori[0, 1] = ((1 - K[0] * H[0]) * P_priori[0, 1]) + ((0 - K[0] * H[1]) * P_priori[1,1]);
            P_priori[1, 0] = ((0 - K[1] * H[0]) * P_priori[0, 0]) + ((1 - K[1] * H[1]) * P_priori[1,0]);
            P_priori[1, 1] = ((1 - K[1] * H[1]) * P_priori[0, 1]) + ((1 - K[1] * H[1]) * P_priori[1,1]);
            return x_priori;
		}
    }
}
