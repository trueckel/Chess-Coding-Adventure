﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ChessChallenge.API;
//using ChessChallenge.Chess;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MyBot : IChessBot
{
    Random rng = new Random();
    bool debug;
    private int searchDepth;
    private Move bestMove;
    int cEvals;
    private int[] pieceVals = { 100, 290, 310, 500, 900, 0 };

    public Move Think(Board board, Timer timer)
    {
        cEvals = 0;
        debug = false;

        Move[] moves = board.GetLegalMoves();
        Console.WriteLine("####################### Legal moves: {0}", moves.Length);

        /*Console.WriteLine("New Move list ---");
        foreach (Move move in moves)
        {
            Console.WriteLine("Move {0} {1}:{2}", board.GetPiece(move.StartSquare), move.StartSquare.Name, move.TargetSquare.Name);
        }*/
        /*Console.WriteLine("After ordering ---");
        foreach (Move move in moves)
        {
            Console.WriteLine("Move {0} {1}:{2}", board.GetPiece(move.StartSquare), move.StartSquare.Name, move.TargetSquare.Name);
        }*/

        System.Random rng = new();

        switch (getNumPiecesLeft(board))
        {
            case < 4:
                searchDepth = 7;
                break;
            case < 7:
                searchDepth = 6;
                break;
            case < 10:
                searchDepth = 5;
                break;
            default:
                searchDepth = 4;
                break;
        }

        //searchDepth = 1;
        bestMove = moves[0];
        Console.WriteLine("Start Search with depth {0}", searchDepth);
        //Search(board, searchDepth, -10000, 10000, board.IsWhiteToMove);
        NegaMax(board, searchDepth, -10000, 10000);
        Console.WriteLine("ms: {0} for {1} evals", timer.MillisecondsElapsedThisTurn, cEvals);
        /*BitboardHelper.VisualizeBitboard(board.GetPieceBitboard(PieceType.Pawn, !board.IsWhiteToMove));
        ulong pawns = board.GetPieceBitboard(PieceType.Pawn, board.IsWhiteToMove);
        ulong res = pawns & (1 << 53);
        bool res_bool = res == (1 << 53);
        Console.WriteLine(Convert.ToString((long)pawns, 2));
        Console.WriteLine(res_bool);
        Console.WriteLine(Convert.ToString((long)res, 2));*/
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

            /*board.MakeMove(move);
            if (board.IsInCheck())
            {
                moveValList[index] += 10;
            }
            board.UndoMove(move);*/

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
            indent += new string('-', searchDepth - depth + 1);
        }
        if (depth == 0)
        {
            cEvals++;
            //int evaluation = Evaluate_NegaMax(board);
            int evaluation = NegaMaxCapturesOnly(board, alpha, beta);
            if (debug)
            {
                Console.WriteLine(indent + " Evaluation: {0}", evaluation);
            }
            return evaluation;
        }

        Move[] legal_moves = board.GetLegalMoves();
        orderMoves(board, legal_moves);

        if (legal_moves.Length == 0)
        {
            if (board.IsInCheck())
            {
                return -1000000;
            }
            return 0;
        }

        int maxEval = alpha;
        foreach (Move move in legal_moves)
        {
            board.MakeMove(move);
            if (debug)
            {
                Console.WriteLine(indent + "move {0}", getMoveAsString(move));
            }
            int eval = -NegaMax(board, depth - 1, -beta, -maxEval);

            if (depth == searchDepth)
            {
                Console.WriteLine(indent + "move {0}: {1}", getMoveAsString(move), eval);
            }

            if (board.GameRepetitionHistory.Contains(board.ZobristKey))
            {
                board.UndoMove(move);
                continue;
            }

            board.UndoMove(move);
            if (eval > maxEval)
            {
                maxEval = eval;
                if (depth == searchDepth)
                {

                    bestMove = move;
                }
                if (maxEval >= beta)
                {
                    //Console.WriteLine(indent + " beta cutoff, maxEval ({0}) >= beta ({1})", maxEval, beta);
                    break;
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
            indent += " capturesOnly";
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
                Console.WriteLine(indent + "move {0}", getMoveAsString(move));
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

    /*public int Evaluate(Board board)
    {
        PieceList[] piecesLists = board.GetAllPieceLists();
        int material = 0;
        int sum_white = 0, sum_black = 0;
        for (int i = 0; i < piecesLists.Length / 2; i++)
        {
            sum_white += piecesLists[i].Count * pieceVals[i];
            sum_black -= piecesLists[i+6].Count * pieceVals[i+6];
            //material += piecesLists[i].Count * pieceVals[i];
        }
        material = sum_white - sum_black;
        if (sum_white > sum_black)
        {
            material += KingToEdgeEvalutation(board);
        }
        else if (sum_white < sum_black)
        {
            material -= KingToEdgeEvalutation(board);
        }
        //material += board.IsWhiteToMove ? endgame_advantage(board) : -endgame_advantage(board);
        return material;
    }*/

    public int Evaluate_NegaMax(Board board)
    {
        PieceList[] piecesLists = board.GetAllPieceLists();
        int eval;
        int sum_white = 0, sum_black = 0;
        for (int i = 0; i < piecesLists.Length / 2; i++)
        {
            sum_white += piecesLists[i].Count * pieceVals[i];
            sum_black += piecesLists[i + 6].Count * pieceVals[i];
            //material += piecesLists[i].Count * pieceVals[i];
        }
        eval = sum_white - sum_black;
        //eval += EvaluateKingsideSafety2(board);
        /*if (sum_white - sum_black > 3)
        {
            eval += KingToEdgeEvalutation(board);
        }
        else if (sum_black - sum_white > 3)
        {
            eval -= KingToEdgeEvalutation(board);
        }*/
        //material += board.IsWhiteToMove ? endgame_advantage(board) : -endgame_advantage(board);
        if (!board.IsWhiteToMove)
        {
            eval = eval * -1;
        }
        int numPiecesLeft = getNumPiecesLeft(board);
        if (numPiecesLeft < 20)
        {
            //eval += (int)((20.0 - numPiecesLeft) / 20.0) * KingControlEvaluation(board);
            //eval += (int)((20.0 - numPiecesLeft) / 20.0) * KingToEdgeEvalutation(board);
        }
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

    public int KingToEdgeEvalutation(Board board)
    {
        Square ks = board.GetKingSquare(!board.IsWhiteToMove);
        int[] edge_bonus = { 50, 30, 10, 0, 0, 10, 30, 50 };
        int bonus = edge_bonus[ks.Rank] + edge_bonus[ks.File];
        //int manhattanDist = Math.Abs(board.GetKingSquare(true).Rank - board.GetKingSquare(false).Rank);
        //manhattanDist += Math.Abs(board.GetKingSquare(true).File - board.GetKingSquare(false).File);
        //return (14 - manhattanDist);
        Console.WriteLine("KingToEdgeEvalutation Bonus {0}", bonus);
        return bonus;
    }

    public int KingControlEvaluation(Board board)
    {
        int attackedSquaresAroundKing = 0;
        Square ks = board.GetKingSquare(!board.IsWhiteToMove);
        bool currentTurn = board.IsWhiteToMove;
        board.TrySkipTurn();
        if (currentTurn == board.IsWhiteToMove)
        {
            return 0;
        }
        for (int i = Math.Max(ks.Rank - 1, 0); i < 8; i++)
        {
            for (int j = Math.Max(ks.File - 1, 0); i < 8; i++)
            {
                attackedSquaresAroundKing += board.SquareIsAttackedByOpponent(new Square(j, i)) ? 1 : 0;
            }
        }
        board.UndoSkipTurn();
        Console.WriteLine("KingControlEvaluationMove Bonus {0}", attackedSquaresAroundKing * 3);
        return attackedSquaresAroundKing * 3;
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

    public int EvaluateKingsideSafety2(Board board)
    {
        int eval = 0;
        for (int i = 13; i < 16; i++)
        {
            if (board.GetPiece(new Square(i)).PieceType != PieceType.Pawn)
            {
                eval -= 20;
            }
        }
        for (int i = 53; i < 56; i++)
        {
            if (board.GetPiece(new Square(i)).PieceType != PieceType.Pawn)
            {
                eval += 20;
            }
        }
        return eval;
    }
}