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

Windows Installer
-----------------
Requires Inno Setup (https://jrsoftware.org/isinfo.php). Install it to get
ISCC.exe on your machine.

Publish:
  dotnet publish src/AutoMerge.App -c Release -r win-x64

Build installer:
  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer/Windows/AutoMerge.iss

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

Git Mergetool Integration
-------------------------
Configure AutoMerge as your default merge tool:

    git config --global merge.tool automerge
    git config --global mergetool.automerge.cmd \
        'automerge --base "$BASE" --local "$LOCAL" --remote "$REMOTE" --merged "$MERGED"'
    git config --global mergetool.automerge.trustExitCode true

Works with SourceTree, Fork, GitKraken, Tower, and any client that supports
custom merge tools. Exit code 0 = resolved, 1 = cancelled.

AI Model Selection
------------------
AutoMerge ships with a bundled model catalog (src/AutoMerge.App/ai-models.xml)
that currently includes:

  - GPT-5 mini (default)
  - GPT-5.2-Codex
  - Claude Sonnet 4.5
  - Claude Opus 4.6
  - Claude Haiku 4.5

Change the active model in Preferences (Ctrl+,) or edit ai-models.xml to add
custom model identifiers.

Platform Notes
--------------
  - Windows 10/11: fully supported.
  - macOS 12+: supported.
  - Linux: not supported yet.

Configuration Storage
---------------------
  - Windows: preferences stored in Registry (HKCU\Software\AutoMerge).
  - macOS: preferences stored as JSON in ~/Library/Application Support/AutoMerge.

Testing
-------
Run all tests (5 test projects):
    dotnet test AutoMerge.sln

Run tests only (dedicated test solution):
    dotnet test AutoMerge.Tests.sln

Run a single test project:
    dotnet test tests/AutoMerge.Core.Tests/AutoMerge.Core.Tests.csproj
    dotnet test tests/AutoMerge.Logic.Tests/AutoMerge.Logic.Tests.csproj
    dotnet test tests/AutoMerge.Infrastructure.Tests/AutoMerge.Infrastructure.Tests.csproj
    dotnet test tests/AutoMerge.UI.Tests/AutoMerge.UI.Tests.csproj
    dotnet test tests/AutoMerge.Integration.Tests/AutoMerge.Integration.Tests.csproj

Troubleshooting
---------------
Copilot CLI not authenticated:
  - Run `copilot` and follow the /login prompt.

SDK package not found:
  - The Copilot SDK is in technical preview. Check the official GitHub repo:
    https://github.com/github/copilot-sdk

Project Structure
-----------------
  src/AutoMerge.Core            Domain models, interfaces, pure logic (zero deps)
  src/AutoMerge.Logic     Use-case handlers, session management, events
  src/AutoMerge.Infrastructure  Copilot SDK, file I/O, diff, configuration
  src/AutoMerge.UI              Avalonia views, ViewModels, controls, converters
  src/AutoMerge.App             Entry point, CLI parser, DI composition root
  tests/                        Five test projects (Core, Application,
                                Infrastructure, UI, Integration)

More Details
------------
See Specs/AutoMerge_PRD.md and Specs/AutoMerge_SPEC.md for architecture, CLI
requirements, and design details.

================================================================================
                              END OF README
================================================================================
