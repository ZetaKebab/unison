#define Name "unison"
#define Version "1.4"
#define Snapcast "snapclient_0.26.0-1_win64"
#define Publisher "Th�o Marchal"
#define URL "https://github.com/ZetaKebab/unison"
#define ExeName "unison.exe"

[Setup]
AppName={#Name}
AppVersion={#Version}
AppVerName={#Name} v{#Version}
AppPublisher={#Publisher}
AppPublisherURL={#URL}
AppSupportURL={#URL}
AppUpdatesURL={#URL}
DefaultDirName={autopf}\{#Name}
DisableProgramGroupPage=yes
ArchitecturesInstallIn64BitMode=x64
OutputBaseFilename="{#Name}-v{#Version}-setup"
OutputDir=..\publish\installer
SetupIconFile=..\Resources\icon-full.ico
UninstallDisplayIcon = "{app}\{#Name}.exe"
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
Source: "..\publish\{#ExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\unison.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\{#Snapcast}\*"; DestDir: "{app}\{#Snapcast}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#Name}"; Filename: "{app}\{#ExeName}"

[Run]
Filename: "{app}\{#Name}.exe"; Parameters: "-frominstaller"; Flags: nowait postinstall skipifsilent

