# Update .target directives to .system: format in Poppy files

$files = Get-ChildItem -Path . -Filter *.pasm -Recurse

$platformMap = @{
	'nes' = 'nes'
	'snes' = 'snes'
	'atari2600' = 'atari2600'
	'atari 2600' = 'atari2600'
	'a2600' = 'atari2600'
	'gb' = 'gameboy'
	'gameboy' = 'gameboy'
	'gba' = 'gba'
	'genesis' = 'genesis'
	'mastersystem' = 'mastersystem'
	'sms' = 'mastersystem'
	'lynx' = 'lynx'
	'turbografx16' = 'turbografx16'
	'tg16' = 'turbografx16'
	'ws' = 'wonderswan'
	'wonderswan' = 'wonderswan'
	'spc700' = 'spc700'
}

$count = 0

foreach ($file in $files) {
	$content = Get-Content $file.FullName -Raw
	$modified = $false

	# Replace .target <platform> with .system:<platform>
	foreach ($old in $platformMap.Keys) {
		$new = $platformMap[$old]
		if ($content -match "\.target\s+`"?$old`"?") {
			$content = $content -replace "\.target\s+`"?$old`"?", ".system:$new"
			$modified = $true
			Write-Host "Updated $($file.FullName): $old -> $new"
		}
	}

	# Also replace .nes with .system:nes
	if ($content -match "^\.nes\s*$") {
		$content = $content -replace "^\.nes\s*$", ".system:nes"
		$modified = $true
		Write-Host "Updated $($file.FullName): .nes -> .system:nes"
	}

	if ($modified) {
		Set-Content -Path $file.FullName -Value $content -NoNewline -Encoding UTF8
		$count++
	}
}

Write-Host "`nUpdated $count files"
