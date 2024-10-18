namespace KCalMeter;

public struct FoodInfo
{
    public FoodInfo(int kcal, int protein, int fat, int carbohydrates)
    {
        KCal = kcal;
        Protein = protein;
        Fat = fat;
        Carbohydrates = carbohydrates;
    }
    
    public int Fat { get; set; }
    public int Protein { get; set; }
    public int Carbohydrates { get; set; }
    public int KCal { get; set; }
}