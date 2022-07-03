using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialMove
{
    None = 0,
    EnPassant = 1,
    Castle = 2,
    Promotion = 3
}

public class Chessboard : MonoBehaviour
{
    // Constants
    private const int SQUARE_SIZE = 1;
    private const int SQUARE_COUNT = 8;

    [Header("Graphics")]
    [SerializeField] private Material darkSquareColor;
    [SerializeField] private Material lightSquareColor;
    [SerializeField] private Material hightlightSquareColor;
    [SerializeField] private Material legalMoveSquareColor;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Transform rematch;
    [SerializeField] private Button rematchButton;

    [SerializeField] private float zOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float capturedSize = 0.5f;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] pieces;

    // Game vars
    private GameObject[,] board;
    private GameObject[,] bg;
    private Camera currentCam;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool whiteTurn;

    private Piece[,] chessPieces;
    private Piece currentPiece;
    private List<Vector2Int> legalMoves = new List<Vector2Int>();

    private List<Piece> capturedWhite = new List<Piece>();
    private List<Piece> capturedBlack = new List<Piece>();

    private SpecialMove specialMove;
    private List<Vector2Int[]> history = new List<Vector2Int[]>();

    // Multiplayer vars
    private int playerCount = -1;
    private int currentColor = -1;
    private bool offline = true;
    private bool[] rematchCondition = new bool[2];

    private void Start()
    {
        whiteTurn = true;

        GenerateBoard(SQUARE_SIZE, SQUARE_COUNT);
        SpawnPieces();
        PositionPieces();

        RegisterEvents();
    }
    private void Update()
    {
        if (!currentCam) 
        {
            currentCam = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCam.ScreenPointToRay(Input.mousePosition);

        Material squareColor = getSquareColor(currentHover.x, currentHover.y);

        if (Physics.Raycast(ray, out info, 100))
        {
            Vector2Int hitPosition = getSquareIndex(info.transform.gameObject);

            // If no square if being hovered before
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                board[hitPosition.x, hitPosition.y].GetComponent<Renderer>().material = hightlightSquareColor;
            }

            // If a square was previously being hovered, change it back
            if (currentHover != hitPosition)
            {
                Material targetColor = isLegalSquare(ref legalMoves, currentHover) ? legalMoveSquareColor : squareColor;
                board[currentHover.x, currentHover.y].GetComponent<Renderer>().material = targetColor;

                currentHover = hitPosition;
                board[hitPosition.x, hitPosition.y].GetComponent<Renderer>().material = hightlightSquareColor;       
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    bool turn = chessPieces[hitPosition.x, hitPosition.y].color == 0 && whiteTurn && currentColor == 0 || 
                        chessPieces[hitPosition.x, hitPosition.y].color == 1 && !whiteTurn && currentColor == 1;

                    if (turn)
                    {
                        currentPiece = chessPieces[hitPosition.x, hitPosition.y];

                        // Legal and special moves generation for the currently selected piece
                        legalMoves = currentPiece.getLegalMoves(ref chessPieces, SQUARE_COUNT);
                        specialMove = currentPiece.getSpecialMoves(ref chessPieces, ref history, ref legalMoves);

                        PreventChecks();
                        HighlightLegalSquares();
                    }
                }
            }

            if (currentPiece != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previous = new Vector2Int(currentPiece.file, currentPiece.rank);

                // Checks for legality of the move
                if (isLegalSquare(ref legalMoves, new Vector2Int(hitPosition.x, hitPosition.y)))
                {
                    Move(previous.x, previous.y, hitPosition.x, hitPosition.y);

                    NetMakeMove msg = new NetMakeMove();
                    
                    msg.initialFile = previous.x;
                    msg.initialRank = previous.y;
                    msg.targetFile = hitPosition.x;
                    msg.targetRank = hitPosition.y;
                    msg.color = currentColor;

                    Client.Instance.SendToServer(msg);
                }
                else
                    currentPiece.position(getSquareCenter(previous.x, previous.y));

                currentPiece = null;
                RemoveLegalSquares();
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                Material targetColor = isLegalSquare(ref legalMoves, currentHover) ? legalMoveSquareColor : squareColor;
                board[currentHover.x, currentHover.y].GetComponent<Renderer>().material = targetColor;

                currentHover = -Vector2Int.one; 
            }

            if (currentPiece && Input.GetMouseButtonUp(0))
            {
                RemoveLegalSquares();
                currentPiece.position(getSquareCenter(currentPiece.file, currentPiece.rank));
                currentPiece = null;

            }
        }

        if (currentPiece)
        {
            Plane horizontalPlane = new Plane(Vector3.forward, transform.position);
            float distance = 10.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentPiece.position((Vector2)ray.GetPoint(distance));

        }
    }

    // Board Generation
    private void GenerateBoard(int squareSize, int squareCount)
    {
        zOffset += transform.position.z;
        bounds = new Vector3(squareCount/2 * squareSize, squareCount/2 * squareSize, 0) + boardCenter;

        bg = new GameObject[squareCount, squareCount];
        board = new GameObject[squareCount, squareCount];
        for(int file=0; file<squareCount; file++)
        {
            for(int rank=0; rank<squareCount; rank++)
            {
                Material squareColor = getSquareColor(file, rank);
                bg[file, rank] = GenerateSquare(squareColor, squareSize, file, rank);
                board[file, rank] = GenerateSquare(squareColor, squareSize, file, rank);
            }
        }
    }
    private GameObject GenerateSquare(Material squareColor, int squareSize, int file, int rank)
    {
        GameObject square = new GameObject();
        square.transform.parent = transform;

        Mesh mesh = new Mesh();
        square.AddComponent<MeshFilter>().mesh = mesh;
        square.AddComponent<MeshRenderer>().material = squareColor;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(file * squareSize, rank * squareSize, zOffset) - bounds;
        vertices[1] = new Vector3(file * squareSize, (rank+1) * squareSize, zOffset) - bounds;
        vertices[2] = new Vector3((file+1) * squareSize, rank * squareSize, zOffset) - bounds;
        vertices[3] = new Vector3((file+1) * squareSize, (rank+1) * squareSize, zOffset) - bounds;

        mesh.vertices = vertices;

        int[] tris = new int[] {0, 1, 2, 1, 3, 2};
        mesh.triangles = tris;      

        square.AddComponent<BoxCollider>();

        return square;
    }

    // Piece Generation
    private void SpawnPieces()
    {
        chessPieces = new Piece[SQUARE_COUNT, SQUARE_COUNT];
        int white = 0, black = 1;

        //white pieces
        chessPieces[0, 0] = SpawnPiece(PieceType.Rook, white);
        chessPieces[1, 0] = SpawnPiece(PieceType.Knight, white);
        chessPieces[2, 0] = SpawnPiece(PieceType.Bishop, white);
        chessPieces[3, 0] = SpawnPiece(PieceType.Queen, white);
        chessPieces[4, 0] = SpawnPiece(PieceType.King, white);
        chessPieces[5, 0] = SpawnPiece(PieceType.Bishop, white);
        chessPieces[6, 0] = SpawnPiece(PieceType.Knight, white);
        chessPieces[7, 0] = SpawnPiece(PieceType.Rook, white);

        for(int i=0; i<SQUARE_COUNT; i++)
            chessPieces[i, 1] = SpawnPiece(PieceType.Pawn, white);

         //black pieces
        chessPieces[0, 7] = SpawnPiece(PieceType.Rook, black);
        chessPieces[1, 7] = SpawnPiece(PieceType.Knight, black);
        chessPieces[2, 7] = SpawnPiece(PieceType.Bishop, black);
        chessPieces[3, 7] = SpawnPiece(PieceType.Queen, black);
        chessPieces[4, 7] = SpawnPiece(PieceType.King, black);
        chessPieces[5, 7] = SpawnPiece(PieceType.Bishop, black);
        chessPieces[6, 7] = SpawnPiece(PieceType.Knight, black);
        chessPieces[7, 7] = SpawnPiece(PieceType.Rook, black);

        for(int i=0; i<SQUARE_COUNT; i++)
            chessPieces[i, 6] = SpawnPiece(PieceType.Pawn, black);
    }
    private Piece SpawnPiece(PieceType type, int color)
    {
        int pieceIndex = (int)(type - 1) + (color == 0 ? 0 : 6);
        Piece piece = Instantiate(pieces[pieceIndex], transform).GetComponent<Piece>();
        piece.type = type;
        piece.color = color;

        return piece; 
    }

    // Positioning
    private void PositionPieces()
    {
        for(int file=0; file<SQUARE_COUNT; file++)
            for(int rank=0; rank<SQUARE_COUNT; rank++)
                if (chessPieces[file, rank] != null)
                    PositionPiece(file, rank, false);
    }
    private void PositionPiece(int file, int rank, bool tween = true)
    {
        chessPieces[file, rank].file = file;
        chessPieces[file, rank].rank = rank;
        chessPieces[file, rank].position(getSquareCenter(file, rank), tween);
    }

    // Rotation
    private void RotatePieces()
    {
        for (int file=0; file<SQUARE_COUNT; file++)
            for (int rank=0; rank<SQUARE_COUNT; rank++)
                if (chessPieces[file, rank] != null)
                    RotatePiece(file, rank);
    }
    private void RotatePiece(int file, int rank)
    {
        chessPieces[file, rank].transform.Rotate(0, 0, 180);
    }

    // Move Generation
    private void Move(int initialFile, int initialRank, int file, int rank)
    {
        Piece currentPiece = chessPieces[initialFile, initialRank];
        Vector2Int previous = new Vector2Int(initialFile, initialRank);

        // Checks for existence of another piece
        if (chessPieces[file, rank] != null)
        {
            Piece targetPiece = chessPieces[file, rank];

            if (targetPiece.color == currentPiece.color) return;

            // Handles captures
            Capture(targetPiece);
        }

        // Updates board
        chessPieces[file, rank] = currentPiece;
        chessPieces[previous.x, previous.y] = null;

        PositionPiece(file, rank);

        history.Add(new Vector2Int[]{previous, new Vector2Int(file, rank)});

        ProcessSpecialMove();
        RemoveLegalSquares();

        if (CheckMateCondition()) CheckMate(currentPiece.color);

        whiteTurn = !whiteTurn;

        if (offline) currentColor = (currentColor == 0) ? 1 : 0;

        return;
    }

    private void HighlightLegalSquares()
    {
        for (int i=0; i<legalMoves.Count; i++)
            board[legalMoves[i].x, legalMoves[i].y].GetComponent<Renderer>().material = legalMoveSquareColor;
    }
    private void RemoveLegalSquares()
    {
        for (int i=0; i<legalMoves.Count; i++)
        {
            int file = legalMoves[i].x, rank = legalMoves[i].y;
            Material squareColor = getSquareColor(file, rank);

            board[legalMoves[i].x, legalMoves[i].y].GetComponent<Renderer>().material = squareColor;
        }

        legalMoves.Clear();
    }

    private void Capture(Piece targetPiece)
    {
        targetPiece.transform.localScale = new Vector2(capturedSize, capturedSize);

        if (targetPiece.color == 0)
        {
            if (targetPiece.type == PieceType.King) CheckMate(1);

            capturedWhite.Add(targetPiece);
            targetPiece.position(new Vector2(0 + capturedWhite.Count * 0.45f, -0.25f * SQUARE_SIZE) - (Vector2)bounds);
        }
        else
        {
            if (targetPiece.type == PieceType.King) CheckMate(0);

            capturedBlack.Add(targetPiece);
            targetPiece.position(new Vector2(0 + capturedBlack.Count * 0.45f, 8.25f * SQUARE_SIZE) - (Vector2)bounds);
        }
    }

    // Check special conditions
    private void CheckMate(int color)
    {
        DisplayVictory(color);
    }

    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            Vector2Int enPassantPosition = history[history.Count - 1][1];
            Vector2Int enemyPawnPosition = history[history.Count - 2][1];

            Piece pawn = chessPieces[enPassantPosition.x, enPassantPosition.y];
            Piece enemyPawn = chessPieces[enemyPawnPosition.x, enemyPawnPosition.y];

            if (pawn.file == enemyPawn.file)
            {
                Capture(enemyPawn);
                chessPieces[enemyPawn.file, enemyPawn.rank] = null;
            }
        }

        if (specialMove == SpecialMove.Promotion)
        {
            Vector2Int lastMove = history[history.Count - 1][1];
            Piece pawn = chessPieces[lastMove.x, lastMove.y];
            int colorIndex = (pawn.color == 0) ? 0 : 1;

            Piece newQueen = SpawnPiece(PieceType.Queen, pawn.color);
            Destroy(chessPieces[lastMove.x, lastMove.y].gameObject);

            chessPieces[lastMove.x, lastMove.y] = newQueen;
            PositionPiece(lastMove.x, lastMove.y, false);
        }

        if (specialMove == SpecialMove.Castle)
        {
            Vector2Int lastMove = history[history.Count - 1][1];
            int colorIndex = (chessPieces[lastMove.x, lastMove.y].color == 0) ? 0 : 7;
            Vector2Int rookIndex = new Vector2Int(-1, -1); //(lastMove.x == 2) ? new Vector2Int(0, 3) : new Vector2Int(7, 5);

            if (lastMove.x == 2) rookIndex = new Vector2Int(0, 3);
            else if (lastMove.x == 6) rookIndex = new Vector2Int(7, 5);

            if (rookIndex.x != -1)
            {
                Piece rook = chessPieces[rookIndex.x, colorIndex];

                chessPieces[rookIndex.y, colorIndex] = rook;
                PositionPiece(rookIndex.y, colorIndex);
                chessPieces[rookIndex.x, colorIndex] = null;
            }
        }
    }

    private void PreventChecks()
    {
        // Gets reference to king
        Piece king = null;
        for(int file=0; file<SQUARE_COUNT; file++)
            for (int rank=0; rank<SQUARE_COUNT; rank++)
            {
                Piece piece = chessPieces[file, rank]; 
                if (piece != null && piece.type == PieceType.King && piece.color == currentPiece.color)
                    king = chessPieces[file, rank];
            }

        SimulateMoves(currentPiece, ref legalMoves, king);
    }

    private void SimulateMoves(Piece simPiece, ref List<Vector2Int> legalMoves, Piece king)
    {
        // Stores the default to be reset at the end of function call
        int file = simPiece.file, rank = simPiece.rank;
        List<Vector2Int> pseudoLegalMoves = new List<Vector2Int>();

        // Iterates through all the pseudolegal moves and simulates them to prevent checks
        for (int move=0; move<legalMoves.Count; move++)
        {
            // Gets the position of the move to be simulated
            int simFile = legalMoves[move].x, simRank = legalMoves[move].y;

            // Gets the king position and updates it if a king move was simulated
            Vector2Int kingPosition = new Vector2Int(king.file, king.rank);
            if (simPiece.type == PieceType.King) kingPosition = new Vector2Int(simFile, simRank);

            // Copies the Pieces array so the original doesn't get updated
            Piece[,] simulation = new Piece[SQUARE_COUNT, SQUARE_COUNT];
            List<Piece> simEnemyPieces = new List<Piece>();

            for (int x=0; x<SQUARE_COUNT; x++)
                for (int y=0; y<SQUARE_COUNT; y++)
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if (simulation[x, y].color != simPiece.color) simEnemyPieces.Add(simulation[x, y]);
                    }

            // Simulates move
            simulation[file, rank] = null;
            simPiece.file = simFile;
            simPiece.rank = simRank;
            simulation[simFile, simRank] = simPiece;

            // Checks if any of the pieces got captured during the last simulation
            Piece capturedPiece = null;
            for (int piece=0; piece<simEnemyPieces.Count; piece++)
            {
                capturedPiece = simEnemyPieces[piece];
                if (capturedPiece.file == simFile && capturedPiece.rank == simRank) simEnemyPieces.Remove(capturedPiece);

                // Checks for En Passant case
                else if (simPiece.type == PieceType.Pawn && capturedPiece.type == PieceType.Pawn && history.Count > 3)
                {
                    Vector2Int[] lastMove = history[history.Count - 1];
                    if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2 && lastMove[1].y == rank && capturedPiece.file == simFile)
                    {
                        simEnemyPieces.Remove(capturedPiece);
                        simulation[capturedPiece.file, capturedPiece.rank] = null;
                    }
                }
            }

            // Gets all legal moves of the enemy pieces
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int piece=0; piece<simEnemyPieces.Count; piece++)
            {
                List<Vector2Int> moves = simEnemyPieces[piece].getLegalMoves(ref simulation, SQUARE_COUNT);
                for (int simMove=0; simMove<moves.Count; simMove++) simMoves.Add(moves[simMove]);
            }

            // Removes the move if the king is under attack
            if (isLegalSquare(ref simMoves, kingPosition)) pseudoLegalMoves.Add(legalMoves[move]);

            simPiece.file = file;
            simPiece.rank = rank;
        }

        // Removes all the pseudolegal moves
        for (int move=0; move<pseudoLegalMoves.Count; move++) legalMoves.Remove(pseudoLegalMoves[move]);
    }

    private bool CheckMateCondition()
    {
        Vector2Int lastMove = history[history.Count - 1][1];
        int targetColor = (chessPieces[lastMove.x, lastMove.y].color == 0) ? 1 : 0;

        List<Piece> enemyPieces = new List<Piece>();
        List<Piece> targetPieces = new List<Piece>(); 
        Piece targetKing = null;

        for (int file=0; file<SQUARE_COUNT; file++)
            for (int rank=0; rank<SQUARE_COUNT; rank++)
                if (chessPieces[file, rank] != null)
                {
                    if (chessPieces[file, rank].color == targetColor)
                    {
                        targetPieces.Add(chessPieces[file, rank]);
                        if (chessPieces[file, rank].type == PieceType.King) targetKing = chessPieces[file, rank];
                    }
                    else enemyPieces.Add(chessPieces[file, rank]);
                }

        // Check if the king is under attack
        List<Vector2Int> currentLegalMoves = new List<Vector2Int>();
        for (int piece=0; piece<enemyPieces.Count; piece++)
        {
            List<Vector2Int> moves = enemyPieces[piece].getLegalMoves(ref chessPieces, SQUARE_COUNT);
            for (int move=0; move<moves.Count; move++) currentLegalMoves.Add(moves[move]);
        }

        if (isLegalSquare(ref currentLegalMoves, new Vector2Int(targetKing.file, targetKing.rank)))
        {
            // Checks if a piece can block the check
            for (int piece=0; piece<targetPieces.Count; piece++)
            {
                List<Vector2Int> blockingMoves = targetPieces[piece].getLegalMoves(ref chessPieces, SQUARE_COUNT);
                SimulateMoves(targetPieces[piece], ref blockingMoves, targetKing);

                if (blockingMoves.Count != 0) return false;
            }

            return true;
        }

        return false;
    }

    // UI
    private void DisplayVictory(int winColor)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winColor).gameObject.SetActive(true);
    }

    public void Reset()
    {
        // Handles UI
        rematchButton.interactable = true;
        
        rematch.transform.GetChild(0).gameObject.SetActive(false);
        rematch.transform.GetChild(1).gameObject.SetActive(false);

        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        // Handles pieces
        currentPiece = null;

        for (int file=0; file<SQUARE_COUNT; file++)
        {
            for(int rank=0; rank<SQUARE_COUNT; rank++)
            {
                if (chessPieces[file, rank] != null)
                    Destroy(chessPieces[file, rank].gameObject);

                chessPieces[file, rank] = null;
            }
        }

        for (int i=0; i<capturedWhite.Count; i++)
            Destroy(capturedWhite[i].gameObject);

        for (int i=0; i<capturedBlack.Count; i++)
            Destroy(capturedBlack[i].gameObject);

        // Handles data
        capturedWhite.Clear();
        capturedBlack.Clear();
        legalMoves.Clear();
        history.Clear();
        rematchCondition[0] = rematchCondition[1] = false;

        // Sets up new board
        SpawnPieces();
        PositionPieces();
        whiteTurn = true;
    }
    public void RematchButton()
    {
        if (offline)
        {
            NetRematch brm = new NetRematch();

            brm.color = 1;
            brm.acceptRematch = 1;

            Client.Instance.SendToServer(brm);

            NetRematch wrm = new NetRematch();

            wrm.color = 0;
            wrm.acceptRematch = 1;

            Client.Instance.SendToServer(wrm);
        }
        else
        {
            NetRematch rm = new NetRematch();

            rm.color = currentColor;
            rm.acceptRematch = 1;

            Client.Instance.SendToServer(rm);
        }
    }
    public void ExitButton()
    {
        NetRematch rm = new NetRematch();

        rm.color = currentColor;
        rm.acceptRematch = 0;

        Client.Instance.SendToServer(rm);

        Reset();

        UI.Instance.ExitMenu();

        Client.Instance.ShutDown();
        Server.Instance.ShutDown();

        playerCount -= 1;
        currentColor -= 1;
    }

    // Utility
    private Vector2Int getSquareIndex(GameObject hitInfo)
    {
        for (int file=0; file<SQUARE_COUNT; file++)
            for (int rank=0; rank<SQUARE_COUNT; rank++)
                if (board[file, rank] == hitInfo)
                    return new Vector2Int(file, rank);

        return -Vector2Int.one;
    }
    private Material getSquareColor(int file, int rank)
    {
        bool isLightSquare = (file + rank) % 2 != 0;
        Material squareColor = isLightSquare ? lightSquareColor : darkSquareColor;

        return squareColor;
    }
    private Vector2 getSquareCenter(int file, int rank)
    {
        return new Vector2(file * SQUARE_SIZE, rank * SQUARE_SIZE) - (Vector2)bounds + new Vector2(SQUARE_SIZE, SQUARE_SIZE) / 2;
    }
    private bool isLegalSquare(ref List<Vector2Int> moves, Vector2Int position)
    {
        for (int i=0; i<moves.Count; i++)
            if (moves[i] == position) return true;

        return false;
    }

    // Multiplayer
    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += WelcomeServer;
        NetUtility.S_MAKE_MOVE += MakeMoveServer;
        NetUtility.S_REMATCH += RematchServer;
        
        NetUtility.C_WELCOME += WelcomeClient;
        NetUtility.C_START_GAME += StartGameClient;
        NetUtility.C_MAKE_MOVE += MakeMoveClient;
        NetUtility.C_REMATCH += RematchClient;

        UI.Instance.setOffline += SetOfflineGame;
    }
    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= WelcomeServer;
        NetUtility.S_MAKE_MOVE -= MakeMoveServer;
        NetUtility.S_REMATCH -= RematchServer;
        
        NetUtility.C_WELCOME -= WelcomeClient;
        NetUtility.C_START_GAME -= StartGameClient;
        NetUtility.C_MAKE_MOVE -= MakeMoveClient;
        NetUtility.C_REMATCH -= RematchClient;

        UI.Instance.setOffline -= SetOfflineGame;
    }

    private void WelcomeServer(NetMessage msg, NetworkConnection connection)
    {
        // Assigns team to connected client and sends it back
        NetWelcome nw = msg as NetWelcome;
        nw.AssignedColor = ++playerCount;
        Server.Instance.SendToClient(connection, nw);

        if (playerCount == 1)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }
    private void MakeMoveServer(NetMessage msg, NetworkConnection connection)
    {
        NetMakeMove nmm = msg as NetMakeMove;

        Server.Instance.Broadcast(nmm);
    }
    private void RematchServer(NetMessage msg, NetworkConnection connection)
    {
        Server.Instance.Broadcast(msg);
    }

    private void WelcomeClient(NetMessage msg)
    {
        NetWelcome nw = msg as NetWelcome;
        currentColor = nw.AssignedColor;
        Debug.Log($"Assigned color is: {nw.AssignedColor}");

        if (offline && currentColor == 0)
            Server.Instance.Broadcast(new NetStartGame());
    }
    private void StartGameClient(NetMessage msg)
    {
        UI.Instance.ChangeCamera((currentColor == 0) ? CameraAngle.white : CameraAngle.black);

        if (currentColor == 1) RotatePieces();
    }
    private void MakeMoveClient(NetMessage msg)
    {
        NetMakeMove nmm = msg as NetMakeMove;
        Debug.Log($"color: {nmm.color} file: {nmm.initialFile}, rank: {nmm.initialRank} -> {nmm.targetFile}, {nmm.targetRank}");

        if (nmm.color != currentColor)
        {
            Piece target = chessPieces[nmm.initialFile, nmm.initialRank];

            legalMoves = target.getLegalMoves(ref chessPieces, SQUARE_COUNT);
            specialMove = target.getSpecialMoves(ref chessPieces, ref history, ref legalMoves);

            Move(nmm.initialFile, nmm.initialRank, nmm.targetFile, nmm.targetRank);
        }
    }
    private void RematchClient(NetMessage msg)
    {
        NetRematch rm = msg as NetRematch;

        // Sets the boolean for a rematch
        rematchCondition[rm.color] = rm.acceptRematch == 1;

        if (rm.color != currentColor)
        {
            rematch.transform.GetChild((rm.acceptRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if (rm.acceptRematch != 1)
                rematchButton.interactable = false;
        }

        if (rematchCondition[0] && rematchCondition[1])
            Reset();
    }

    private void SetOfflineGame(bool value)
    {
        playerCount = -1;
        currentColor = -1;
        offline = value;
    } 
}