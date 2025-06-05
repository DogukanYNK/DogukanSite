function Show-Tree {
    param (
        [string]$Path = ".",
        [int]$Level = 0
    )

    $indent = if ($Level -eq 0) { "" } else { ("│  " * ($Level - 1)) + "├─ " }

    $items = Get-ChildItem -LiteralPath $Path | Where-Object {
        $_.Name -notin @("bin", "obj")
    } | Sort-Object -Property PSIsContainer, Name

    foreach ($item in $items) {
        # Burada sadece $item.Name kullanılmalı:
        Write-Host "$indent$item"

        if ($item.PSIsContainer) {
            Show-Tree -Path $item.FullName -Level ($Level + 1)
        }
    }
}

Show-Tree
