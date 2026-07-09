/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Class implementing the Random Sample Consensus (RANSAC) algorithm for a generic data type.
    /// RANSAC is an iterative non-deterministic algorithm used for fitting estimation in the presence of a large number of outliers in the data.
    /// </summary>
    /// <typeparam name="TModel">The type of the model being fitted to the data.</typeparam>
    public class RandomSampleConsensus<TModel>
    {
        /// <summary>
        /// Delegate for generating a model from a pair of samples,
        /// represented by their indices.
        /// </summary>
        /// <param name="index1">The first sample index.</param>
        /// <param name="index2">The second sample index.</param>
        /// <returns>A model generated from the provided indices.</returns>
        public delegate TModel GenerateModel(int index1, int index2);

        /// <summary>
        /// Delegate for evaluating the agreement score of a model with respect to the entire dataset.
        /// </summary>
        /// <param name="model">The model to be evaluated.</param>
        /// <param name="modelSet">The set of all generated models for comparison.</param>
        /// <returns>The score representing the model's agreement with the data.</returns>
        public delegate float EvaluateModelScore(TModel model, TModel[,] modelSet);

        private readonly TModel[,] _modelSet;
        private readonly int _exclusionZone;
        private readonly int _maxDataPoints;

        /// <summary>
        /// Initializes a new instance of the RandomSampleConsensus class.
        /// </summary>
        /// <param name="maxDataPoints">The total number of sample points.</param>
        /// <param name="exclusionZone">The threshold to avoid selecting samples too close to each other.</param>
        public RandomSampleConsensus(int maxDataPoints = 10, int exclusionZone = 2)
        {
            _maxDataPoints = maxDataPoints;
            _exclusionZone = exclusionZone;
            _modelSet = new TModel[maxDataPoints, maxDataPoints];
        }

        /// <summary>
        /// Calculates the best fitting model based on the RANSAC algorithm.
        /// </summary>
        /// <param name="modelGenerator">The function to generate models from data points.</param>
        /// <param name="modelScorer">The function to evaluate the agreement score of a model.</param>
        /// <returns>The model that best fits the data according to the RANSAC algorithm.</returns>
        public TModel FindOptimalModel(GenerateModel modelGenerator, EvaluateModelScore modelScorer)
        {
            return FindOptimalModel(modelGenerator, modelScorer, _maxDataPoints);
        }

        /// <summary>
        /// Calculates the best fitting model based on the RANSAC algorithm.
        /// </summary>
        /// <param name="modelGenerator">The function to generate models from data points.</param>
        /// <param name="modelScorer">The function to evaluate the agreement score of a model.</param>
        /// <param name="dataPointsCount">The number of data points to evaluate.</param>
        /// <returns>The model that best fits the data according to the RANSAC algorithm.</returns>
        public TModel FindOptimalModel(GenerateModel modelGenerator,
            EvaluateModelScore modelScorer, int dataPointsCount)
        {
            for (int i = 0; i < dataPointsCount; ++i)
            {
                for (int j = i + 1; j < dataPointsCount; ++j)
                {
                    _modelSet[i, j] = modelGenerator(
                        (i + _exclusionZone) % dataPointsCount,
                        (j + _exclusionZone) % dataPointsCount);
                }
            }

            TModel bestModel = default;
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < dataPointsCount; ++i)
            {
                int y = Random.Range(0, dataPointsCount - 1);
                int x = Random.Range(y + 1, dataPointsCount);

                TModel model = _modelSet[y, x];
                float score = modelScorer(model, _modelSet);

                if (score < bestScore)
                {
                    bestModel = model;
                    bestScore = score;
                }
            }

            return bestModel;
        }
    }
}
