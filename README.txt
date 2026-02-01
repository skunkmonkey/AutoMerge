================================================================================
                                 AUTOMERGE
                    AI-Powered Merge Conflict Resolution Tool
================================================================================

Quickstart
----------

Prerequisites:
  - .NET 8 SDK
  - Git
  - GitHub Copilot CLI installed and authenticated (Copilot subscription required)

Build:
    dotnet restore
    dotnet build AutoMerge.sln

Run (CLI mode):
    dotnet run --project src/AutoMerge.App -- --base path/to/base.txt --local path/to/local.txt --remote path/to/remote.txt --merged path/to/merged.txt

    dotnet run --project src/AutoMerge.App -- BASE LOCAL REMOTE MERGED

    dotnet run --project src/AutoMerge.App -- --local path/to/local.txt --remote path/to/remote.txt --merged path/to/merged.txt

    dotnet run --project src/AutoMerge.App -- --help

Run (GUI only):
    dotnet run --project src/AutoMerge.App

Notes:
  - The GUI is the primary app. The CLI arguments launch the same UI with a
    conflict session loaded.
  - In GUI-only mode, click "Open Merge..." and select Local, Remote, and
    Merged (output). Base is optional.
  - --no-gui is reserved for a future release.

Platform Notes
--------------
  - Windows 10/11: fully supported.
  - macOS 12+: supported.
  - Linux: not supported yet.

Testing
-------
Run all tests:
    dotnet test AutoMerge.sln

Run a single test project:
    dotnet test tests/AutoMerge.UI.Tests/AutoMerge.UI.Tests.csproj

Troubleshooting
---------------
Copilot CLI not authenticated:
  - Run `copilot` and follow the /login prompt.

SDK package not found:
  - The Copilot SDK is in technical preview. Check the official GitHub repo:
    https://github.com/github/copilot-sdk

More Details
------------
See Specs/AutoMerge_PRD.md and Specs/AutoMerge_SPEC.md for architecture, CLI
requirements, and design details.

================================================================================
                              END OF README
================================================================================
