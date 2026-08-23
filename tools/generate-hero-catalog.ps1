# Генерує каталоги героїв Dota 2 і чемпіонів League of Legends із публічних джерел.
#
# Запускають руками, коли виходить новий герой; у репозиторій іде ЗГЕНЕРОВАНИЙ
# файл, а не результат запуску на кожній збірці.
#
#   powershell -File tools/generate-hero-catalog.ps1
#
# Навіщо скрипт замість того, щоб набрати список руками: ідентифікатори в CDN
# нерегулярні — Data Dragon зве Wukong «MonkeyKing», а Cho'Gath «Chogath».
# Помилка в такому рядку дає невидиму дірку: портрет просто не завантажиться,
# і ніщо про це не скаже.
#
# Джерела (обидва без ключа):
#   Dota 2 — OpenDota
#   LoL    — Riot Data Dragon

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$root = Split-Path -Parent $PSScriptRoot

# ---- Dota 2 ----
$dota = Invoke-RestMethod 'https://api.opendota.com/api/heroes'
$dotaRows = $dota |
    Sort-Object localized_name |
    ForEach-Object {
        [pscustomobject]@{
            Slug = $_.name -replace '^npc_dota_hero_', ''
            Name = $_.localized_name
        }
    }

# ---- League of Legends ----
$versions = Invoke-RestMethod 'https://ddragon.leagueoflegends.com/api/versions.json'
$version = $versions[0]
$champJson = Invoke-RestMethod "https://ddragon.leagueoflegends.com/cdn/$version/data/en_US/champion.json"
$lolRows = $champJson.data.PSObject.Properties.Value |
    Sort-Object name |
    ForEach-Object { [pscustomobject]@{ Slug = $_.id; Name = $_.name } }

Write-Host "Dota 2: $($dotaRows.Count) героїв; LoL: $($lolRows.Count) чемпіонів (ddragon $version)"

if ($dotaRows.Count -lt 100 -or $lolRows.Count -lt 100) {
    throw "Каталог підозріло малий — не переписуємо наявний файл."
}

function Format-CsArray($rows) {
    ($rows | ForEach-Object { '            "{0}"' -f $_.Slug }) -join ",`n"
}

# Ключ завжди в лапках: слаги — зовнішні дані, і покладатися на те, що вони
# лишаться припустимими ідентифікаторами JS, немає підстав.
function Format-TsMap($rows) {
    ($rows | ForEach-Object { '  "{0}": "{1}"' -f $_.Slug, ($_.Name -replace '"', '\"') }) -join ",`n"
}

$cs = @"
// ЦЕЙ ФАЙЛ ЗГЕНЕРОВАНО. Не правити руками.
// Джерело: tools/generate-hero-catalog.ps1
// Data Dragon: $version

namespace TForge.Common
{
    /// <summary>
    /// Герої Dota 2 і чемпіони League of Legends — те, що банять і пікають у
    /// цих дисциплінах замість карт.
    ///
    /// Значення — це ідентифікатори в CDN, а не назви: вони нерегулярні
    /// (Wukong у Data Dragon звуть MonkeyKing), тож переписати їх руками
    /// означало б тихо зламати частину портретів. Файл генерується.
    ///
    /// Підписи — на клієнті, Esport-Frontend/src/constants/gameHeroes.ts,
    /// той самий поділ, що в Games і Countries.
    /// </summary>
    public static class GameHeroes
    {
        /// <summary>Версія Data Dragon, з якої знято список і з якої беруть портрети.</summary>
        public const string DdragonVersion = "$version";

        public static readonly string[] Dota2 =
        {
$(Format-CsArray $dotaRows)
        };

        public static readonly string[] LeagueOfLegends =
        {
$(Format-CsArray $lolRows)
        };

        private static readonly string[] None = Array.Empty<string>();

        public static string[] ForGame(string? game) => game switch
        {
            Games.Dota2 => Dota2,
            Games.LeagueOfLegends => LeagueOfLegends,
            _ => None
        };

        public static bool HasHeroes(string? game) => ForGame(game).Length > 0;

        public static bool IsValid(string? game, string? hero) =>
            hero != null && ForGame(game).Contains(hero);
    }
}
"@

$csPath = Join-Path $root 'Esport-Backend/Common/GameHeroes.cs'
Set-Content -Path $csPath -Value $cs -Encoding utf8
Write-Host "-> $csPath"

$ts = @"
// ЦЕЙ ФАЙЛ ЗГЕНЕРОВАНО. Не правити руками.
// Джерело: tools/generate-hero-catalog.ps1

/** Версія Data Dragon — мусить збігатися з Common/GameHeroes.cs. */
export const DDRAGON_VERSION = "$version";

const DOTA2_NAMES: Record<string, string> = {
$(Format-TsMap $dotaRows)
};

const LOL_NAMES: Record<string, string> = {
$(Format-TsMap $lolRows)
};

export const HERO_NAMES: Record<string, Record<string, string>> = {
  Dota2: DOTA2_NAMES,
  LeagueOfLegends: LOL_NAMES
};

/** Підпис героя; сам слаг, якщо такого немає — краще, ніж порожня плитка. */
export const heroName = (game: string, slug: string) => HERO_NAMES[game]?.[slug] ?? slug;

/**
 * Портрет із зовнішнього CDN. Плитка, у якої зображення не завантажилось,
 * показує назву — драфт від цього не ламається.
 */
export const heroPortraitUrl = (game: string, slug: string) =>
  game === "LeagueOfLegends"
    ? ``https://ddragon.leagueoflegends.com/cdn/`${DDRAGON_VERSION}/img/champion/`${slug}.png``
    : ``https://cdn.cloudflare.steamstatic.com/apps/dota2/images/dota_react/heroes/`${slug}.png``;
"@

$tsPath = Join-Path $root 'Esport-Frontend/src/constants/gameHeroes.ts'
Set-Content -Path $tsPath -Value $ts -Encoding utf8
Write-Host "-> $tsPath"
