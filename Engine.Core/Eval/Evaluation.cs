

public class Evaluation()
{
    private int queenWt = 9;
    private int rookWt = 5;
    private int bishopWt = 3;
    private int knightWt = 3;
    private int pawnWt = 1;
    private double DoubledIsolatedWt = -0.5;
    
    int[] blackPieces = [1, 2, 2, 2, 8]
    int[] whitePieces = [1, 2, 2, 2, 8]
    
    public double materialScore(){
        double score = queenWt * (whitePieces[0] - blackPieces[0]);
        double score += rookWt * (whitePieces[1] - blackPieces[1]);
        double score += bishopWt * (whitePieces[2] - blackPieces[2]);
        double score += knightWt * (whitePieces[3] - blackPieces[3]);
        double score += pawnWt * (whitePieces[4] - blackPieces[4]);
        
        return score
    }
    
    
    
    
}