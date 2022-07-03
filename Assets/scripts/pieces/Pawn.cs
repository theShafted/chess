using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{
    public override List<Vector2Int> getLegalMoves(ref Piece[,] board, int squareCount)
    {
        List<Vector2Int> legalMoves = new List<Vector2Int>();

        int rankDirection = (color == 0) ? 1 : -1;

        // Check for one rank pawn move
        if (board[file, rank + rankDirection] == null)
        {
            legalMoves.Add(new Vector2Int(file, rank + rankDirection));

            // Check for two ranks pawn move from initial position
            if (color == 0 && rank == 1 && board[file, rank + 2*rankDirection] == null)
                legalMoves.Add(new Vector2Int(file, rank + 2*rankDirection));

            if (color == 1 && rank == 6 && board[file, rank + 2*rankDirection] == null)
                legalMoves.Add(new Vector2Int(file, rank + 2*rankDirection));
        }

        // Check for pawn capture move
        if (file != squareCount - 1)
        {
            Vector2Int target = new Vector2Int(file + 1, rank + rankDirection);
            if (board[target.x, target.y] != null && board[target.x, target.y].color != color)
                legalMoves.Add(target);
        }

        if (file != 0)
        {
            Vector2Int target = new Vector2Int(file - 1, rank + rankDirection);
            if (board[target.x, target.y] != null && board[target.x, target.y].color != color)
                legalMoves.Add(target);
        }

        return legalMoves;
    }

    public override SpecialMove getSpecialMoves(ref Piece[,] chessPieces, ref List<Vector2Int[]> history, ref List<Vector2Int> legalMoves)
    {
        int rankDirection = (color == 0) ? 1 : -1;

        // Promotion
        if ((color == 0 && rank == 6) || (color == 1 && rankDirection == 1)) return SpecialMove.Promotion;
        
        // En Passant
        if (history.Count > 3)
        {
            Vector2Int[] lastMove = history[history.Count-1];

            // Checks if last move was of a Pawn
            if (chessPieces[lastMove[1].x, lastMove[1].y].type == PieceType.Pawn)
            {
                // Check if the pawn moved two squares and landed next to the current pawn
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2 && lastMove[1].y == rank)
                {
                    if (lastMove[1].x == file - 1) legalMoves.Add(new Vector2Int(file - 1, rank + rankDirection));

                    if (lastMove[1].x == file + 1) legalMoves.Add(new Vector2Int(file + 1, rank + rankDirection));

                    return SpecialMove.EnPassant;
                }
            } 
        }

        return SpecialMove.None;
    }
}
