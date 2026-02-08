#define AppName "AutoMerge"
#define AppVersion "0.1.0"
#define AppPublisher "AutoMerge"
#define AppExeName "AutoMerge.exe"
#define AppSourceDir "..\\..\\src\\AutoMerge.App\\bin\\Release\\net8.0\\win-x64\\publish"
#define AppIcon "..\\..\\src\\AutoMerge.UI\\Assets\\AppIcon_32.ico"

[Setup]
AppId={{B4E4F31E-0C9A-4BC5-9E16-7E5C8B8E2B7F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename={#AppName}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#AppIcon}
UninstallDisplayIcon={#AppIcon}
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#AppSourceDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\\{#AppName}"; Filename: "{app}\\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{#AppIcon}"
Name: "{commondesktop}\\{#AppName}"; Filename: "{app}\\{#AppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"; IconFilename: "{#AppIcon}"

[Run]
Filename: "{app}\\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
