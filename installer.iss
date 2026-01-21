; Inno Setup Script for TypingTamagotchi

[Setup]
AppName=TypeCreature
AppVersion=1.1
AppPublisher=Sean
DefaultDirName={autopf}\TypeCreature
DefaultGroupName=TypeCreature
OutputDir=installer_output
OutputBaseFilename=TypeCreature_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
SetupIconFile=installer_icon.ico
UninstallDisplayIcon={app}\TypingTamagotchi.exe

[Files]
; 메인 실행 파일 및 DLL
Source: "TypingTamagotchi\publish\TypingTamagotchi.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "TypingTamagotchi\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; Assets 폴더
Source: "TypingTamagotchi\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\TypeCreature"; Filename: "{app}\TypingTamagotchi.exe"; IconFilename: "{app}\Assets\app.ico"
Name: "{autodesktop}\TypeCreature"; Filename: "{app}\TypingTamagotchi.exe"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "바탕화면에 바로가기 만들기"; GroupDescription: "추가 아이콘:"; Flags: checked

[Run]
Filename: "{app}\TypingTamagotchi.exe"; Description: "TypeCreature 실행"; Flags: nowait postinstall skipifsilent
