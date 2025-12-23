using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System;
using System.Threading.Tasks;
using System.Windows;
using Model;

namespace ViewModel
{
    public class GameViewModel : INotifyPropertyChanged
    {
        public enum GameMode
        {
            HumanVsHuman,
            HumanVsComputer
        }

        private GameMode _mode = GameMode.HumanVsComputer;
        public GameMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnPropertyChanged(nameof(Mode));
                    OnPropertyChanged(nameof(IsAiEnabled));
                }
            }
        }

        public bool IsAiEnabled => Mode == GameMode.HumanVsComputer;
        public ICommand SetModeCommand { get; }
        public ObservableCollection<CellViewModel> Board { get; }
        public ICommand DropCommand { get; }
        public ICommand ResetCommand { get; }

        private bool _isGameOver;
        public bool IsGameOver
        {
            get => _isGameOver;
            set
            {
                if (_isGameOver != value)
                {
                    _isGameOver = value;
                    OnPropertyChanged(nameof(IsGameOver));
                }
            }
        }

        private int _redWins;
        public int RedWins
        {
            get => _redWins;
            set { _redWins = value; OnPropertyChanged(nameof(RedWins)); }
        }

        private int _yellowWins;
        public int YellowWins
        {
            get => _yellowWins;
            set { _yellowWins = value; OnPropertyChanged(nameof(YellowWins)); }
        }

        private string _currentPlayer = "Red";
        public string CurrentPlayer
        {
            get => _currentPlayer;
            set
            {
                if (_currentPlayer != value)
                {
                    _currentPlayer = value;
                    OnPropertyChanged(nameof(CurrentPlayer));
                }
            }
        }

        public GameViewModel()
        {
            Board = new ObservableCollection<CellViewModel>();
            _intBoard = new int[6 * 7];
            for (int i = 0; i < 6 * 7; i++)
            {
                var cell = new CellViewModel();
                cell.Index = i;
                cell.Row = i / 7;
                cell.Column = i % 7;
                Board.Add(cell);
            }

            DropCommand = new RelayCommand(param =>
            {
                if (IsGameOver) return;
                // in Human vs Computer, human only plays Red; in Human vs Human both players can click
                if (IsAiEnabled && CurrentPlayer == "Yellow") return;
                if (param is int col)
                    MakeMove(col);
                else if (param is string s && int.TryParse(s, out var c2))
                    MakeMove(c2);
            });

            ResetCommand = new RelayCommand(_ => Reset());
            SetModeCommand = new RelayCommand(param =>
            {
                if (param is string s)
                {
                    if (s == "HvH" || s == nameof(GameMode.HumanVsHuman)) Mode = GameMode.HumanVsHuman;
                    else if (s == "HvC" || s == nameof(GameMode.HumanVsComputer)) Mode = GameMode.HumanVsComputer;
                    Reset();
                }
            });
        }

        public void MakeMove(int column)
        {
            // drop piece to lowest empty cell in the column
            for (int r = 5; r >= 0; r--)
            {
                int idx = r * 7 + column;
                var cell = Board[idx];
                if (cell.State == "Empty")
                {
                    cell.State = CurrentPlayer;
                    _intBoard[idx] = CurrentPlayer == "Red" ? 1 : 2;
                    // check win
                    if (CheckWinAt(idx, CurrentPlayer, out var winningIndices))
                    {
                        foreach (var wi in winningIndices)
                            Board[wi].IsWinning = true;
                        IsGameOver = true;
                        if (CurrentPlayer == "Red") RedWins++; else YellowWins++;
                        return;
                    }

                    // swap player
                    CurrentPlayer = CurrentPlayer == "Red" ? "Yellow" : "Red";

                    // if now it's Yellow's turn, run AI
                    if (!IsGameOver && CurrentPlayer == "Yellow" && IsAiEnabled)
                    {
                        // run AI on background thread with iterative deepening and time limit
                        Task.Run(() =>
                        {
                            try
                            {
                                int best = AiPlayer.GetBestMoveWithTimeLimit(_intBoard, 2, 8, 1200);
                                // invoke on UI thread
                                Application.Current.Dispatcher.Invoke(() => MakeMove(best));
                            }
                            catch { }
                        });
                    }

                    return;
                }
            }
        }

        private int[] _intBoard;

        private void Reset()
        {
            for (int i = 0; i < Board.Count; i++)
            {
                Board[i].State = "Empty";
                Board[i].IsWinning = false;
            }
            IsGameOver = false;
            CurrentPlayer = "Red";
        }

        private bool CheckWinAt(int index, string player, out int[] winningIndices)
        {
            // board is 6 rows x 7 cols
            int rows = 6, cols = 7;
            int r = index / cols;
            int c = index % cols;

            // directions: horizontal, vertical, diag /, diag \
            var directions = new (int dr, int dc)[] { (0, 1), (1, 0), (1, 1), (1, -1) };
            foreach (var (dr, dc) in directions)
            {
                var list = new System.Collections.Generic.List<int> { index };
                // forward
                int rr = r + dr, cc = c + dc;
                while (rr >= 0 && rr < rows && cc >= 0 && cc < cols && Board[rr * cols + cc].State == player)
                {
                    list.Add(rr * cols + cc);
                    rr += dr; cc += dc;
                }
                // backward
                rr = r - dr; cc = c - dc;
                while (rr >= 0 && rr < rows && cc >= 0 && cc < cols && Board[rr * cols + cc].State == player)
                {
                    list.Insert(0, rr * cols + cc);
                    rr -= dr; cc -= dc;
                }

                if (list.Count >= 4)
                {
                    winningIndices = list.ToArray();
                    return true;
                }
            }

            winningIndices = Array.Empty<int>();
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

    public class CellViewModel : INotifyPropertyChanged
    {
        public int Index { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        private string _state = "Empty"; // "Empty", "Red", "Yellow"
        public string State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        private bool _isWinning;
        public bool IsWinning
        {
            get => _isWinning;
            set { if (_isWinning != value) { _isWinning = value; OnPropertyChanged(nameof(IsWinning)); } }
        }

        public CellViewModel()
        {
            State = "Empty";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Simple relay command implementation
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}