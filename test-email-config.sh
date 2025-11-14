#!/bin/bash

# ==============================================
# 📧 Email Configuration Test Script
# ==============================================

echo "🧪 Testing DSecure Email Configuration..."
echo "=========================================="
echo ""

# Configuration
API_URL="https://localhost:44316"
TEST_EMAIL="nishus877@gmail.com"

echo "📍 API URL: $API_URL"
echo "📧 Test Email: $TEST_EMAIL"
echo ""

# Test 1: Check if API is running
echo "🔍 Test 1: Checking if API is running..."
if curl -k -s "$API_URL/swagger" > /dev/null 2>&1; then
    echo "✅ API is running"
else
    echo "❌ API is not running. Start with: dotnet run"
    exit 1
fi
echo ""

# Test 2: Check email configuration
echo "🔍 Test 2: Checking email configuration..."
CONFIG_RESPONSE=$(curl -k -s -X GET "$API_URL/api/ForgotPassword/email-config-check")
echo "Response:"
echo "$CONFIG_RESPONSE" | jq '.'
echo ""

# Check if password is set
if echo "$CONFIG_RESPONSE" | grep -q "\"NOT SET\""; then
    echo "❌ Email password not configured!"
 echo "💡 Fix: Update .env file with EmailSettings__FromPassword"
    exit 1
else
    echo "✅ Email configuration found"
fi
echo ""

# Test 3: Send test email
echo "🔍 Test 3: Sending test email to $TEST_EMAIL..."
TEST_RESPONSE=$(curl -k -s -X POST "$API_URL/api/ForgotPassword/test-email" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$TEST_EMAIL\"}")

echo "Response:"
echo "$TEST_RESPONSE" | jq '.'
echo ""

# Check if email was sent successfully
if echo "$TEST_RESPONSE" | grep -q "\"success\":true"; then
    echo "✅ Test email sent successfully!"
    echo "📬 Check inbox: $TEST_EMAIL"
    OTP=$(echo "$TEST_RESPONSE" | jq -r '.testOtp')
    echo "🔑 Test OTP: $OTP"
else
    echo "❌ Failed to send test email"
    echo "💡 Check troubleshooting guide: Documentation/EMAIL-TROUBLESHOOTING.md"
    exit 1
fi
echo ""

# Test 4: Test actual forgot password flow
echo "🔍 Test 4: Testing forgot password flow..."
FORGOT_RESPONSE=$(curl -k -s -X POST "$API_URL/api/ForgotPassword/request-otp" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$TEST_EMAIL\"}")

echo "Response:"
echo "$FORGOT_RESPONSE" | jq '.'
echo ""

if echo "$FORGOT_RESPONSE" | grep -q "\"success\":true"; then
    echo "✅ Forgot password flow working!"
else
    echo "⚠️ Forgot password flow might have issues"
fi
echo ""

# Summary
echo "=========================================="
echo "📊 Test Summary"
echo "=========================================="
echo "✅ API Running"
echo "✅ Email Configuration Loaded"
if echo "$TEST_RESPONSE" | grep -q "\"success\":true"; then
    echo "✅ Test Email Sent"
    echo "✅ System Ready!"
else
    echo "❌ Test Email Failed"
    echo "📖 See: Documentation/EMAIL-TROUBLESHOOTING.md"
fi
echo ""
echo "🎊 Testing Complete!"
