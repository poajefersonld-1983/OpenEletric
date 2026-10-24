$ErrorActionPreference = 'Stop'
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class OpenEletricTestFonts {
    [DllImport("gdi32.dll", CharSet=CharSet.Unicode)]
    public static extern int AddFontResource(string path);
}
'@
$fonts = Join-Path $PSScriptRoot '..\test-resources\fonts'
Get-ChildItem $fonts -Filter '*.ttf' | ForEach-Object {
    $destination = Join-Path "$env:WINDIR\Fonts" $_.Name
    Copy-Item $_.FullName $destination -Force
    New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts' -Name $_.BaseName -Value $_.Name -PropertyType String -Force | Out-Null
    if ([OpenEletricTestFonts]::AddFontResource($destination) -eq 0) { throw "Nao foi possivel registrar a fonte $($_.Name)" }
}
