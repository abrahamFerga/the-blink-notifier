# Generates placeholder PNG icons for Store/MSIX submission.
# Replace the output files with real artwork before submitting to the Microsoft Store.
# Run from the Images\ directory: pwsh ./generate-icons.ps1
Add-Type -AssemblyName System.Drawing

$accent = [System.Drawing.Color]::FromArgb(0x26, 0x8B, 0xD2)   # Blink blue

function New-Icon {
    param([string]$Path, [int]$Width, [int]$Height)
    $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $margin = [int]([Math]::Min($Width, $Height) * 0.1)
    $g.FillEllipse([System.Drawing.SolidBrush]::new($accent),
        $margin, $margin, $Width - 2*$margin, $Height - 2*$margin)
    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Created $Path ($Width x $Height)"
}

$here = $PSScriptRoot

New-Icon "$here\Square44x44Logo.png"    44   44
New-Icon "$here\Square150x150Logo.png" 150  150
New-Icon "$here\Wide310x150Logo.png"   310  150
New-Icon "$here\StoreLogo.png"          50   50
New-Icon "$here\SplashScreen.png"      620  300

Write-Host "Done. Replace with real artwork before Store submission."
