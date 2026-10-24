[CmdletBinding()]
param([switch]$SemAbrir)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$framework = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue
if (-not $framework -or $framework.Release -lt 528040) {
    throw 'Instale o .NET Framework 4.8 pelo site da Microsoft antes de abrir o OpenEletric: https://dotnet.microsoft.com/download/dotnet-framework/net48'
}
$headers = @{ Accept='application/vnd.github+json'; 'User-Agent'='OpenEletric-Installer' }
$release = Invoke-RestMethod 'https://api.github.com/repos/poajefersonld-1983/OpenEletric/releases/latest' -Headers $headers
$assetName = 'OpenEletric-Windows-x64.zip'
$asset = @($release.assets | Where-Object name -eq $assetName)
$hashAsset = @($release.assets | Where-Object name -eq 'SHA256SUMS.txt')
if ($asset.Count -ne 1 -or $hashAsset.Count -ne 1) { throw 'A versao publicada ainda nao tem os arquivos completos de Windows.' }
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('OpenEletric-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    $zip = Join-Path $temporary $assetName
    $hashFile = Join-Path $temporary 'SHA256SUMS.txt'
    Invoke-WebRequest $asset[0].browser_download_url -OutFile $zip -UseBasicParsing
    Invoke-WebRequest $hashAsset[0].browser_download_url -OutFile $hashFile -UseBasicParsing
    $hashLine = @(Get-Content $hashFile | Where-Object { $_ -match ('^[0-9a-fA-F]{64}\s+\*?' + [regex]::Escape($assetName) + '$') })
    if ($hashLine.Count -ne 1) { throw 'Hash de verificacao do Windows nao encontrado.' }
    $expected = ($hashLine[0] -split '\s+')[0]
    if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne $expected) { throw 'O download nao passou na verificacao. Nenhum programa foi executado.' }
    $tag = $release.tag_name -replace '[^a-zA-Z0-9._-]', '_'
    $destination = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) ('OpenEletric\versions\' + $tag)
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Expand-Archive $zip -DestinationPath $destination -Force
    $exe = Join-Path $destination 'OpenEletric.exe'
    if (-not (Test-Path $exe)) { throw 'O executavel nao foi encontrado no pacote.' }
    $documents = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'OpenEletric'
    $examples = Join-Path $documents 'Exemplos'
    New-Item -ItemType Directory -Path $examples -Force | Out-Null
    Get-ChildItem (Join-Path $destination 'exemplos') -Filter '*.circuito' | ForEach-Object {
        $target = Join-Path $examples $_.Name
        if (-not (Test-Path $target)) { Copy-Item $_.FullName $target }
    }
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'OpenEletric.lnk'))
    $shortcut.TargetPath = $exe
    $shortcut.WorkingDirectory = $documents
    $shortcut.Description = 'OpenEletric - projetos eletricos'
    $shortcut.Save()
    Write-Host "OpenEletric pronto em: $destination"
    Write-Host "Seus projetos ficam em: $documents"
    if (-not $SemAbrir) { Start-Process -FilePath $exe -WorkingDirectory $documents }
} finally { Remove-Item $temporary -Recurse -Force -ErrorAction SilentlyContinue }
