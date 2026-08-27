param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Admin', 'SingleClub')]
    [string]$KeySet,
    [Parameter(Mandatory = $true)]
    [string]$OutFile
)

$secretName = if ($KeySet -eq 'Admin') { 'JustGo:ApiKeyAdmin' } else { 'JustGo:ApiKeySingleClub' }
$secretLine = dotnet user-secrets list --project .\JustGo\JustGo.AppHost.csproj | Select-String -Pattern "^$([regex]::Escape($secretName)) = " | Select-Object -First 1

if (-not $secretLine) {
    throw "Missing secret '$secretName'."
}

$secretValue = $secretLine.ToString().Split(' = ', 2)[1]
dotnet user-secrets set "JustGo:ApiKey" $secretValue --project .\JustGo\JustGo.AppHost.csproj | Out-Null

Start-Sleep -Seconds 2

function Invoke-Json([string]$Uri) {
    try {
        return Invoke-RestMethod $Uri -ErrorAction Stop
    } catch {
        return [ordered]@{ error = $_.Exception.Message }
    }
}

$clubs = Invoke-Json 'http://localhost:61704/clubs/search?PageNumber=1&PageSize=5'
$clubIds = @()
if ($clubs -isnot [hashtable] -and $clubs -isnot [System.Collections.IDictionary]) {
    if ($clubs.items) { $clubIds = @($clubs.items | Select-Object -ExpandProperty id -First 2) }
    elseif ($clubs.data) { $clubIds = @($clubs.data | Select-Object -ExpandProperty id -First 2) }
    elseif ($clubs.results) { $clubIds = @($clubs.results | Select-Object -ExpandProperty id -First 2) }
}

$samples = @()
foreach ($clubId in $clubIds) {
    $samples += [ordered]@{
        clubId = $clubId
        clubDetail = Invoke-Json "http://localhost:61704/clubs/$clubId"
        members = Invoke-Json "http://localhost:61704/members/search?PageNumber=1&PageSize=10&ClubId=$clubId"
    }
}

[ordered]@{
    keySet = $KeySet
    clubs = $clubs
    samples = $samples
} | ConvertTo-Json -Depth 20 | Set-Content -Path $OutFile
