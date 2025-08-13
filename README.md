# Fortnite AFK Mover

This application simulates movements and actions in Fortnite to prevent being kicked for inactivity.

## Features

- Simulates key presses (W, S, Space) and random mouse clicks
- Allows you to choose when to start the process by left-clicking on the desired window
- Options to stop the process:
  - Set a maximum duration (in minutes)
  - Configure a custom key to stop
- Random wait times between actions (3-30 seconds)
- Random key press durations (2-5 seconds)

## Requirements

- Windows
- .NET Framework 4.7.2 or higher
- Visual Studio 2019/2022 or .NET SDK for compilation

## Compilation and Execution

1. Clone or download this repository
2. Open a terminal in the project folder
3. Run: `dotnet build`
4. Run: `dotnet run`

## Release Build

To create a release version for distribution:

1. Open a terminal in the project folder
2. Run: `dotnet publish -c Release`
3. The compiled application will be available in the `bin\Release\net472\publish` folder
4. Share the contents of this folder with users who want to run the application
5. Users only need to run the `FortniteAFK.exe` file to start the application

## Usage Instructions

1. Start the application
2. Configure if you want to set a maximum duration
3. Configure if you want to use a key to stop the process
4. Left-click on the Fortnite window to start
5. The application will begin simulating movements automatically

## Warning

Using this type of application may violate Fortnite's terms of service. Use it at your own risk.