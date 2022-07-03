using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    public override List<Vector2Int> getLegalMoves(ref Piece[,] board, int squareCount)
    {
        List<Vector2Int> legalMoves = new List<Vector2Int>();

        // Rightwards King moves
        if (file + 1 < squareCount)
        {
            if (board[file+1, rank] == null || board[file+1, rank].color != color)
                legalMoves.Add(new Vector2Int(file+1, rank));

            // Top right King moves
            if (rank + 1 < squareCount)
                if (board[file+1, rank+1] == null || board[file+1, rank+1].color != color)
                    legalMoves.Add(new Vector2Int(file+1, rank+1));

            // Bottom right King moves
            if (rank - 1 >= 0)
                if (board[file+1, rank-1] == null || board[file+1, rank-1].color != color)
                    legalMoves.Add(new Vector2Int(file+1, rank-1));
        }

        // Leftwards King moves
        if (file - 1 >= 0)
        {
            if (board[file-1, rank] == null || board[file-1, rank].color != color)
                legalMoves.Add(new Vector2Int(file-1, rank));

            // Top left King moves
            if (rank + 1 < squareCount)
                if (board[file-1, rank+1] == null || board[file-1, rank+1].color != color)
                    legalMoves.Add(new Vector2Int(file-1, rank+1));

            // Bottom left King moves
            if (rank - 1 >= 0)
                if (board[file-1, rank-1] == null || board[file-1, rank-1].color != color)
                legalMoves.Add(new Vector2Int(file-1, rank-1));
        }

        // Upwards King move
        if (rank + 1 < squareCount)
            if (board[file, rank+1] == null || board[file, rank+1].color != color)
                legalMoves.Add(new Vector2Int(file, rank+1)); 

        // Downwards King move
        if (rank - 1 >= 0)
            if (board[file, rank-1] == null || board[file, rank-1].color != color)
                legalMoves.Add(new Vector2Int(file, rank-1)); 

        return legalMoves;
    }

    public override SpecialMove getSpecialMoves(ref Piece[,] chessPieces, ref List<Vector2Int[]> history, ref List<Vector2Int> legalMoves)
    {
        SpecialMove specialMove = SpecialMove.Castle;
        int colorIndex = (color == 0) ? 0 : 7;
        bool kingMoved = false, leftRookMoved = false, rightRookMoved = false;

        // Checks if the King or either of the rooks have moved
        for (int i=0; i<history.Count; i++)
        {
            Vector2Int move = history[i][0];
            if (move.x == 4 && move.y == colorIndex) kingMoved = true;
            else if (move.x == 0 && move.y == colorIndex) leftRookMoved = true;
            else if (move.x == 7 && move.y == colorIndex) rightRookMoved = true;
        }

        if (kingMoved == true || leftRookMoved == true || rightRookMoved == true) specialMove = SpecialMove.None;
 
        // Checks for any obstructions on left side of king
        for (int i=1; i<4; i++)
            if (chessPieces[i, colorIndex] != null) specialMove = SpecialMove.None;

        if (specialMove != SpecialMove.None && chessPieces[0, colorIndex].type == PieceType.Rook)
            legalMoves.Add(new Vector2Int(2, colorIndex));    

        specialMove = SpecialMove.Castle;
        // Checks for any obstruction on right side of king
        for (int i=5; i<7; i++)
            if (chessPieces[i, colorIndex] != null) specialMove = SpecialMove.None;

        if (specialMove != SpecialMove.None && chessPieces[7, colorIndex].type == PieceType.Rook)
            legalMoves.Add(new Vector2Int(6, colorIndex));    

        return specialMove;
    }
}
