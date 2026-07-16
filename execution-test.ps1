# =====================================================================
#  GMS Execution Domain — Uçtan Uca Deneme Senaryosu
#  Çalıştırma:  powershell -ExecutionPolicy Bypass -File .\execution-test.ps1
#  Ön koşul  :  API çalışıyor olmalı (http://localhost:18080)
# =====================================================================

$base = "http://localhost:18080/api"

# Seed (hazır) kayıt kimlikleri
$cust = "d4444444-4444-4444-4444-444444444401"
$proj = "e5555555-5555-5555-5555-555555555501"
$env  = "f6666666-6666-6666-6666-666666666604"
$usr  = "b2222222-2222-2222-2222-222222222201"

# UTF-8 gövde ile POST yardımcıları
function Post($url, $obj) {
  $b = [Text.Encoding]::UTF8.GetBytes(($obj | ConvertTo-Json -Depth 6))
  Invoke-RestMethod $url -Method Post -Body $b -ContentType "application/json; charset=utf-8"
}
function Code($e) { [int]$e.Exception.Response.StatusCode }

# Onaylı (Approved) bir değişiklik üretir: create -> submit -> tüm adımları approve
function New-ApprovedChange($title) {
  $c = Post "$base/change-requests" @{
    title=$title; businessReason="deneme"; customerId=$cust; projectId=$proj; environmentId=$env
    changeClass="Normal"; changeType="ConfigurationChange"; priority="Medium"; createdByUserId=$usr
    revision=@{ technicalSummary="ozet"; estimatedDurationMinutes=20; rollbackScript="ROLLBACK" }
    assets=@(); documents=@()
  }
  Post "$base/change-requests/$($c.id)/submit?actorUserId=$usr" @{} | Out-Null
  $a = Invoke-RestMethod "$base/approvals/by-change/$($c.id)"
  for ($i=0; $i -lt $a.steps.Count; $i++) {
    $cur = Invoke-RestMethod "$base/approvals/by-change/$($c.id)"
    $act = $cur.steps | Where-Object status -eq 'Active' | Select-Object -First 1
    Post "$base/approvals/$($cur.id)/approve" @{ userId=$act.approverUserId; comment="ok"; signatureMeaning="Onay" } | Out-Null
  }
  return $c.id
}

# Onaylı değişikliklerden zamanlanmış (Scheduled) release üretir
function New-ScheduledRelease($name, $changeIds) {
  $rel = Post "$base/releases" @{
    name=$name; version="v1"; customerId=$cust; projectId=$proj; environmentId=$env
    releaseType="Minor"; releaseManagerUserId=$usr; changeIds=$changeIds; actorUserId=$usr
    deploymentPlan=@{ deploymentStrategy="rolling" }; documents=@()
  }
  Post "$base/releases/$($rel.id)/schedule" @{ actorUserId=$usr } | Out-Null
  return $rel
}

Write-Host "`n=========== SENARYO 1: BASARI AKISI ===========" -ForegroundColor Cyan
$c1 = New-ApprovedChange "Deneme Basari A"
$c2 = New-ApprovedChange "Deneme Basari B"
$rel = New-ScheduledRelease "Deneme Release" @($c1, $c2)
Write-Host "Release olusturuldu: $($rel.releaseNo)  (Scheduled, 2 degisiklik)"

$run = Post "$base/deployments" @{ releasePlanId=$rel.id; actorUserId=$usr; notes="deneme yurutme" }
Write-Host "Yurutme olusturuldu: $($run.executionNo)  durum=$($run.status)  adim=$($run.steps.Count)"

$r = Post "$base/deployments/$($run.id)/start" @{ actorUserId=$usr }
$relS = (Invoke-RestMethod "$base/releases/$($rel.id)").status
Write-Host "Baslatildi -> run=$($r.status)   release=$relS"

for ($i=1; $i -le 2; $i++) {
  Post "$base/deployments/$($run.id)/start-next-step" @{ actorUserId=$usr } | Out-Null
  $r = Post "$base/deployments/$($run.id)/complete-step" @{ actorUserId=$usr; notes="adim $i tamam" }
  Write-Host "Adim $i baslatildi+tamamlandi -> run=$($r.status)"
}

$f = Invoke-RestMethod "$base/deployments/$($run.id)"
$relF = (Invoke-RestMethod "$base/releases/$($rel.id)").status
$s1 = (Invoke-RestMethod "$base/change-requests/$c1").status
$s2 = (Invoke-RestMethod "$base/change-requests/$c2").status
Write-Host "SONUC -> run=$($f.status)/$($f.overallResult)  release=$relF  degisiklikler=$s1,$s2" -ForegroundColor Green

Write-Host "`n=========== SENARYO 2: BASARISIZLIK + ROLLBACK ===========" -ForegroundColor Cyan
$c3 = New-ApprovedChange "Deneme Hata A"
$c4 = New-ApprovedChange "Deneme Hata B"
$rel2 = New-ScheduledRelease "Deneme Hata Release" @($c3, $c4)
$run2 = Post "$base/deployments" @{ releasePlanId=$rel2.id; actorUserId=$usr }
Post "$base/deployments/$($run2.id)/start" @{ actorUserId=$usr } | Out-Null
Post "$base/deployments/$($run2.id)/start-next-step" @{ actorUserId=$usr } | Out-Null

$r = Post "$base/deployments/$($run2.id)/fail-step" @{ actorUserId=$usr; notes="deploy hatasi" }
$relFail = (Invoke-RestMethod "$base/releases/$($rel2.id)").status
Write-Host "Adim basarisiz -> run=$($r.status)   release=$relFail (InProgress kalir)"

$rb = Post "$base/deployments/$($run2.id)/rollback" @{ actorUserId=$usr; notes="geri alindi" }
$relRb = (Invoke-RestMethod "$base/releases/$($rel2.id)").status
$s3 = (Invoke-RestMethod "$base/change-requests/$c3").status
$s4 = (Invoke-RestMethod "$base/change-requests/$c4").status
Write-Host "Rollback -> run=$($rb.status)/$($rb.overallResult)  release=$relRb  degisiklikler=$s3,$s4" -ForegroundColor Yellow
Write-Host ("Adimlar -> " + (($rb.steps | ForEach-Object { $_.stepOrder.ToString() + ':' + $_.status }) -join ', '))

Write-Host "`n=========== SENARYO 3: KURAL KONTROLLERI (400 beklenir) ===========" -ForegroundColor Cyan
# Scheduled olmayan release icin yurutme -> 400
try { Post "$base/deployments" @{ releasePlanId=$rel.id; actorUserId=$usr } | Out-Null; Write-Host "Tamamlanmis release icin yurutme -> 200 (BEKLENMEDIK)" }
catch { Write-Host "Tamamlanmis release icin yurutme -> HTTP $(Code $_)  (dogru)" }

Write-Host "`nBitti. Detaylari gormek icin:  $base/deployments  veya  http://localhost:18080/swagger`n" -ForegroundColor Cyan
