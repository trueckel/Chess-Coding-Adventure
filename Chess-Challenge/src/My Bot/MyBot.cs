﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MyBot : IChessBot
{
    bool debug;
    private int searchDepth;
    private Move bestMove;
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

        int numPieces = 0;
        foreach (PieceList list in board.GetAllPieceLists())
        {
            numPieces += list.Count();
        }

        switch (numPieces)
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

        //searchDepth = 2;
        bestMove = moves[0];
        Console.WriteLine("Start Search with depth {0}", searchDepth);
        //Search(board, searchDepth, -10000, 10000, board.IsWhiteToMove);
        NegaMax(board, searchDepth, -10000, 10000);
        Console.WriteLine("ms: {0} for {1} evals", timer.MillisecondsElapsedThisTurn, cEvals);
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
            int evaluation = Evaluate_NegaMax(board);
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

            board.UndoMove(move);
            if (eval > maxEval)
            {
                maxEval = eval;
                if (depth == searchDepth)
                {
                    bestMove = move;
                }
                if (maxEval >= beta) {
                    Console.WriteLine(indent + " beta cutoff, maxEval ({0}) >= beta ({1})", maxEval, beta);
                    break;
                }
            }
        }
        return maxEval;
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
            material += endgame_advantage(board);
        }
        else if (sum_white < sum_black)
        {
            material -= endgame_advantage(board);
        }
        //material += board.IsWhiteToMove ? endgame_advantage(board) : -endgame_advantage(board);
        return material;
    }

    public int Evaluate_NegaMax(Board board)
    {
        PieceList[] piecesLists = board.GetAllPieceLists();
        int material = 0;
        int sum_white = 0, sum_black = 0;
        for (int i = 0; i < piecesLists.Length / 2; i++)
        {
            sum_white += piecesLists[i].Count * pieceVals[i];
            sum_black -= piecesLists[i + 6].Count * pieceVals[i + 6];
            //material += piecesLists[i].Count * pieceVals[i];
        }
        material = sum_white - sum_black;
        if (sum_white > sum_black)
        {
            material += endgame_advantage(board);
        }
        else if (sum_white < sum_black)
        {
            material -= endgame_advantage(board);
        }
        //material += board.IsWhiteToMove ? endgame_advantage(board) : -endgame_advantage(board);
        if (!board.IsWhiteToMove)
        {
            material = material * -1;
        }
        return material;
    }

    public int getPieceVal(Piece piece)
    {
        return pieceVals[((int)piece.PieceType) - 1];
    }

    public void printMoveEvals(Move move, int eval)
    {
        Console.WriteLine("Move: {0}, Eval: {1}", getMoveAsString(move), eval);
    }

    public string replaceMoveName(string moveName)
    {
        return moveName.Replace("8", "1").Replace("7", "2").Replace("6", "3").Replace("5", "4");
    }

    public int endgame_advantage(Board board)
    {
        int manhattanDist = Math.Abs(board.GetKingSquare(true).Rank - board.GetKingSquare(false).Rank);
        manhattanDist += Math.Abs(board.GetKingSquare(true).File - board.GetKingSquare(false).File);
        return (14 - manhattanDist);
    }
}