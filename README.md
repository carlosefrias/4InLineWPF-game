# FourInLine (Connect Four)

Simple WPF implementation of Connect Four using MVVM.

## Features

- WPF UI (click column to drop a disc)
- MVVM with `GameViewModel` and `CellControl` view
- Human vs Human and Human vs Computer modes (select via `Mode` menu)
- Computer player: minimax/negamax with alpha-beta, iterative deepening and time limit
- Win detection with winning pieces highlighted and win counters

## Requirements

- .NET 8 SDK (Windows, WPF support)

## Build

From the repository root:

```powershell
dotnet build FourInLine.sln
```

## Run

Run the app (Debug):

```powershell
dotnet run --project FourInLineGame\FourInLineGame.csproj --configuration Debug
```

## Controls

- Click a column cell to drop a disc (when it's your turn).
- Use the `Mode` menu (top) to switch between `Human vs Human` and `Human vs Computer` (switching resets the board).
- `Reset` button clears the board.
- Current player and win counters are shown in the status bar.

## AI settings

- The AI (Yellow) uses `Model/AiPlayer.cs`.
- Default iterative deepening depth/time is invoked from `ViewModel/GameViewModel.cs` (currently 1200ms time limit, up to depth 8).
- To tune: edit the call to `AiPlayer.GetBestMoveWithTimeLimit` in `GameViewModel`.

## Important files

- `ViewModel/GameViewModel.cs` - main game logic and view model
- `Model/AiPlayer.cs` - AI implementation (negamax with alpha-beta)
- `View/CellControl.xaml` - cell UI
- `FourInLineGame/MainWindow.xaml` - main window and bindings

## Troubleshooting

- If build fails, run `dotnet restore` then `dotnet build` and check output.

## Next ideas

- Improve AI (bitboards, transposition table, better eval)
- Add difficulty slider / settings UI
- Add animations and prettier styles

## License

Add your project license here.
