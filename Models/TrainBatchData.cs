namespace IReadThis.Recommender.Models
{
    internal sealed class TrainBatchData
    {
        public TrainBatchData() { }
        public TrainBatchData(int[] birthYears, int[] sexIds, int[,] paddedCategories, int[,] textTokens, float[] trueRatings) : this()
        {
            BirthYears = birthYears;
            SexIds = sexIds;
            PaddedCategories = paddedCategories;
            TextTokens = textTokens;
            TrueRatings = trueRatings;
        }

        public int[] BirthYears { get; set; }
        public int[] SexIds { get; set; }
        public int[,] PaddedCategories { get; set; }
        public int[,] TextTokens { get; set; }
        public float[] TrueRatings { get; set; }
    }
}
