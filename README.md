# Strategy-Game-Unity

A turn based strategy game developed in C# with a Unity frontend.

## Repository Setup

Third party assets are stored in a submodule
found [here](https://github.com/Anthony-de-cruz/Strategy-Game-Unity-Third-Party-Assets).

Add the `--recursive` arg when you clone the repository.

Alternatively, download the submodule with: `git submodule update --init --recursive`.

## Build

Build the GameLogic library _before_ trying to open or build the Unity project. This can be done via `dotnet build` or
via your IDE. `dotnet` likely wil not be able to load the Unity project referenced in the solution, as Unity is needed to generate it.

Once the GameLogic library is build, you can load up the unity project and build as normal.