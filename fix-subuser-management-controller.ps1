# Remove AccessLevel and JobTitle from SubuserManagementController
# Quick PowerShell fix script

$file = "BitRaserApiProject\Controllers\SubuserManagementController.cs"

Write-Host "Fixing SubuserManagementController.cs..." -ForegroundColor Cyan

# Read file
$content = Get-Content $file -Raw

# Remove AccessLevel references
$content = $content -replace 'JobTitle\s*=\s*subuser\.JobTitle,', ''
$content = $content -replace 'JobTitle\s*=\s*dto\.JobTitle,', ''
$content = $content -replace 'if\s*\(dto\.JobTitle\s*!=\s*null\)\s*subuser\.JobTitle\s*=\s*dto\.JobTitle;', ''
$content = $content -replace '\(s\.JobTitle\s*!=\s*null\s*&&\s*s\.JobTitle\.Contains\(search\)\)', ''

$content = $content -replace 'AccessLevel\s*=\s*subuser\.AccessLevel,', ''
$content = $content -replace 'AccessLevel\s*=\s*dto\.AccessLevel\s*\?\?\s*"limited",', ''
$content = $content -replace 'if\s*\(dto\.AccessLevel\s*!=\s*null\)\s*subuser\.AccessLevel\s*=\s*dto\.AccessLevel;', ''
$content = $content -replace 'if\s*\(!string\.IsNullOrEmpty\(accessLevel\)\)', 'if (false) // AccessLevel removed'
$content = $content -replace 'query\s*=\s*query\.Where\(s\s*=>\s*s\.AccessLevel\s*==\s*accessLevel\);', '// AccessLevel removed'

$content = $content -replace 'SubusersByAccessLevel\s*=\s*await\s*query\s*\.GroupBy\(s\s*=>\s*s\.AccessLevel\).*?\.ToDictionaryAsync\(x\s*=>\s*x\.AccessLevel,\s*x\s*=>\s*x\.Count\),', ''

# Clean up extra commas
$content = $content -replace ',\s*,', ','
$content = $content -replace ',\s*\}', ' }'

# Save file
$content | Set-Content $file -Encoding UTF8

Write-Host "✅ Fixed!" -ForegroundColor Green
Write-Host "Now run: dotnet build" -ForegroundColor Yellow
