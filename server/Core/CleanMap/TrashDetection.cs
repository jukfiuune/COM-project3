namespace Core.CleanMap;

public sealed class TrashDetection
{
    public string Label { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public int X1 { get; init; }
    public int Y1 { get; init; }
    public int X2 { get; init; }
    public int Y2 { get; init; }
    public int ClsId { get; init; }
}
