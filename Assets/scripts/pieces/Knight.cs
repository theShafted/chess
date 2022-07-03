using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
    public override List<Vector2Int> getLegalMoves(ref Piece[,] board, int squareCount)
    {
        List<Vector2Int> legalMoves = new List<Vector2Int>();

        //Top right Knight moves
        int targetFile = file + 1;
        int targetRank = rank + 2;

        if (targetFile < squareCount && targetRank < squareCount)
            if (board[targetFile, targetRank] == null || board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

        targetFile = file + 2;
        targetRank = rank + 1;

        if (targetFile < squareCount && targetRank < squareCount)
            if (board[targetFile, targetRank] == null || board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

        // Top left Knight moves
        targetFile = file - 1;
        targetRank = rank + 2;

        if (targetFile >= 0 && targetRank < squareCount)
            if (board[targetFile, targetRank] == null || board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

        targetFile = file - 2;
        targetRank = rank + 1;

        if (targetFile >= 0 && targetRank < squareCount)
            if (board[targetFile, targetRank] == null || board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

        // Bottom right Knight moves
        targetFile = file + 1;
        targetRank = rank - 2;

        if (targetFile < squareCount && targetRank >= 0)
            if (board[targetFile, targetRank] == null || board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

        targetFile = file + 2;
        targetRank = rank - 1;

        if (targetFile < squareCount && targetRank >= 0)
            if (board[targetFile, targetRank] == null || board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

        //Bottom left Knight moves
        targetFile = file - 1;
        targetRank = rank - 2;

        if (targetFile >= 0 && targetRank >= 0)
            if (board[targetFile, targetRank] == null || board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

        targetFile = file - 2;
        targetRank = rank - 1;

        if (targetFile >= 0 && targetRank >= 0)
            if (board[targetFile, targetRank] == null || board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

        return legalMoves;
    }
}
