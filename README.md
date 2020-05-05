# ParallelRisk

This library provides an easy way to run minimax and alpha-beta pruning, both in serial and in parallel, for a simplified version of Hasbro's Risk.

## Running the Console Application

You can find already-built versions of the program in the **Releases** tab of GitHub.

If you have downloaded or built a standalone excutable, simply run the following, or the equivalent on Linux or OSX:

```shell
ParallelRiskConsole.exe <YOUR ARGUMENTS HERE>
```

Otherwise, if you have downloaded the cross-platform dll file, install [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) and run:

```shell
dotnet run ParallelRiskConsole.dll <YOUR ARGUMENTS HERE>
```

To view possible command line arguments, use the `--help` argument.

## Building the Application

Install the [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) and run:

```shell
dotnet build --configuration Release
```

## Project Layout

### ParallelRiskConsole Application

This application provides an easy way to test the library using the command line. Modify `Program.cs` if you need to change the behavior of the driver program. The [CommandLineParser](https://github.com/commandlineparser/commandline) library was used to simplify commandline access; consult their documentation for information on setting up new command line arguments.

### ParallelRisk Library

This library contains the majority of the code in the project, and is intended to be incorporated into other applications, such as the console application, or [ParallelRiskInteractive](https://github.com/ssandif/ParallelRiskInteractive).

#### Algorithm Files

* `Minimax.cs`: Variations on the minimax algorithm
* `AlphaBeta.cs`: Variations on the minimax algorithm with alpha-beta pruning
* `IState.cs`: Interface for a game state, used by `Minimax.cs` and `AlphaBeta.cs`
* `IMove.cs`: Interface for a move, used by `Minimax.cs` and `AlphaBeta.cs`

#### Game Files

* `Risk.cs`: Allows for easy construction of a randomized game state based on the standard map of Risk.
* `BoardState.cs`: Represents the current state of the board
* `Move.cs`: Represents a possible move from a `BoardState`
* `Continent.cs`: Represents a continent on the board
* `Territory.cs`: Represents a territory on the board
* `Player.cs`: Enum that identifies the max vs. min vs. neutral player

#### Utility Files

* `ControlledThreadPool.cs`: A custom "thread pool"-like construct that runs functions sent to it on its worker tasks, if available, and otherwise runs it on the current thread
* `ImmutableAdjacencyMatrix.cs`: Implementation of an adjacency matrix for an undirected graph

### Miscellaneous Notes

* Many of the types involved are immutable `readonly struct`s. Be careful to remember to use the new `struct` returned by functions that appear to modify it. Also, consider consulting the [documentation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/in-parameter-modifier) on `in` parameters and why they are used in certain places.
* [Visual Studio](https://visualstudio.microsoft.com) and [Visual Studio Code](https://code.visualstudio.com) are convenient editor options for modifying and building the code.
