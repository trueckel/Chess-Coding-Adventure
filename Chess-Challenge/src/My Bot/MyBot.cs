using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MyBot : IChessBot
{
    bool debug;
    private int currentSearchDepth;
    private Move[] bestMove;
    int cEvals;
    int cDictHits;
    private int[] pieceVals = { 100, 290, 310, 500, 900, 0 };
    private Timer gameTimer;
    private Dictionary<ulong, int> knownPositions;
    private double paraboloid_center_bonus(double x, double y) => (-Math.Pow(x - 3.5, 2) / 30 - Math.Pow(y - 3.5, 2) / 30 + 0.3) * 100;

    public Move Think(Board board, Timer timer)
    {
        gameTimer = timer;
        debug = false;

        /*double paraboloid(double x, double y) => Math.Pow(x - 3.5, 2) / 0.1225 + Math.Pow(y - 3.5, 2) / 0.245 - 100;

        double paraboloid2(double x, double y) => (-Math.Pow(x - 3.5, 2) / 30 - Math.Pow(y - 3.5, 2) / 30 + 0.3)*100;

        double late_factor = 1-(0.5*0.5);
        double center = paraboloid(3, 4) * (1.0 - late_factor) + paraboloid2(3, 4) * late_factor;
        double bottom   = paraboloid(0, 4) * (1.0 - late_factor) + paraboloid2(0, 4) * late_factor;
        double left = paraboloid(4, 0) * (1.0 - late_factor) + paraboloid2(4, 0) * late_factor;
        double corner = paraboloid(0, 0) * (1.0 - late_factor) + paraboloid2(0, 0) * late_factor;
        double corner2 = paraboloid(7, 7) * (1.0 - late_factor) + paraboloid2(7, 7) * late_factor;
        Console.WriteLine("center: {0}", center);
        Console.WriteLine("left: {0}", left);
        Console.WriteLine("bottom: {0}", bottom);
        Console.WriteLine("corner: {0}", corner);
        Console.WriteLine("corner2: {0}", corner2);*/

        Move[] moves = board.GetLegalMoves();
        Console.WriteLine("####################### Legal moves: {0}", moves.Length);

        int maxSearchDepth = 4;
        bestMove = new Move[maxSearchDepth+1];
        bestMove[0] = moves[0];

        knownPositions = new Dictionary<ulong, int>();
        cDictHits = 0;
        for (int curDepth = 1; curDepth <= maxSearchDepth; curDepth++)
        {
            cEvals = 0;
            currentSearchDepth = curDepth;
            int maxEval = NegaMax(board, curDepth, -10000000, 10000000);
            if (maxEval == 10000)
            {
                Console.WriteLine("Found checkmate with searchDepth {0} after {1} ms", curDepth, timer.MillisecondsElapsedThisTurn);
                break;
            }
            if ((double)gameTimer.MillisecondsElapsedThisTurn > (double)gameTimer.MillisecondsRemaining * 0.07)
            {
                Console.WriteLine("DictHits {0}", cDictHits);
                Console.WriteLine("TURN TIME EXPIRED after {0} ms during searchDepth {1}", gameTimer.MillisecondsElapsedThisTurn, currentSearchDepth);
                return bestMove[currentSearchDepth - 1];
            }
            Console.WriteLine("Search with depth {0} took {1} ms for {2} evals", curDepth, timer.MillisecondsElapsedThisTurn, cEvals);
        }
        Console.WriteLine("DictHits {0}", cDictHits);

        return bestMove[currentSearchDepth];
    }

    public void orderMoves(Board board, Move[] moves)
    {
        int[] moveValList = new int[moves.Length];
        int index = 0;
        ulong pieceAttacks;
        foreach (Move move in moves)
        {
            Piece movePiece = board.GetPiece(move.StartSquare);
            Piece capturePiece = board.GetPiece(move.TargetSquare);

            if (capturePiece.PieceType != PieceType.None)
            {
                moveValList[index] += getPieceVal(capturePiece) - getPieceVal(movePiece);
            }

            /*pieceAttacks = BitboardHelper.GetPieceAttacks(movePiece.PieceType, move.TargetSquare, board, board.IsWhiteToMove);
            moveValList[index] += BitboardHelper.GetNumberOfSetBits(pieceAttacks) * 10;*/

            board.MakeMove(move);
            if (board.IsInCheck())
            {
                moveValList[index] += 10;
            }
            board.UndoMove(move);

            index++;
        }
        Array.Sort(moveValList, moves);
        Array.Reverse(moves);
    }

    public int NegaMax(Board board, int depth, int alpha, int beta)
    {
        string indent = "";
        if (debug)
        {
            indent = board.IsWhiteToMove ? "w" : "b";
            indent += new string('-', currentSearchDepth - depth + 1);
            if (currentSearchDepth == depth)
            {
                indent += "-------------------------";
            }
        }

        Move[] legal_moves = board.GetLegalMoves();
        orderMoves(board, legal_moves);

        if (legal_moves.Length == 0)
        {
            if (board.IsInCheck())
            {
                if (debug)
                {
                    Console.WriteLine(indent + " Evaluation: {0} (checkmate)", -10000);
                }
                return -10000;
            }
            if (debug)
            {
                Console.WriteLine(indent + " Evaluation: {0} (stalemate)", 0);
            }
            return 0;
        }

        if (depth == 0)
        {
            cEvals++;
            //int evaluation = Evaluate_NegaMax(board);
            int evaluation = NegaMaxCapturesOnly(board, alpha, beta);
            if (debug)
            {
                //Console.WriteLine(indent + " Evaluation: {0}", evaluation);
            }
            return evaluation;
        }

        int maxEval = alpha;
        foreach (Move move in legal_moves)
        {
            board.MakeMove(move);
            if (debug)
            {
                //Console.WriteLine(indent + "move {0}", getMoveAsString(move));
            }
            //int extension = board.IsInCheck() ? 1 : 0;
            int eval = -NegaMax(board, depth - 1, -beta, -maxEval);

            if ((double)gameTimer.MillisecondsElapsedThisTurn > (double)gameTimer.MillisecondsRemaining * 0.07)
            {
                return beta;
            }

            /*if (depth == searchDepth)
            {
                debug = false;
                Console.WriteLine(indent + "move {0}: {1}", getMoveAsString(move), eval);
            }*/

            if (board.GameRepetitionHistory.Contains(board.ZobristKey))
            {
                board.UndoMove(move);
                continue;
            }

            board.UndoMove(move);
            if (eval > maxEval)
            {
                maxEval = eval;
                if (depth == currentSearchDepth)
                {
                    if (debug)
                    {
                        Console.WriteLine(indent + "new BEST move {0}: {1}", getMoveAsString(move), eval);
                    }

                    bestMove[currentSearchDepth] = move;
                }
                if (maxEval >= beta)
                {
                    //Console.WriteLine(indent + " beta cutoff, maxEval ({0}) >= beta ({1})", maxEval, beta);
                    return beta;
                    //break;

                }
            }
        }
        return maxEval;
    }

    public int NegaMaxCapturesOnly(Board board, int alpha, int beta)
    {
        string indent = "";
        if (debug)
        {
            indent = board.IsWhiteToMove ? "w" : "b";
            indent += " CO ";
        }

        cEvals++;
        int evaluation = Evaluate_NegaMax(board);
        if (evaluation >= beta)
        {
            return beta;
        }
        alpha = Math.Max(alpha, evaluation);

        Move[] legal_moves = board.GetLegalMoves(true);
        orderMoves(board, legal_moves);

        /*if (legal_moves.Length == 0)
        {
            if (board.IsInCheck())
            {
                return -1000000;
            }
            return 0;
        }*/

        //int maxEval = alpha;
        foreach (Move move in legal_moves)
        {
            board.MakeMove(move);
            if (debug)
            {
                //Console.WriteLine(indent + "move {0}", getMoveAsString(move));
            }
            int eval = -NegaMaxCapturesOnly(board, -beta, -alpha);
            board.UndoMove(move);

            if (eval >= beta)
            {
                //Console.WriteLine(indent + " beta cutoff, eval ({0}) >= beta ({1})", eval, beta);
                return beta;
            }
            alpha = Math.Max(alpha, eval);
        }
        return alpha;
    }

    public string getMoveAsString(Move move)
    {
        PieceType movingPiece = move.MovePieceType;
        char movingPieceAsString = movingPiece.ToString() == "Knight" ? 'N' : movingPiece.ToString()[0];
        PieceType capturedPiece = move.CapturePieceType;
        string startSquare = move.StartSquare.Name;
        string targetSquare = move.TargetSquare.Name;

        if (capturedPiece == PieceType.None && movingPiece == PieceType.Pawn)
        {
            return targetSquare;
        }
        else if (capturedPiece == PieceType.None)
        {
            return movingPieceAsString + startSquare + targetSquare;
        }
        return movingPieceAsString + "x" + targetSquare;
    }

    public int Evaluate_NegaMax(Board board)
    {
        if (knownPositions.ContainsKey(board.ZobristKey))
        {
            cDictHits++;
            return knownPositions[board.ZobristKey];
        }

        PieceList[] piecesLists = board.GetAllPieceLists();
        int eval = 0;
        int material_maxplayer = 0, material_minplayer = 0;
        for (int i = 0; i < piecesLists.Length / 2; i++)
        {
            material_maxplayer += piecesLists[i].Count * pieceVals[i];
            material_minplayer += piecesLists[i + 6].Count * pieceVals[i];
            //material += piecesLists[i].Count * pieceVals[i];
        }

        eval += KnightPositionEval(board);
        eval += BishopPositionEval(board);
        //Console.WriteLine("after knightEval: {0}", eval);

        eval += EvaluateKingsPosition(board, material_maxplayer + material_minplayer);

        int king_safety = EvaluateKingsSafety(board, true) - EvaluateKingsSafety(board, false);
        //Console.WriteLine("king_safety: {0}", king_safety);
        eval += king_safety;

        // Black-White differenciation until here

        if (!board.IsWhiteToMove)
        {
            eval = eval * -1;
        }

        // Max-Min Player differenciation beginning here

        if (!board.IsWhiteToMove)
        {
            int temp = material_maxplayer;
            material_maxplayer = material_minplayer;
            material_minplayer = temp;
        }
        eval += material_maxplayer - material_minplayer;

        if (material_maxplayer > material_minplayer && material_minplayer < 900)
        {
            eval += EvaluateKingsDistance(board);
        } else if (material_minplayer > material_maxplayer && material_maxplayer < 900)
        {
            eval -= EvaluateKingsDistance(board);
        }

        knownPositions[board.ZobristKey] = eval;
        return eval;
    }

    public int getPieceVal(Piece piece)
    {
        return pieceVals[((int)piece.PieceType) - 1];
    }

    public void printMoveEvals(Move move, int eval)
    {
        Console.WriteLine("Move: {0}, Eval: {1}", getMoveAsString(move), eval);
    }

    public int EvaluateKingsDistance(Board board)
    {
        int manhattanDist = Math.Abs(board.GetKingSquare(true).Rank - board.GetKingSquare(false).Rank);
        manhattanDist += Math.Abs(board.GetKingSquare(true).File - board.GetKingSquare(false).File);
        int eval = (14 - manhattanDist) * 2;
        return (14 - manhattanDist)*2;
    }

    public int getNumPiecesLeft(Board board)
    {
        int numPieces = 0;
        foreach (PieceList list in board.GetAllPieceLists())
        {
            numPieces += list.Count();
        }
        return numPieces;
    }

    private int EvaluateKingsSafety(Board board, bool color)
    {
        int cntAdjacentPawns = 0;
        ulong kAdjacentSquares = BitboardHelper.GetKingAttacks(board.GetKingSquare(color));

        int idx = 0;
        while (true)
        {
            idx = BitboardHelper.ClearAndGetIndexOfLSB(ref kAdjacentSquares);
            if (idx > 63) 
                break;
            if (board.GetPiece(new Square(idx)).PieceType == PieceType.Pawn)
            {
                cntAdjacentPawns++;
            }
        }
        return cntAdjacentPawns * 5;
    }

    public int EvaluateKingsPosition(Board board, int total_material)
    {
        double eval = 0;
        double lategameFactor = 1.0 - Math.Pow((float)total_material / 7800.0, 2);
        //Console.WriteLine("lategameFactor: {0}", lategameFactor);

        double paraboloid_corner_bonus(double x, double y) => Math.Pow(x - 3.5, 2) / 0.245 + Math.Pow(y - 3.5, 2) / 0.1225 - 100;

        eval += paraboloid_center_bonus(board.GetKingSquare(true).File, board.GetKingSquare(true).Rank) * lategameFactor +
            paraboloid_corner_bonus(board.GetKingSquare(true).File, board.GetKingSquare(true).Rank) * 0.5 * (1.0-lategameFactor);

        eval -= paraboloid_center_bonus(board.GetKingSquare(false).File, board.GetKingSquare(false).Rank) * lategameFactor +
            paraboloid_corner_bonus(board.GetKingSquare(false).File, board.GetKingSquare(false).Rank) * 0.5 * (1.0-lategameFactor);

        //eval += paraboloid(board.GetKingSquare(true).File, board.GetKingSquare(true).Rank);
        //Console.WriteLine("EvaluateKingsPosition W: {0}", eval);
        //eval -= paraboloid(board.GetKingSquare(false).File, board.GetKingSquare(false).Rank);
        //Console.WriteLine("EvaluateKingsPosition: {0}", eval);
        return Convert.ToInt32(eval);
    }

    public int KnightPositionEval(Board board)
    {
        double eval = 0;
        foreach (Piece knight in board.GetPieceList(PieceType.Knight, true))
        {
            eval += paraboloid_center_bonus(knight.Square.File, knight.Square.Rank);
        }
        foreach (Piece knight in board.GetPieceList(PieceType.Knight, false))
        {
            eval -= paraboloid_center_bonus(knight.Square.File, knight.Square.Rank);
        }
        return Convert.ToInt32(eval);
    }

    public int BishopPositionEval(Board board)
    {
        int eval = 0;
        foreach (Piece bishop in board.GetPieceList(PieceType.Bishop, true))
        {
            eval += 2 * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Bishop, bishop.Square, board.WhitePiecesBitboard));
        }
        foreach (Piece bishop in board.GetPieceList(PieceType.Bishop, false))
        {
            eval -= 2 * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Bishop, bishop.Square, board.BlackPiecesBitboard));
        }
        //Console.WriteLine("BishopEval: {0}", eval);
        return eval;
    }
}