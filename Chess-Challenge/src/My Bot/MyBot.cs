using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MyBot : IChessBot
{
    readonly System.Random rng = new();
    private int searchDepth;
    int cEvals;
    private int[] pieceVals = { 100, 290, 310, 500, 900, 0, -100, -290, -310, -500, -900, 0 };
    Dictionary<string, int> openingMovesBoni = new Dictionary<string, int>
    {
        { "e4", 30 },
        { "d4", 30 },
        { "c4", 25 },
        { "b3", 20 },
        { "g3", 20 },
        { "Nb1c3", 30 },
        { "Nb1d2", 25 },
        { "Ng1f3", 30 },
        { "Ng1e2", 25 },
        { "Bc1f4", 30 },
        { "Bc1g5", 25 },
        { "Bf1c4", 30 },
        { "Bf1b5", 25 },
        { "Bf1g2", 35 },
        { "Bc1b2", 35 },
        { "Ke1g1", 50 },
        { "Ke1c1", 50 },
    };

    public Move Think(Board board, Timer timer)
    {
        cEvals = 0;

        Move[] moves = board.GetLegalMoves();
        Console.WriteLine("Legal moves: {0}", moves.Length);

        //int searchDepth;
        switch (moves.Length)
        {
            case > 40:
                searchDepth = 3;
                break;
            case > 30:
                searchDepth = 4;
                break;
            default:
                searchDepth = 5;
                break;
        }
        searchDepth = 3;

        Move bestMove = moves[0];
        /*Console.WriteLine("Start MinMax Search with depth {0}", searchDepth);
        Search(board, searchDepth, int.MinValue, int.MaxValue);
        Console.WriteLine("ms: {0} for {1} evals, best move: {2}", timer.MillisecondsElapsedThisTurn, cEvals, getMoveAsString(bestMove));
        cEvals = 0;*/
        int time_stop = timer.MillisecondsElapsedThisTurn;
        Console.WriteLine("Start NegaMax Search with depth {0}", searchDepth);
        bestMove = getBestMove(board, searchDepth);
        Console.WriteLine("ms: {0} for {1} evals, best move: {2}", timer.MillisecondsElapsedThisTurn - time_stop, cEvals, getMoveAsString(bestMove));
        return bestMove;
    }

    public void orderMoves(Board board, Move[] moves)
    {
        int[] moveValList = new int[moves.Length];
        int index = 0;
        foreach (Move move in moves)
        {
            Piece movePieceType = board.GetPiece(move.StartSquare);
            Piece capturePieceType = board.GetPiece(move.TargetSquare);
            
            if (capturePieceType.PieceType != PieceType.None)
            {
                moveValList[index] += getPieceVal(capturePieceType) - getPieceVal(movePieceType);
            }

            board.MakeMove(move);
            if (board.IsInCheck())
            {
                moveValList[index] += 10;
            }
            board.UndoMove(move);

            index++;
        }
        Array.Sort(moveValList, moves);
        if (board.IsWhiteToMove)
        {
            Array.Reverse(moves);
        }
    }

    public Move getBestMove(Board board, int searchDepth)
    {
        Move[] legal_moves = board.GetLegalMoves();
        orderMoves(board, legal_moves);

        Move bestMove = legal_moves[0];
        int maxScore = -10000;
        int alpha = -10000;
        int beta = 10000;
        foreach (Move move in legal_moves)
        {
            board.MakeMove(move);
            Console.WriteLine("==== Try move {0}", getMoveAsString(move));
            int score = -NegaMax(board, searchDepth-1, alpha, beta);
            Console.WriteLine("=========> Move Score {0}", score);
            if (score > maxScore)
            {
                beta = score;
                bestMove = move;
                maxScore = score;
            }
            board.UndoMove(move);
        }
        return bestMove;
    }

    public int NegaMax(Board board, int depth, int alpha, int beta)
    {
        
        //string indent = board.IsWhiteToMove ? "w" : "b";
        //indent += new string('-', searchDepth-depth+1);
        if (depth == 0)
        {
            cEvals++;
            int evaluation = Evaluate(board);
            //Console.WriteLine(indent + " Evaluation: {0}", evaluation);
            return evaluation;
        }

        Move[] legal_moves = board.GetLegalMoves();
        orderMoves(board, legal_moves);

        if (legal_moves.Length == 0)
        {
            
            if (board.IsInCheck())
            {
                //Console.WriteLine(indent + " Return -inf (checkmate)");
                return int.MinValue;
            }
            //Console.WriteLine("Return 0 (stalemate)");
            return 0;
        }

        foreach (Move move in legal_moves)
        {
            board.MakeMove(move);
            //Console.WriteLine(indent + "move {0}", getMoveAsString(move));

            int eval = -NegaMax(board, depth - 1, -beta, -alpha);

            //Console.WriteLine(indent + " Current eval: {0}, alpha: {1}, beta: {2}", eval, alpha, beta);

            alpha = Math.Max(alpha, eval);
            board.UndoMove(move);
            if (alpha >= beta)
            {
                //Console.WriteLine(indent + " Pruning: alpha: {0}, beta: {1}", alpha, beta);
                return beta;
                break;
            }
        }
        //Console.WriteLine(indent + " return alpha: {0}", alpha);
        return alpha;
    }

    public int Search(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0)
        {
            cEvals++;
            return Evaluate(board);
        }

        Move[] legal_moves = board.GetLegalMoves();
        orderMoves(board, legal_moves);

        if (legal_moves.Length == 0)
        {
            if (board.IsInCheck())
            {
                return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
            }
            return 0;
        }

        if (board.IsWhiteToMove)
        {
            int maxEval = alpha;
            foreach (Move move in legal_moves)
            {
                board.MakeMove(move);
                int eval = Search(board, depth - 1, alpha, beta); 
                if (depth == searchDepth)
                {
                    /*if (board.PlyCount < 20)
                    {
                        if (openingMovesBoni.ContainsKey(getMoveAsString(move)))
                        {
                            eval += openingMovesBoni[getMoveAsString(move)];
                        }
                        eval += rng.Next(-20, 20);
                    }*/
                    printMoveEvals(move, eval);
                }
                board.UndoMove(move);
                if (eval > maxEval)
                {
                    maxEval = eval;
                }
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return maxEval;
        }
        else
        {
            int minEval = beta;
            foreach (Move move in legal_moves)
            {
                board.MakeMove(move);
                int eval = Search(board, depth - 1, alpha, beta);
                if (depth == searchDepth)
                {
                    /*string replMoveName = replaceMoveName(getMoveAsString(move));
                    if (board.PlyCount < 20)
                    {
                        if (openingMovesBoni.ContainsKey(replMoveName))
                        {
                            eval -= openingMovesBoni[replMoveName];
                        }
                        eval -= rng.Next(-20, 20);
                    }*/
                    printMoveEvals(move, eval);
                }
                board.UndoMove(move);
                if (eval < minEval)
                {
                    minEval = eval;
                }
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return minEval;
        }
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

    public int Evaluate(Board board)
    {
        PieceList[] piecesLists = board.GetAllPieceLists();
        int material = 0;
        for (int i = 0; i < piecesLists.Length; i++)
        {
            material += piecesLists[i].Count() * pieceVals[i];
        }

        int eval = board.IsWhiteToMove ? material : material * -1;
        //Console.WriteLine("Return evaluation; {0}", eval);
        return eval;
    }

    public int getPieceVal(Piece piece)
    {
        return pieceVals[((int)piece.PieceType)-1];
    }

    public void printMoveEvals(Move move, int eval)
    {
        Console.WriteLine("Move: {0}, Eval: {1}", getMoveAsString(move), eval);
    }

    public string replaceMoveName(string moveName)
    {
        return moveName.Replace("8", "1").Replace("7", "2").Replace("6", "3").Replace("5", "4");
    }
}