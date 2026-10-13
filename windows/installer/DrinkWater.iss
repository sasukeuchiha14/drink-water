#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir SourcePath + "..\artifacts\publish\win-x64"
#endif
#define ProjectRoot SourcePath + "..\.."

[Setup]
AppId={{96EAB36C-A9DB-4051-A204-D12BFFCD4B89}
AppName=Drink Water
AppVersion={#AppVersion}
AppPublisher=Hardik Garg
DefaultDirName={localappdata}\Programs\DrinkWater
DefaultGroupName=Drink Water
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.17763
SetupIconFile={#ProjectRoot}\logo.ico
UninstallDisplayIcon={app}\DrinkWater.exe
OutputDir=..\artifacts\installer
OutputBaseFilename=DrinkWater-{#AppVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UsePreviousTasks=no
VersionInfoDescription=Drink Water Installer
VersionInfoProductName=Drink Water
VersionInfoVersion={#AppVersion}

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startup"; Description: "Start Drink Water when I sign in"; Flags: unchecked; Check: CanOfferStartup

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Drink Water"; Filename: "{app}\DrinkWater.exe"; WorkingDir: "{app}"; AppUserModelID: "HardikGarg.DrinkWater"
Name: "{autodesktop}\Drink Water"; Filename: "{app}\DrinkWater.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\DrinkWater.exe"; Description: "Launch Drink Water"; Flags: nowait postinstall skipifsilent

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  RunValue = 'DrinkWater';

var
  WasInstalled: Boolean;

function StartupCommand: String;
begin
  Result := '"' + ExpandConstant('{app}\DrinkWater.exe') + '"';
end;

function CanOfferStartup: Boolean;
begin
  Result := not FileExists(ExpandConstant('{app}\DrinkWater.exe')) and
    not RegValueExists(HKCU, RunKey, RunValue);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
    WasInstalled := FileExists(ExpandConstant('{app}\DrinkWater.exe'));

  if (CurStep = ssPostInstall) and not WasInstalled and
    WizardIsTaskSelected('startup') and not RegValueExists(HKCU, RunKey, RunValue) then
  begin
    if Length(StartupCommand) > 260 then
      MsgBox('The install path is too long for Windows startup registration. Drink Water was installed; startup was not enabled.', mbInformation, MB_OK)
    else if not RegWriteStringValue(HKCU, RunKey, RunValue, StartupCommand) then
      MsgBox('Drink Water was installed, but startup could not be enabled. You can try again from its tray menu.', mbInformation, MB_OK);
  end;
end;

function InitializeUninstall: Boolean;
var
  ExitCode: Integer;
begin
  Result := True;
  if FileExists(ExpandConstant('{app}\DrinkWater.exe')) then
  begin
    Result := Exec(ExpandConstant('{app}\DrinkWater.exe'), '--quit', '', SW_HIDE,
      ewWaitUntilTerminated, ExitCode) and (ExitCode = 0);
    if not Result then
      MsgBox('Please quit Drink Water from its tray menu, then run uninstall again.', mbInformation, MB_OK);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ExistingCommand: String;
begin
  if (CurUninstallStep = usUninstall) and
    RegQueryStringValue(HKCU, RunKey, RunValue, ExistingCommand) and
    (CompareText(ExistingCommand, StartupCommand) = 0) then
  begin
    if not RegDeleteValue(HKCU, RunKey, RunValue) then
      Log('Could not remove the Drink Water startup entry.');
  end;
  { Personal settings and logs are deliberately preserved on uninstall. }
end;
