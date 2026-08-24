# Завантажує зображення карт для драфту.
#
# На відміну від героїв, офіційного CDN для карт немає: Data Dragon і CDN
# Valve віддають портрети, а карти — ні. Тому джерела тут задані вручну, і
# після ротації Active Duty їх доводиться правити руками, як і сам пул у
# Common/GameMaps.cs.
#
# Імена файлів мусять збігатися зі значеннями GameMaps.cs (Dust2.jpg), інакше
# import.meta.glob їх не знайде і плитка лишиться з самим підписом.

param(
    [string]$OutputRoot = (Join-Path $PSScriptRoot "..\Esport-Frontend\src\assets\maps")
)

$ErrorActionPreference = "Stop"

# Джерело на кожну карту. Заповніть URL перед запуском: сталого каталогу,
# з якого їх можна взяти автоматично, не існує.
$Sources = @{
    "CS2" = @{
        "Ancient"  = ""
        "Anubis"   = ""
        "Dust2"    = ""
        "Inferno"  = ""
        "Mirage"   = ""
        "Nuke"     = ""
        "Overpass" = ""
    }
    "Valorant" = @{
        "Abyss"  = ""
        "Ascent" = ""
        "Haven"  = ""
        "Lotus"  = ""
        "Split"  = ""
        "Summit" = ""
        "Sunset" = ""
    }
}

$missing = @()

foreach ($game in $Sources.Keys) {
    $folder = Join-Path $OutputRoot $game
    New-Item -ItemType Directory -Force -Path $folder | Out-Null

    foreach ($map in $Sources[$game].Keys) {
        $url = $Sources[$game][$map]
        $target = Join-Path $folder "$map.jpg"

        if ([string]::IsNullOrWhiteSpace($url)) {
            $missing += "$game/$map"
            continue
        }

        Write-Host "$game/$map <- $url"
        Invoke-WebRequest -Uri $url -OutFile $target
    }
}

if ($missing.Count -gt 0) {
    # Не помилка: скрипт зробив усе, що міг. Плитка без файла показує підпис,
    # тож драфт від цього не ламається — але сказати про це треба вголос.
    Write-Warning "Джерело не задано для: $($missing -join ', ')"
    Write-Warning "Ці карти лишаться підписами, доки файли не з'являться."
}
