using System.Diagnostics;

namespace Model
{
    // Simple minimax with alpha-beta for Connect Four.
    // Board representation: int[42], row-major (r*7 + c), values: 0 empty, 1 Red, 2 Yellow
    public static class AiPlayer
    {
        private const int Rows = 6;
        private const int Cols = 7;

        public static int GetBestMove(int[] board, int player, int depth = 6)
        {
            return GetBestMoveWithTimeLimit(board, player, depth, 2000);
        }

        // New: immediate win/block + iterative deepening with time limit (milliseconds)
        public static int GetBestMoveWithTimeLimit(int[] board, int player, int maxDepth = 8, int timeLimitMs = 1000)
        {
            int cols = Cols;
            int opponent = 3 - player;

            var valid = GetValidMoves(board);
            if (valid.Length == 0) return 3;

            // immediate win
            foreach (var col in valid)
            {
                var nb = (int[])board.Clone();
                MakeMove(nb, col, player);
                if (CheckWinner(nb) == player) return col;
            }

            // immediate block (if opponent can win next move)
            var oppWinning = new System.Collections.Generic.List<int>();
            foreach (var col in valid)
            {
                var nb = (int[])board.Clone();
                MakeMove(nb, col, opponent);
                if (CheckWinner(nb) == opponent) oppWinning.Add(col);
            }
            if (oppWinning.Count > 0)
            {
                // choose a blocking move among valid moves (prefer center)
                return oppWinning.OrderBy(m => Math.Abs((cols / 2) - m)).First();
            }

            // iterative deepening with time limit
            var sw = Stopwatch.StartNew();
            long deadline = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(timeLimitMs).Ticks;
            int lastBest = valid.OrderBy(m => Math.Abs((cols / 2) - m)).First();

            for (int depth = 1; depth <= maxDepth; depth++)
            {
                try
                {
                    int bestCol = lastBest;
                    int bestScore = int.MinValue;
                    var moves = valid.OrderBy(m => Math.Abs((cols / 2) - m)).ToArray();
                    foreach (var col in moves)
                    {
                        if (DateTime.UtcNow.Ticks > deadline) throw new OperationCanceledException();
                        var nb = (int[])board.Clone();
                        MakeMove(nb, col, player);
                        int score = -Negamax(nb, depth - 1, -int.MaxValue, int.MaxValue, opponent, deadline);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCol = col;
                        }
                    }
                    lastBest = bestCol;
                    // if search found a forced win, break early
                    if (bestScore >= 100000) break;
                }
                catch (OperationCanceledException)
                {
                    break; // time expired, return lastBest
                }
                if (sw.ElapsedMilliseconds >= timeLimitMs) break;
            }

            return lastBest;
        }

        private static int Negamax(int[] board, int depth, int alpha, int beta, int player)
        {
            int winner = CheckWinner(board);
            if (winner != 0)
                return (winner == player) ? 1000000 : -1000000;
            if (depth == 0)
                return Evaluate(board, player);

            var moves = GetValidMoves(board).OrderBy(m => Math.Abs(3 - m)).ToArray();
            int value = int.MinValue + 1;
            foreach (var col in moves)
            {
                var nb = (int[])board.Clone();
                MakeMove(nb, col, player);
                int score = -Negamax(nb, depth - 1, -beta, -alpha, 3 - player);
                if (score > value) value = score;
                if (value > alpha) alpha = value;
                if (alpha >= beta) break; // beta cut
            }
            return value;
        }

        // Overload with deadline check
        private static int Negamax(int[] board, int depth, int alpha, int beta, int player, long deadlineTicks)
        {
            if (DateTime.UtcNow.Ticks > deadlineTicks) throw new OperationCanceledException();
            int winner = CheckWinner(board);
            if (winner != 0)
                return (winner == player) ? 1000000 : -1000000;
            if (depth == 0)
                return Evaluate(board, player);

            var moves = GetValidMoves(board).OrderBy(m => Math.Abs(3 - m)).ToArray();
            int value = int.MinValue + 1;
            foreach (var col in moves)
            {
                if (DateTime.UtcNow.Ticks > deadlineTicks) throw new OperationCanceledException();
                var nb = (int[])board.Clone();
                MakeMove(nb, col, player);
                int score = -Negamax(nb, depth - 1, -beta, -alpha, 3 - player, deadlineTicks);
                if (score > value) value = score;
                if (value > alpha) alpha = value;
                if (alpha >= beta) break; // beta cut
            }
            return value;
        }

        private static int[] GetValidMoves(int[] board)
        {
            var list = new System.Collections.Generic.List<int>();
            for (int c = 0; c < Cols; c++)
            {
                if (board[c] == 0) // top cell empty
                    list.Add(c);
            }
            return list.ToArray();
        }

        private static void MakeMove(int[] board, int col, int player)
        {
            for (int r = Rows - 1; r >= 0; r--)
            {
                int idx = r * Cols + col;
                if (board[idx] == 0)
                {
                    board[idx] = player;
                    return;
                }
            }
        }

        private static int CheckWinner(int[] board)
        {
            // return 0 none, 1 red, 2 yellow
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    int idx = r * Cols + c;
                    int p = board[idx];
                    if (p == 0) continue;
                    // check 4 directions
                    if (c + 3 < Cols && board[idx + 1] == p && board[idx + 2] == p && board[idx + 3] == p) return p;
                    if (r + 3 < Rows && board[idx + Cols] == p && board[idx + 2 * Cols] == p && board[idx + 3 * Cols] == p) return p;
                    if (c + 3 < Cols && r + 3 < Rows && board[idx + Cols + 1] == p && board[idx + 2 * (Cols + 1)] == p && board[idx + 3 * (Cols + 1)] == p) return p;
                    if (c - 3 >= 0 && r + 3 < Rows && board[idx + Cols - 1] == p && board[idx + 2 * (Cols - 1)] == p && board[idx + 3 * (Cols - 1)] == p) return p;
                }
            return 0;
        }

        private static int Evaluate(int[] board, int player)
        {
            int score = 0;
            // center preference
            for (int r = 0; r < Rows; r++)
            {
                int idx = r * Cols + 3;
                if (board[idx] == player) score += 3;
            }

            // windows of 4
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    int[] window = new int[4];
                    // horizontal
                    if (c + 3 < Cols)
                    {
                        for (int k = 0; k < 4; k++) window[k] = board[r * Cols + c + k];
                        score += ScoreWindow(window, player);
                    }
                    // vertical
                    if (r + 3 < Rows)
                    {
                        for (int k = 0; k < 4; k++) window[k] = board[(r + k) * Cols + c];
                        score += ScoreWindow(window, player);
                    }
                    // diag down-right
                    if (r + 3 < Rows && c + 3 < Cols)
                    {
                        for (int k = 0; k < 4; k++) window[k] = board[(r + k) * Cols + c + k];
                        score += ScoreWindow(window, player);
                    }
                    // diag down-left
                    if (r + 3 < Rows && c - 3 >= 0)
                    {
                        for (int k = 0; k < 4; k++) window[k] = board[(r + k) * Cols + c - k];
                        score += ScoreWindow(window, player);
                    }
                }

            return score;
        }

        private static int ScoreWindow(int[] window, int player)
        {
            int opponent = 3 - player;
            int countPlayer = window.Count(x => x == player);
            int countEmpty = window.Count(x => x == 0);
            int countOpp = window.Count(x => x == opponent);

            if (countPlayer == 4) return 100000;
            if (countPlayer == 3 && countEmpty == 1) return 100;
            if (countPlayer == 2 && countEmpty == 2) return 10;
            if (countOpp == 3 && countEmpty == 1) return -80;
            return 0;
        }
    }
}
