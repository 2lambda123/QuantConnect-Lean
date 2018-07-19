﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using Accord.Math;

namespace QuantConnect.Algorithm.Framework.Portfolio.Optimization
{    
    /// <summary>
    /// Mean-Variance Portfolio Optimization
    /// </summary>
    public class MeanVariancePortfolio : IPortfolioOptimization
    {
        protected double[,] _cov;
        protected double[] _x0;
        protected double[] _scale;
        protected double _lower;
        protected double _upper;
        protected List<double[]> _constraints;
        protected List<int> _constraintTypes;

        protected double _targetReturn;

        protected int Size => _cov == null ? 0 : _cov.GetLength(0);

        public MeanVariancePortfolio(double lower, double upper, double targetReturn = 0.0)
        {
            _constraints = new List<double[]>();
            _constraintTypes = new List<int>();
            _cov = null;
            _x0 = null;
            _scale = null;
            _lower = lower;
            _upper = upper;
            _targetReturn = targetReturn;
        }

        public void SetCovariance(double[,] cov) => _cov = cov;

        public void SetInitialValue(double[] init = null)
        {
            if (init == null || init.Length != Size)
            {
                if (_x0 == null || _x0.Length != Size)
                {
                    _x0 = Vector.Create(Size, 1.0 / Size);
                }
            }
            else
            {
                _x0 = init;
            }
        }

        public void SetScale(double[] scale = null)
        {
            if (scale == null || scale.Length != Size)
            {
                if (_scale == null || _scale.Length != Size)
                {
                    _scale = Vector.Create(Size, 1.0);
                }
            }
            else
            {
                _scale = scale;
            }
        }

        public void SetConstraints(double[] lc, ConstraintType ct, double rc)
        {
            if (lc.Length != Size)
            {
                throw new ArgumentOutOfRangeException(String.Format("Incorrect number of constraints: {0}", lc));
            }
            var c = Vector.Create(Size + 1, rc);
            lc.CopyTo(c, 0);
            _constraints.Add(c);
            _constraintTypes.Add((int)ct);
        }

        /// <summary>
        /// Solve QP problem
        /// </summary>
        /// <param name="x">Portfolio weights</param>
        /// <returns></returns>
        protected virtual int Optimize(out double[] x)
        {
            alglib.minqpstate state;

            // set quadratic/linear terms
            alglib.minqpcreate(Size, out state);
            alglib.minqpsetquadraticterm(state, _cov.Multiply(2.0));
            //alglib.minqpsetlinearterm(state, b);

            alglib.minqpsetstartingpoint(state, _x0);            
            alglib.minqpsetbc(state, Vector.Create(Size, _lower), Vector.Create(Size, _upper));

            // wire all constraints            
            var C = Matrix.Create(_constraints.ToArray());
            alglib.minqpsetlc(state, C, _constraintTypes.ToArray());
            _constraints.Clear();
            _constraintTypes.Clear();

            int ret = 0;
            x = Vector.Create(Size, 0.0);
            alglib.minqpreport rep;
            bool autoscale = true;
            while (autoscale)
            {
                //if (autoscale) // For version 3.14
                //{
                //    alglib.minqpsetscaleautodiag(_state);
                //}
                //else
                {
                    SetScale();
                    alglib.minqpsetscale(state, _scale);
                }
                autoscale = false;

                // Solve problem                
                //alglib.minqpsetalgodenseaul(_state, 0, 1.0e+4, 0); // For version 3.14
                alglib.minqpsetalgobleic(state, 0.0, 0.0, 0.0, 0);
                alglib.minqpoptimize(state);

                // Get results
                double[] res;
                alglib.minqpresults(state, out res, out rep);
                ret = rep.terminationtype;
                x = res;

                // Restart with different scale
                if (ret == -9)
                    autoscale = true;
            }
            return ret;
        }

        /// <summary>
        /// Perform mean variance optimization given the returns
        /// </summary>   
        /// <param name="expectedReturns">Vector of expected returns</param>
        /// <returns>Portfolio weights</returns>
        public double[] Optimize(double[] expectedReturns)
        {
            SetInitialValue();

            // sum(x) = 1
            SetConstraints(Vector.Create(Size, 1.0), ConstraintType.Equal, 1.0);
            // mu^T x = R  or mu^T x >= 0
            SetConstraints(expectedReturns, _targetReturn == 0.0 ? ConstraintType.More : ConstraintType.Equal, _targetReturn);

            double[] W;
            var ret = Optimize(out W);

            return W;
        }
    }

    /// <summary>
    /// Maximum Sharpe Ratio Portfolio Optimization
    /// </summary>
    public class MaxSharpeRatioPortfolio : MeanVariancePortfolio
    {
        double _riskFreeRate;
        double[] _expectedReturns;

        public MaxSharpeRatioPortfolio(double lower, double upper, double riskFreeRate = 0.0) : base(lower, upper, 0.0)
        {
            _riskFreeRate = riskFreeRate;
        }

        public new double[] Optimize(double[] expectedReturns)
        {
            _expectedReturns = expectedReturns;

            SetInitialValue();

            SetConstraints(Vector.Create(Size, 1.0), ConstraintType.Equal, 1.0);

            double[] W;
            var ret = Optimize(out W); // use NLP solver

            return W;
        }

        public static void SharpeRatio(double[] x, ref double func, object obj)
        {
            var opt = (MaxSharpeRatioPortfolio)obj;
            var annual_return = opt._expectedReturns.Dot(x);
            var annual_volatility = Math.Sqrt(x.Dot(opt._cov).Dot(x));
            func = (annual_return - opt._riskFreeRate) / annual_volatility;
            func = Double.IsInfinity(func) || Double.IsNaN(func)  ? 1.0E+300 : -func;
        }

        protected override int Optimize(out double[] x)
        {
            alglib.minbleicstate state;
            
            double diffstep = 1.0e-6; // This variable contains differentiation step            
            alglib.minbleiccreatef(_x0, diffstep, out state);
            alglib.minbleicsetbc(state, Vector.Create(Size, _lower), Vector.Create(Size, _upper));

            // wire all constraints            
            var C = Matrix.Create(_constraints.ToArray());
            alglib.minbleicsetlc(state, C, _constraintTypes.ToArray());
            _constraints.Clear();
            _constraintTypes.Clear();

            // Stopping conditions for the optimizer. 
            alglib.minbleicsetcond(state, 0, 0, 0, 0);
            alglib.minbleicoptimize(state, SharpeRatio, null, this);

            alglib.minbleicreport rep;
            alglib.minbleicresults(state, out x, out rep);
            return rep.terminationtype;
        }
    }

}
