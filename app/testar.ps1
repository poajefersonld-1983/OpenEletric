param([string[]]$Only)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$names = @('Program','Domain','Exports','Engineering','AdvancedUI','EngineeringReport','Catalog','CableSelection','XlsxWorkbook','Enclosures','Curves','ProjectWizard','DiagramSvg','ReportPrinting','PhaseBalancing','DriveMotor','LegacyXls','DiagramAppearance','ProjectMetadata','ProjectEditing','SourceNetwork','FaultDynamics','FaultWaveform','ReportSelection','ReportPdf','CatalogSearch','CatalogBrowser','WizardSourceEditor','MotorStarting','MotorStartingForm','CableFactors','CableFactorsForm','BreakerSelection','BreakerSelectionForm','CalculationProfile','CalculationProfileForm','Coordination','CoordinationForm','SwitchgearSelection','SwitchgearSelectionForm','ProtectionResponses','ProtectionResponsesForm','ComponentCsv','ResidualSelection','ResidualSelectionForm','MotorStarterSelection','MotorStarterSelectionForm','DiagramEditing','Grounding','GroundingForm','ReturnSizing','ReturnSizingForm','FaultLoops','FaultLoopsForm','ContactTime','ContactTimeForm','NetworkFlow','NetworkFlowForm','FaultExtremes','FaultExtremesForm','FaultTimeTrace','FaultTimeTraceForm','BusbarSelection','BusbarSelectionForm','EnclosureCatalog','EnclosureCatalogForm','DrawingDxf','DrawingDxfForm','DrawingAmb')
$sources = $names | ForEach-Object { Join-Path $PSScriptRoot ($_ + '.cs') }
$output = Join-Path $PSScriptRoot 'verificacao'
New-Item -ItemType Directory -Path $output -Force | Out-Null
# Independent input fixtures, copied without requiring previous generated outputs.
foreach ($folder in @('referencias-involucros','referencias-dxf','referencias-amb')) {
    $source = Join-Path $PSScriptRoot ('amostras-testes\' + $folder)
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $output -Recurse -Force }
}
$libraryArguments = & (Join-Path $PSScriptRoot 'dependencias.ps1')
$log = @()
$allTests = @('DomainTests','EngineeringTests','SourceNetworkTests','FaultDynamicsTests','FaultWaveformTests','ReportPrintingTests','ReportSelectionTests','ReportPdfTests','ExportTests','CatalogTests','CatalogSearchTests','CableSelectionTests','CableFactorTests','CableSizingTests','BreakerSelectionTests','CalculationProfileTests','CoordinationTests','SwitchgearSelectionTests','ProtectionResponsesTests','ResidualSelectionTests','MotorStarterSelectionTests','DiagramEditingTests','GroundingTests','ReturnSizingTests','FaultLoopsTests','ContactTimeTests','NetworkFlowTests','FaultExtremeTests','FaultTimeTraceTests','BusbarSelectionTests','XlsxTests','LegacyXlsTests','DrawingDxfTests','DrawingAmbTests','EnclosureCatalogTests','EnclosureTests','CurvesTests','CurvePersistenceTests','PhaseBalancingTests','DriveMotorTests','DiagramAppearanceTests','ProjectMetadataTests','ProjectEditingTests','CrashRecoveryTests','IntegrationTests','GeneratorTests','DefinedSourceTests','WizardSourceTests','VoltageDropTests','MotorStartingTests','LoadReferenceTests','ExportReferenceTests','IndustrialScenarioTests','DesktopLaunchTests')
$selectedTests = $allTests
if ($Only) { foreach ($name in $Only) { if ($name -notin $allTests) { throw "Teste desconhecido: $name" } }; $selectedTests = $Only }
foreach ($test in $selectedTests) {
    $target = Join-Path $output ($test + '.exe')
    $arguments = @('/nologo', '/target:exe', "/main:$test", "/out:$target", '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Runtime.Serialization.dll', '/r:System.Core.dll', '/r:System.Numerics.dll', '/r:System.Xml.Linq.dll', '/r:System.IO.Compression.dll', '/r:System.IO.Compression.FileSystem.dll') + $libraryArguments + $sources + (Join-Path $PSScriptRoot ($test + '.cs'))
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw "Falha na compilação de $test" }
    $result = & $target 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Falha em ${test}: $result" }
    $line = "${test}: $result"
    $log += $line
    Write-Output $line
}
$logFile = if ($Only) { 'resultados-parciais.txt' } else { 'resultados-testes.txt' }
$log | Set-Content -LiteralPath (Join-Path $output $logFile) -Encoding UTF8
