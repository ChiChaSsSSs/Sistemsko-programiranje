using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ML.DataOperationsCatalog;

namespace Projekat3.SentimentAnalysis
{
    public class SentimentService
    {
        private readonly MLContext mlContext;
        private readonly PredictionEngine<SentimentData, SentimentPrediction> predEngine;

        public SentimentService()
        {
            mlContext = new MLContext();

            IDataView dataView = mlContext.Data.LoadFromTextFile<SentimentData>("dataset.tsv", hasHeader: true);

            TrainTestData trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            IDataView trainingData = trainTestSplit.TrainSet;
            IDataView testData = trainTestSplit.TestSet;


            var dataProcessPipeline = mlContext.Transforms.Text.FeaturizeText
                (outputColumnName: "Features", inputColumnName: nameof(SentimentData.Text));


            var trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features");
            var trainingPipeline = dataProcessPipeline.Append(trainer);

            ITransformer trainedModel = trainingPipeline.Fit(trainingData);


            predEngine = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(trainedModel);
        }

        public SentimentPrediction Predict(string text)
        {
            return predEngine.Predict(new SentimentData { Text = text });
        }
    }
}
