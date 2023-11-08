using ChessChallenge.API;
using System.Collections.Generic;
using System;

namespace ChessChallenge.Example
{
    public class EvilBot : IChessBot
    {
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
        { "Ke1g1", 50 },
        { "Ke1c1", 50 },
    };

        public Move Think(Board board, Timer timer)
        {
            cEvals = 0;

            Move[] moves = board.GetLegalMoves();
            Console.WriteLine("Legal moves: {0}", moves.Length);

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

            /*int numPieces = 0;
            foreach (PieceList list in board.GetAllPieceLists())
            {
                numPieces += list.Count();
            }*/

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


            bestMove = moves[0];
            Console.WriteLine("Start Search with depth {0}", searchDepth);
            Search(board, searchDepth, int.MinValue, int.MaxValue, board.IsWhiteToMove);
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

        /*public int SearchMajorResponses(Board board, int alpha, int beta, bool maximizingPlayer)
        {
            Move[] legal_moves = board.GetLegalMoves();
            List<Move> filteredMoves = new List<Move>();
            foreach (Move move in legal_moves)
            {
                if (move.CapturePieceType != PieceType.None)
                {
                    filteredMoves.Add(move);
                    continue;
                }
                board.MakeMove(move);
                if (board.IsInCheck())
                {
                    filteredMoves.Add(move);
                }
                board.UndoMove(move);
            }

            orderMoves(board, legal_moves);

            if (legal_moves.Length == 0)
            {
                if (board.IsInCheck())
                {
                    return maximizingPlayer ? int.MinValue : int.MaxValue;
                }
                return 0;
            }

            if (maximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (Move move in legal_moves)
                {
                    board.MakeMove(move);
                    int eval = SearchMajorResponses(board, alpha, beta, false);
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
                int minEval = int.MaxValue;
                foreach (Move move in legal_moves)
                {
                    board.MakeMove(move);
                    int eval = SearchMajorResponses(board, alpha, beta, true);
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


        }*/

        public int Search(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
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
                    return maximizingPlayer ? int.MinValue : int.MaxValue;
                }
                return 0;
            }

            if (maximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (Move move in legal_moves)
                {
                    board.MakeMove(move);
                    int eval = Search(board, depth - 1, alpha, beta, false);
                    board.UndoMove(move);
                    if (eval > maxEval)
                    {
                        maxEval = eval;
                        if (depth == searchDepth)
                        {
                            bestMove = move;
                        }
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
                int minEval = int.MaxValue;
                foreach (Move move in legal_moves)
                {
                    board.MakeMove(move);
                    int eval = Search(board, depth - 1, alpha, beta, true);
                    board.UndoMove(move);
                    if (eval < minEval)
                    {
                        minEval = eval;
                        if (depth == searchDepth)
                        {
                            bestMove = move;
                        }
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
                material += piecesLists[i].Count * pieceVals[i];
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
    }
}