; Inno Setup Script for TypingTamagotchi

[Setup]
AppName=TypingTamagotchi
AppVersion=1.0
AppPublisher=TypingTamagotchi
DefaultDirName={autopf}\TypingTamagotchi
DefaultGroupName=TypingTamagotchi
OutputDir=installer_output
OutputBaseFilename=TypingTamagotchi_Setup
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
Name: "{group}\TypingTamagotchi"; Filename: "{app}\TypingTamagotchi.exe"
Name: "{autodesktop}\TypingTamagotchi"; Filename: "{app}\TypingTamagotchi.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "바탕화면에 바로가기 만들기"; GroupDescription: "추가 아이콘:"

[Run]
Filename: "{app}\TypingTamagotchi.exe"; Description: "TypingTamagotchi 실행"; Flags: nowait postinstall skipifsilent
