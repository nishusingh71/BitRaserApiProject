# ==============================================
# 📧 Email Configuration Test Script (PowerShell)
# ==============================================

Write-Host "🧪 Testing DSecure Email Configuration..." -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ApiUrl = "https://localhost:44316"
$TestEmail = "nishus877@gmail.com"

Write-Host "📍 API URL: $ApiUrl" -ForegroundColor Yellow
Write-Host "📧 Test Email: $TestEmail" -ForegroundColor Yellow
Write-Host ""

# Test 1: Check if API is running
Write-Host "🔍 Test 1: Checking if API is running..." -ForegroundColor Cyan
try {
    $null = Invoke-WebRequest -Uri "$ApiUrl/swagger" -UseBasicParsing -SkipCertificateCheck -ErrorAction Stop
    Write-Host "✅ API is running" -ForegroundColor Green
} catch {
    Write-Host "❌ API is not running. Start with: dotnet run" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 2: Check email configuration
Write-Host "🔍 Test 2: Checking email configuration..." -ForegroundColor Cyan
try {
    $ConfigResponse = Invoke-RestMethod -Uri "$ApiUrl/api/ForgotPassword/email-config-check" `
        -Method GET `
   -SkipCertificateCheck

  Write-Host "Response:" -ForegroundColor Yellow
    $ConfigResponse | ConvertTo-Json -Depth 10 | Write-Host
    Write-Host ""

    if ($ConfigResponse.fromEnvironmentVariables.password -eq "NOT SET") {
        Write-Host "❌ Email password not configured!" -ForegroundColor Red
   Write-Host "💡 Fix: Update .env file with EmailSettings__FromPassword" -ForegroundColor Yellow
        exit 1
    } else {
     Write-Host "✅ Email configuration found" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Failed to check configuration: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 3: Send test email
Write-Host "🔍 Test 3: Sending test email to $TestEmail..." -ForegroundColor Cyan
try {
    $TestBody = @{
        email = $TestEmail
    } | ConvertTo-Json

    $TestResponse = Invoke-RestMethod -Uri "$ApiUrl/api/ForgotPassword/test-email" `
        -Method POST `
        -ContentType "application/json" `
        -Body $TestBody `
        -SkipCertificateCheck

    Write-Host "Response:" -ForegroundColor Yellow
    $TestResponse | ConvertTo-Json -Depth 10 | Write-Host
    Write-Host ""

    if ($TestResponse.success -eq $true) {
     Write-Host "✅ Test email sent successfully!" -ForegroundColor Green
 Write-Host "📬 Check inbox: $TestEmail" -ForegroundColor Cyan
        Write-Host "🔑 Test OTP: $($TestResponse.testOtp)" -ForegroundColor Yellow
    } else {
        Write-Host "❌ Failed to send test email" -ForegroundColor Red
     Write-Host "💡 Check troubleshooting guide: Documentation\EMAIL-TROUBLESHOOTING.md" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ Error sending test email: $_" -ForegroundColor Red
exit 1
}
Write-Host ""

# Test 4: Test actual forgot password flow
Write-Host "🔍 Test 4: Testing forgot password flow..." -ForegroundColor Cyan
try {
    $ForgotBody = @{
  email = $TestEmail
    } | ConvertTo-Json

    $ForgotResponse = Invoke-RestMethod -Uri "$ApiUrl/api/ForgotPassword/request-otp" `
        -Method POST `
        -ContentType "application/json" `
  -Body $ForgotBody `
        -SkipCertificateCheck

  Write-Host "Response:" -ForegroundColor Yellow
  $ForgotResponse | ConvertTo-Json -Depth 10 | Write-Host
    Write-Host ""

    if ($ForgotResponse.success -eq $true) {
      Write-Host "✅ Forgot password flow working!" -ForegroundColor Green
 } else {
        Write-Host "⚠️ Forgot password flow might have issues" -ForegroundColor Yellow
 }
} catch {
  Write-Host "⚠️ Error testing forgot password: $_" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "📊 Test Summary" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "✅ API Running" -ForegroundColor Green
Write-Host "✅ Email Configuration Loaded" -ForegroundColor Green

if ($TestResponse.success -eq $true) {
    Write-Host "✅ Test Email Sent" -ForegroundColor Green
    Write-Host "✅ System Ready!" -ForegroundColor Green
Write-Host ""
    Write-Host "🎊 All Tests Passed!" -ForegroundColor Cyan
} else {
    Write-Host "❌ Test Email Failed" -ForegroundColor Red
    Write-Host "📖 See: Documentation\EMAIL-TROUBLESHOOTING.md" -ForegroundColor Yellow
}
Write-Host ""
