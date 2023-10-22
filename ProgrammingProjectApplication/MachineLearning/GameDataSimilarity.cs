using System.Collections;

public struct GameDataSimilarity
{
    private int otherGameDataId;
    public int OtherGameDataId
    {
        get { return otherGameDataId; }
        set
        {
            if (value >= 0)
            {
                otherGameDataId = value;
            }
        }
    }

    public double SimilarityCoefficientValue { get; set; }

    public GameDataSimilarity()
    {

    }

    public GameDataSimilarity(int id, double similarity)
    {
        OtherGameDataId = id;
        SimilarityCoefficientValue = similarity;
    }
}