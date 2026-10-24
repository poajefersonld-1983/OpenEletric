$ErrorActionPreference = 'Stop'
$directory = Join-Path $PSScriptRoot 'third_party\ExcelDataReader'
$assemblies = @(
    @{ File = 'ExcelDataReader.dll'; Hash = '03050036BADC7651F357A57CDC150451FC021CDB56E31918F6A92D8D6629B844' },
    @{ File = 'System.ValueTuple.dll'; Hash = 'E4774AEAD2793F440E0CED6C097048423D118E0B6ED238C6FE5B456ACB07817F' }
)
foreach ($assembly in $assemblies) {
    $path = Join-Path $directory $assembly.File
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $assembly.Hash) { throw "Componente alterado: $($assembly.File)" }
    "/r:$path"
    "/resource:$path,CircuitoStudio.$($assembly.File)"
}
foreach ($license in @(@('LICENSE.txt','Legal.ExcelDataReader'), @('System.ValueTuple.LICENSE.txt','Legal.ValueTuple'), @('System.ValueTuple.NOTICES.txt','Legal.ValueTuple.Notices'))) {
    $path = Join-Path $directory $license[0]
    if (!(Test-Path -LiteralPath $path)) { throw "Licença ausente: $path" }
    "/resource:$path,$($license[1])"
}
'/r:System.Data.dll'

$pdfDirectory = Join-Path $PSScriptRoot 'third_party\PDFsharp'
$pdfManifest = Get-Content -LiteralPath (Join-Path $pdfDirectory 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($package in $pdfManifest) {
    foreach ($assembly in $package.Assemblies) {
        $path = Join-Path $pdfDirectory $assembly.File
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $assembly.Hash) { throw "Componente PDF alterado: $($assembly.File)" }
        $name = [IO.Path]::GetFileName($path)
        if ($name -eq 'PdfSharp-gdi.dll') { "/r:PdfEngine=$path" }
        elseif ($name.StartsWith('PdfSharp', [StringComparison]::Ordinal)) { "/r:PdfSupport=$path" }
        "/resource:$path,CircuitoStudio.$name"
    }
}
$pdfLicense = Join-Path $pdfDirectory 'LICENSES.txt'
if ((Get-FileHash -LiteralPath $pdfLicense -Algorithm SHA256).Hash -ne 'FFF42F7C7430750EF1A0DECDDFBA7A7AB5458A3883D4C1062628C4FC8C63A544') { throw 'Licenças PDF alteradas.' }
"/resource:$pdfLicense,Legal.PDFsharp"
$frameworkDirectory = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
"/r:$(Join-Path $frameworkDirectory 'netstandard.dll')"
"/r:$(Join-Path $frameworkDirectory 'WPF\PresentationCore.dll')"
"/r:$(Join-Path $frameworkDirectory 'WPF\WindowsBase.dll')"
