using Microsoft.ML;
using Microsoft.ML.Data;
using ProgrammingProjectApplication.Database;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProgrammingProjectApplication.MachineLearning
{
    public class ClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint Cluster { get; set; }
    }


    public static class Recommendation
    {
        static MLContext context = new MLContext();
        static ITransformer model;
        static readonly string modelsPath = Path.Combine(Environment.CurrentDirectory, "MachineLearning", "Models");

        static Recommendation()
        {
            string modelFileName = "kmeansClustering.ml";
            LoadModel(modelFileName);
        }

        public static async Task<GameDataSimilarity[]> GetSimilarGamesAsync(GameData game)
        {
            if (game.Cluster < 0)
            {
                game.Cluster = PredictGameCluster(game);
                await DatabaseHandler.UpdateGameDataAsync(game);
            }

            GameData[] otherGames = await DatabaseHandler.GetClusteredGamesDataAsync(game.Cluster);
            GameDataSimilarity[] similarities = new GameDataSimilarity[otherGames.Length - 1]; // exclude main game

            int similarityCounter = 0;
            foreach (var otherGame in otherGames)
            {
                if (otherGame.Id == game.Id)
                    continue;

                double similarityCoefficient = ComputeJaccardIndex(game, otherGame);
                DebugHelper.WriteMessage($"similarityCounter: {similarityCounter} similarities.len:{similarities.Length}");
                similarities[similarityCounter] = new GameDataSimilarity(otherGame.Id, similarityCoefficient);
                similarityCounter++;
            }

            return similarities;
        }

        private static double ComputeJaccardIndex(GameData mainGameData, GameData otherGameData)
        {
            double coefficientValue = 0;
            string[] mainTags = mainGameData.Tags.Split(';');
            string[] otherTags = otherGameData.Tags.Split(';');
            double intersectionCounter = 0;

            foreach (var mainTag in mainTags)
                intersectionCounter += otherTags.Contains(mainTag) ? 1.0 : 0;

            coefficientValue = intersectionCounter / (mainTags.Length + otherTags.Length - intersectionCounter);

            return coefficientValue;
        }

        public static void LoadModel(string modelFileName = "model.ml")
        {
            string modelPath = Path.Combine(modelsPath, modelFileName);
            if (!File.Exists(modelPath))
                return;

            model = context.Model.Load(modelPath, out DataViewSchema inputSchema);
        }

        public static async Task TrainAndSaveModelToFile(string modelFileName = "model.ml")
        {
            DebugHelper.WriteMessage($"Started training ML Model.");

            GameData[] gameDatas = await DatabaseHandler.GetAllGamesDataAsync();

            IDataView dataView = context.Data.LoadFromEnumerable(gameDatas);

            var textEstimator = context.Transforms.Text.NormalizeText("Tags")
                .Append(context.Transforms.Text.TokenizeIntoWords("TagsTokenized", "Tags", new char[] { ';' }))
                .Append(context.Transforms.Conversion.MapValueToKey("TagsMapped", "TagsTokenized"))
                .Append(context.Transforms.Text.ProduceNgrams("TagsNgrams", "TagsMapped"))
                .Append(context.Transforms.NormalizeLpNorm("TagsNormalized", "TagsNgrams"))
                .Append(context.Clustering.Trainers.KMeans("TagsNormalized", numberOfClusters: 20));

            model = textEstimator.Fit(dataView);

            string modelPath = Path.Combine(modelsPath, modelFileName);
            context.Model.Save(model, dataView.Schema, modelPath);
            DebugHelper.WriteMessage($"ML Model was saved in: {modelPath}");
        }

        public static int PredictGameCluster(GameData gameData)
        {
            var predictionEngine = context.Model.CreatePredictionEngine<GameData, ClusterPrediction>(model);
            return (int)predictionEngine.Predict(gameData).Cluster;
        }

        public static async Task AssignClusterToAllGameData()
        {
            DebugHelper.WriteMessage($"Started assigning clusters to games.");

            var predictionEngine = context.Model.CreatePredictionEngine<GameData, ClusterPrediction>(model);

            GameData[] gameDatas = await DatabaseHandler.GetAllGamesDataAsync();
            for (int i = 0; i < gameDatas.Length; i++)
            {
                var prediction = predictionEngine.Predict(gameDatas[i]);
                gameDatas[i].Cluster = (int)prediction.Cluster;
            }
            var response = await DatabaseHandler.UpdateManyGameDataAsync(gameDatas);

            DebugHelper.WriteMessage($"Assigning result: {response.Result} Msg:{response.Message}");
        }
    }
}