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

namespace GestureTests.Util
{
    /// <summary>
    /// General purpose matrix class. Can hold matrices of arbitrary dimensions. 
    /// </summary>
    public class Matrix
    {
        /// <summary>
        /// The data contained in the matrix.Implemented as a linear vector. Use provided accessor to access values.
        /// </summary>
        private float[] Data;

        /// <summary>
        /// Height of the matrix.
        /// </summary>
        public int Rows { get; protected set; }

        /// <summary>
        /// Width of the matrix.
        /// </summary>
        public int Columns { get; protected set; }

        /// <summary>
        /// Accessor for datapoint at M_{row}{column}
        /// </summary>
        /// <param name="row">row location of data point. Ensure it is between 0 and Rows-1 </param>
        /// <param name="column">column location of data point. Ensure it is between 0 and Columns-1 </param>
        /// <returns>data point at M_{row}{column}</returns>
        public float this[int row, int column]
        {
            get { return Data[row * Columns + column]; }
            set { Data[row * Columns + column] = value; }
        }

        #region Constructors

        public Matrix(int rows, int columns, float[] data)
        {
            Rows = rows;
            Columns = columns;
            Data = data;
        }

        public Matrix(int numRows)
        {
            Rows = numRows;
            Columns = numRows;
            Data = new float[Rows * Columns];
            Set(0);
        }

        public Matrix(int numRows, int numColumns)
        {
            Rows = numRows;
            Columns = numColumns;
            Data = new float[Rows * Columns];
            Set(0);
        }

        #endregion

        /// <summary>
        /// Sets all datapoints within the matrix to a specified value.
        /// </summary>
        /// <param name="value"></param>
        public void Set(float value)
        {
            for (int i = 0; i < Rows; ++i)
                for (int j = 0; j < Columns; ++j)
                    this[i, j] = value;
        }

        /// <summary>
        /// Constructs an identity matrix of size 'rowsxcolumns'
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Matrix GetIdentityOfSize(int rows, int columns)
        {
            Matrix identity = new Matrix(rows, columns);

            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < columns; ++j)
                    identity[i, j] = (i == j) ? 1.0f : 0.0f;

            return identity;
        }

        /// <summary>
        /// Returns a copy of this matrix.
        /// </summary>
        public Matrix Clone
        {
            get
            {
                Matrix clone = new Matrix(Rows, Columns);

                for (int i = 0; i < Rows; ++i)
                    for (int j = 0; j < Columns; ++j)
                        clone[i, j] = this[i, j];

                return clone;
            }
        }

        /// <summary>
        /// Returns a transpose of this matrix.
        /// </summary>
        public Matrix Transpose
        {
            get
            {
                Matrix m = new Matrix(this.Columns, this.Rows);

                for (int i = 0; i < m.Rows; ++i)
                    for (int j = i; j < m.Columns; ++j)
                        m[i, j] = this[j, i];

                return m;
            }
        }

        /// <summary>
        /// Returns an inverse of this matrix, computed using the Gauss-Jordan Elimination method.
        /// </summary>
        public Matrix Inverse
        {
            get
            {
                //initially identity
                Matrix result = GetIdentityOfSize(Rows, Columns);

                //temporary copy of this matrix where elementary row operations will be performed.
                Matrix m = this.Clone;

                int row, col, index;

                // Try to make m into the identity matrix.  Perform corresponding
                // operations on result;
                for (row = 0; row < Rows; ++row)
                {
                    int pivot_row = row;
                    // Find the best pivot (i.e. largest magnitude)
                    for (index = row + 1; index < Rows; ++index)
                        if (Math.Abs(m[index, row]) > Math.Abs(m[pivot_row, row]))
                            pivot_row = index;

                    // Swap rows to put the best pivot on the diagonal.
                    for (index = 0; index < Columns; ++index)
                    {
                        float temp;

                        temp = m[row, index];
                        m[row, index] = m[pivot_row, index];
                        m[pivot_row, index] = temp;

                        temp = result[row, index];
                        result[row, index] = result[pivot_row, index];
                        result[pivot_row, index] = temp;
                    }

                    // Normalize the pivot row (i.e. put a one on the diagonal)
                    float pivot = m[row, row];
                    for (col = 0; col < Columns; ++col)
                    {
                        m[row, col] = m[row, col] / pivot;
                        result[row, col] = result[row, col] / pivot;
                    }

                    // Introduce zeros above and below the pivot.
                    for (index = 0; index < Rows; ++index)
                        if (index != row)
                        {
                            float scale = m[index, row];
                            for (col = 0; col < Columns; ++col)
                            {
                                m[index, col] = m[index, col] - scale * m[row, col];
                                result[index, col] = result[index, col] - scale * result[row, col];
                            }
                        }
                }

                return result;
            }
        }

        #region matrix operations

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            if (m1.Rows != m2.Rows || m1.Columns != m2.Columns) return null;

            Matrix result = new Matrix(m1.Rows, m2.Columns);

            for (int i = 0; i < result.Rows; ++i)
                for (int j = 0; j < result.Columns; ++j)
                    result[i, j] = m1[i, j] + m2[i, j];

            return result;
        }

        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            if (m1.Rows != m2.Rows || m1.Columns != m2.Columns) return null;

            Matrix result = new Matrix(m1.Rows, m2.Columns);

            for (int i = 0; i < result.Rows; ++i)
                for (int j = 0; j < result.Columns; ++j)
                    result[i, j] = m1[i, j] - m2[i, j];

            return result;
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            //if the two matrices are not fit for multiplication, return null.
            if (m1.Columns != m2.Rows) return null;

            Matrix m = new Matrix(m1.Rows, m2.Columns);

            for (int i = 0; i < m1.Rows; ++i)
                for (int j = 0; j < m2.Columns; ++j)
                {
                    float val = 0;
                    for (int index = 0; index < m1.Columns; ++index)
                        val += m1[i, index] * m2[index, j];
                    m[i, j] = val;
                }

            return m;
        }


        public static Matrix operator -(Matrix m)
        {
            Matrix negative = new Matrix(m.Rows, m.Columns);

            for (int i = 0; i < negative.Rows; ++i)
                for (int j = 0; j < negative.Columns; ++j)
                    negative[i, j] = -m[i, j];

            return negative;
        }

        public static Matrix operator *(Matrix m, float value)
        {
            Matrix result = new Matrix(m.Rows, m.Columns);

            for (int i = 0; i < result.Rows; ++i)
                for (int j = 0; j < result.Columns; ++j)
                    result[i, j] = m[i, j] * value;

            return result;
        }

        public static Matrix operator /(Matrix m, float value)
        {
            Matrix result = new Matrix(m.Rows, m.Columns);

            for (int i = 0; i < result.Rows; ++i)
                for (int j = 0; j < result.Columns; ++j)
                    result[i, j] = m[i, j] / value;

            return result;
        }

        #endregion

        public override string ToString()
        {
            string str = "";

            for (int i = 0; i < Rows; ++i)
            {
                str += "\nrow(" + i + "): ";
                for (int j = 0; j < Columns; ++j)
                    str += this[i, j] + ",";
            }

            return str;
        }
    }
}
