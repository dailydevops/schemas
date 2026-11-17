<#
.SYNOPSIS
    Verifies the content of every .nupkg in a directory before any release exists.
.DESCRIPTION
    Per package: the four target framework folders, the dependency list, the readme,
    the license metadata, and the absence of conformance suite or rule set data.
    Exits 1 when any package violates a check, 0 otherwise.
.PARAMETER PackageDirectory
    Directory that contains the .nupkg files.
.PARAMETER AllowMissingReadme
    Limitation until package readmes exist (REL-03 requires zero gaps): a missing readme is
    reported as a KNOWN GAP and does not fail the run.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackageDirectory,
    [switch]$AllowMissingReadme
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$requiredFolders = @('netstandard2.0', 'netstandard2.1', 'net10.0', 'net11.0')
$polyfills = @('Microsoft.Bcl.AsyncInterfaces', 'System.Memory', 'System.Threading.Tasks.Extensions')
$suiteSegments = @('conformance', 'suites', 'rulesets')
$suiteExtensions = @('.sch', '.xsl', '.xslt')

$failures = 0
$gaps = [System.Collections.Generic.List[string]]::new()

$packages = @(Get-ChildItem -Path $PackageDirectory -Filter '*.nupkg' -File | Where-Object { $_.Name -notlike '*.snupkg' })
if ($packages.Count -eq 0) {
    Write-Host "::error::No .nupkg found in $PackageDirectory"
    exit 1
}

foreach ($package in $packages) {
    $errors = [System.Collections.Generic.List[string]]::new()
    $zip = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $entries = @($zip.Entries | ForEach-Object { $_.FullName })

        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' -and $_.FullName -notlike '*/*' } | Select-Object -First 1
        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $meta = $nuspec.package.metadata
        $id = [string]$meta.id

        # Target framework folders
        $folders = @($requiredFolders | Where-Object { $tfm = $_; $entries | Where-Object { $_ -like "lib/$tfm/*.dll" } })
        foreach ($tfm in $requiredFolders) {
            if ($folders -notcontains $tfm) { $errors.Add("missing folder lib/$tfm") }
        }

        # Dependencies
        # Grouped and ungrouped <dependency> elements; an ungrouped one has no target framework.
        foreach ($dependency in @($nuspec.SelectNodes("//*[local-name()='dependency']"))) {
            if ($null -eq $dependency) { continue }
            $group = $dependency.ParentNode
            $targetFramework = if ($group.LocalName -eq 'group') { $group.GetAttribute('targetFramework') } else { '' }
            $depId = [string]$dependency.id
            $allowed = ($depId -eq 'NetEvolve.Schemas') -or $depId.StartsWith('NetEvolve.Schemas.', [System.StringComparison]::Ordinal) `
                -or ($depId -eq 'NetEvolve.Arguments') `
                -or (($polyfills -contains $depId) -and $targetFramework -eq '.NETStandard2.0') `
                -or ($depId -eq 'Microsoft.Extensions.DependencyInjection.Abstractions' -and $id -eq 'NetEvolve.Schemas.DependencyInjection')
            if (-not $allowed) { $errors.Add("disallowed dependency $depId in group '$targetFramework'") }
        }

        # License
        if ($meta.license.type -ne 'expression' -or $meta.license.InnerText -ne 'MIT') {
            $errors.Add('license expression is not MIT')
        }
        if ($meta.requireLicenseAcceptance -eq 'true') { $errors.Add('package requires license acceptance') }

        # Readme
        $readme = [string]$meta.readme
        if ([string]::IsNullOrEmpty($readme) -or $entries -notcontains $readme) {
            if ($AllowMissingReadme) { $gaps.Add($id) } else { $errors.Add('readme missing') }
        }

        # Suite data
        foreach ($entry in $entries) {
            $segments = $entry.ToLowerInvariant().Split('/')
            $extension = [System.IO.Path]::GetExtension($entry).ToLowerInvariant()
            if (@($segments | Where-Object { $suiteSegments -contains $_ }).Count -gt 0 -or $suiteExtensions -contains $extension) {
                $errors.Add("suite or rule set data: $entry")
            }
        }
    }
    finally { $zip.Dispose() }

    Write-Host "$($package.Name): folders $(($folders | ForEach-Object { "lib/$_" }) -join ', ')"
    foreach ($e in $errors) { Write-Host "::error::$($package.Name): $e" }
    $failures += $errors.Count
}

foreach ($gap in $gaps) { Write-Host "KNOWN GAP: $gap has no package readme (REL-03 requires zero gaps)" }
exit ($failures -gt 0 ? 1 : 0)
