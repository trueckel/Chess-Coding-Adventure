﻿using ChessChallenge.API;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ChessChallenge.Example
{
    public class EvilBot : IChessBot
    {
        bool debug;
        private int currentSearchDepth;
        private Move[] bestMove;
        int cEvals;
        private int[] pieceVals = { 100, 290, 310, 500, 900, 0 };
        private Timer gameTimer;
        private double paraboloid_center_bonus(double x, double y) => (-Math.Pow(x - 3.5, 2) / 30 - Math.Pow(y - 3.5, 2) / 30 + 0.3) * 100;

        public Move Think(Board board, Timer timer)
        {
            gameTimer = timer;
            debug = false;

            Move[] moves = board.GetLegalMoves();

            int maxSearchDepth = 4;
            bestMove = new Move[maxSearchDepth + 1];
            bestMove[0] = moves[0];

            for (int curDepth = 1; curDepth <= maxSearchDepth; curDepth++)
            {
                cEvals = 0;
                currentSearchDepth = curDepth;
                int maxEval = NegaMax(board, curDepth, -10000000, 10000000);
                if (maxEval == 10000)
                {
                    break;
                }
                if ((double)gameTimer.MillisecondsElapsedThisTurn > (double)gameTimer.MillisecondsRemaining * 0.07)
                {
                    return bestMove[currentSearchDepth - 1];
                }
            }

            return bestMove[currentSearchDepth];
        }

        public void orderMoves(Board board, Move[] moves)
        {
            int[] moveValList = new int[moves.Length];
            int index = 0;
            foreach (Move move in moves)
            {
                Piece movePiece = board.GetPiece(move.StartSquare);
                Piece capturePiece = board.GetPiece(move.TargetSquare);

                if (capturePiece.PieceType != PieceType.None)
                {
                    moveValList[index] += getPieceVal(capturePiece) - getPieceVal(movePiece);
                }

                index++;
            }
            Array.Sort(moveValList, moves);
            Array.Reverse(moves);
        }

        public int NegaMax(Board board, int depth, int alpha, int beta)
        {

            Move[] legal_moves = board.GetLegalMoves();
            orderMoves(board, legal_moves);

            if (legal_moves.Length == 0)
            {
                if (board.IsInCheck())
                {
                    return -10000;
                }
                return 0;
            }

            if (depth == 0)
            {
                cEvals++;
                int evaluation = NegaMaxCapturesOnly(board, alpha, beta);
                return evaluation;
            }

            int maxEval = alpha;
            foreach (Move move in legal_moves)
            {
                board.MakeMove(move);
                int eval = -NegaMax(board, depth - 1, -beta, -maxEval);

                if ((double)gameTimer.MillisecondsElapsedThisTurn > (double)gameTimer.MillisecondsRemaining * 0.07)
                {
                    return beta;
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
                    if (depth == currentSearchDepth)
                    {
                        bestMove[currentSearchDepth] = move;
                    }
                    if (maxEval >= beta)
                    {
                        return beta;
                    }
                }
            }
            return maxEval;
        }

        public int NegaMaxCapturesOnly(Board board, int alpha, int beta)
        {
            cEvals++;
            int evaluation = Evaluate_NegaMax(board);
            if (evaluation >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);

            Move[] legal_moves = board.GetLegalMoves(true);
            orderMoves(board, legal_moves);

            foreach (Move move in legal_moves)
            {
                board.MakeMove(move);
                int eval = -NegaMaxCapturesOnly(board, -beta, -alpha);
                board.UndoMove(move);

                if (eval >= beta)
                {
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
            PieceList[] piecesLists = board.GetAllPieceLists();
            int eval = 0;
            int material_maxplayer = 0, material_minplayer = 0;
            for (int i = 0; i < piecesLists.Length / 2; i++)
            {
                material_maxplayer += piecesLists[i].Count * pieceVals[i];
                material_minplayer += piecesLists[i + 6].Count * pieceVals[i];
            }

            eval += KnightPositionEval(board);

            eval += EvaluateKingsPosition(board, material_maxplayer + material_minplayer);

            int king_safety = EvaluateKingsSafety(board, true) - EvaluateKingsSafety(board, false);
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
            }
            else if (material_minplayer > material_maxplayer && material_maxplayer < 900)
            {
                eval -= EvaluateKingsDistance(board);
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

        public int EvaluateKingsDistance(Board board)
        {
            int manhattanDist = Math.Abs(board.GetKingSquare(true).Rank - board.GetKingSquare(false).Rank);
            manhattanDist += Math.Abs(board.GetKingSquare(true).File - board.GetKingSquare(false).File);
            int eval = (14 - manhattanDist) * 2;
            return (14 - manhattanDist) * 2;
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

            double paraboloid_corner_bonus(double x, double y) => Math.Pow(x - 3.5, 2) / 0.245 + Math.Pow(y - 3.5, 2) / 0.1225 - 100;

            eval += paraboloid_center_bonus(board.GetKingSquare(true).File, board.GetKingSquare(true).Rank) * lategameFactor +
                paraboloid_corner_bonus(board.GetKingSquare(true).File, board.GetKingSquare(true).Rank) * 0.5 * (1.0 - lategameFactor);

            eval -= paraboloid_center_bonus(board.GetKingSquare(false).File, board.GetKingSquare(false).Rank) * lategameFactor +
                paraboloid_corner_bonus(board.GetKingSquare(false).File, board.GetKingSquare(false).Rank) * 0.5 * (1.0 - lategameFactor);

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
    }
}