using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece
{
    public override List<Vector2Int> getLegalMoves(ref Piece[,] board, int squareCount)
    {
        List<Vector2Int> legalMoves = new List<Vector2Int>();

        // Top right Bishop moves
        for (int targetRank=rank+1, targetFile=file+1; targetRank<squareCount && targetFile<squareCount; targetRank++, targetFile++)
        {
            if (board[targetFile, targetRank] == null)
            {
                legalMoves.Add(new Vector2Int(targetFile, targetRank));
                continue;
            }

            else if (board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

            break;
        }

        // Bottom right Bishop moves
        for (int targetRank=rank-1, targetFile=file+1; targetRank>=0 && targetFile<squareCount; targetRank--, targetFile++)
        {
            if (board[targetFile, targetRank] == null)
            {
                legalMoves.Add(new Vector2Int(targetFile, targetRank));
                continue;
            }

            else if (board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

            break;
        }

        // Top left Bishop moves
        for (int targetRank=rank+1, targetFile=file-1; targetRank<squareCount && targetFile>=0; targetRank++, targetFile--)
        {
            if (board[targetFile, targetRank] == null)
            {
                legalMoves.Add(new Vector2Int(targetFile, targetRank));
                continue;
            }

            else if (board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

            break;
        }

        // Bottom left Bishop moves
        for (int targetRank=rank-1, targetFile=file-1; targetRank>=0 && targetFile>=0; targetRank--, targetFile--)
        {
            if (board[targetFile, targetRank] == null)
            {
                legalMoves.Add(new Vector2Int(targetFile, targetRank));
                continue;
            }

            else if (board[targetFile, targetRank].color != color)
                legalMoves.Add(new Vector2Int(targetFile, targetRank));

            break;
        }

        return legalMoves;
    }
}
