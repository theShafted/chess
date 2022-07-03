using System.Collections.Generic;
using UnityEngine;

public enum PieceType
{
    None = 0,
    Pawn = 1,
    Knight = 2,
    Bishop = 3,
    Rook = 4,
    Queen = 5,
    King = 6
}

public class Piece : MonoBehaviour
{
    public int color;
    public int rank;
    public int file;
    public PieceType type;

    private Vector2 targetPosition;

    private void Update()
    {
        transform.position = Vector2.Lerp(transform.position, targetPosition, Time.deltaTime * 15);
    }

    public virtual void position(Vector2 position, bool tween = true)
    {
        targetPosition = position;
        if (!tween) transform.position = position;
    }

    public virtual List<Vector2Int> getLegalMoves(ref Piece[,] board, int squareCount)
    {
        List<Vector2Int> legalMoves = new List<Vector2Int>();

        return legalMoves;
    }

    public virtual SpecialMove getSpecialMoves(ref Piece[,] chessPieces, ref List<Vector2Int[]> history, ref List<Vector2Int> legalMoves)
    {
        return SpecialMove.None;
    }
}
