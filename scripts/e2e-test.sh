#!/usr/bin/env bash
set -euo pipefail

BASE="http://localhost:18080"
PASS=0
FAIL=0

assert_status() {
    local desc="$1" expected="$2" actual="$3"
    if [ "$expected" = "$actual" ]; then
        echo "  PASS: $desc (HTTP $actual)"
        PASS=$((PASS + 1))
    else
        echo "  FAIL: $desc — expected $expected, got $actual"
        FAIL=$((FAIL + 1))
    fi
}

echo "=== E2E Tests ==="

echo "--- Health ---"
STATUS=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/healthz")
assert_status "GET /healthz" 200 "$STATUS"

STATUS=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/readyz")
assert_status "GET /readyz" 200 "$STATUS"

echo "--- Create Coordinate System ---"
RESP=$(curl -s -w '\n%{http_code}' -X POST "$BASE/api/v1/coordinate-systems" \
    -H "Content-Type: application/json" -H "X-Request-Id: test-001" \
    -d '{"name":"grid","width":10,"height":10}')
BODY=$(echo "$RESP" | head -1)
STATUS=$(echo "$RESP" | tail -1)
assert_status "POST create system" 200 "$STATUS"
SYS_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
echo "  System ID: $SYS_ID"

echo "--- Create System with invalid params ---"
STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/api/v1/coordinate-systems" \
    -H "Content-Type: application/json" -d '{"name":"bad","width":0,"height":10}')
assert_status "POST create system width=0" 400 "$STATUS"

STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/api/v1/coordinate-systems" \
    -H "Content-Type: application/json" -d '{"name":"","width":5,"height":5}')
assert_status "POST create system empty name" 400 "$STATUS"

echo "--- Get Coordinate System ---"
STATUS=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/v1/coordinate-systems/$SYS_ID")
assert_status "GET system" 200 "$STATUS"

STATUS=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/v1/coordinate-systems/00000000-0000-0000-0000-000000000000")
assert_status "GET nonexistent system" 404 "$STATUS"

echo "--- Create Point ---"
RESP=$(curl -s -w '\n%{http_code}' -X POST "$BASE/api/v1/coordinate-systems/$SYS_ID/points" \
    -H "Content-Type: application/json" -d '{"x":5,"y":5,"direction":"N"}')
BODY=$(echo "$RESP" | head -1)
STATUS=$(echo "$RESP" | tail -1)
assert_status "POST create point" 200 "$STATUS"
PT_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
echo "  Point ID: $PT_ID"

echo "--- Create Point errors ---"
STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/api/v1/coordinate-systems/$SYS_ID/points" \
    -H "Content-Type: application/json" -d '{"x":0,"y":0,"direction":"N"}')
assert_status "POST duplicate point in system" 400 "$STATUS"

STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/api/v1/coordinate-systems/$SYS_ID/points" \
    -H "Content-Type: application/json" -d '{"x":99,"y":0,"direction":"N"}')
assert_status "POST point out of bounds" 400 "$STATUS"

echo "--- Get Point ---"
STATUS=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/v1/points/$PT_ID")
assert_status "GET point" 200 "$STATUS"

echo "--- Move Point ---"
RESP=$(curl -s -w '\n%{http_code}' -X POST "$BASE/api/v1/points/$PT_ID/move" \
    -H "Content-Type: application/json" -d '{"commands":["R","M","M","R","R","M"]}')
BODY=$(echo "$RESP" | head -1)
STATUS=$(echo "$RESP" | tail -1)
assert_status "POST move point (M/R/L commands)" 200 "$STATUS"
echo "  Result: $BODY"

STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/api/v1/points/$PT_ID/move" \
    -H "Content-Type: application/json" -d '{"commands":["M","M","M","M","M","M","M"]}')
assert_status "POST move out of bounds" 400 "$STATUS"

STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/api/v1/points/$PT_ID/move" \
    -H "Content-Type: application/json" -d '{"commands":[]}')
assert_status "POST move empty commands" 400 "$STATUS"

echo "--- Delete Point ---"
STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X DELETE "$BASE/api/v1/points/$PT_ID")
assert_status "DELETE point" 204 "$STATUS"

STATUS=$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/v1/points/$PT_ID")
assert_status "GET deleted point" 404 "$STATUS"

echo "--- Delete System ---"
STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X DELETE "$BASE/api/v1/coordinate-systems/$SYS_ID")
assert_status "DELETE system" 204 "$STATUS"

echo "--- X-Request-Id ---"
REQ_ID=$(curl -s -D - -o /dev/null -X POST "$BASE/api/v1/coordinate-systems" \
    -H "Content-Type: application/json" -H "X-Request-Id: my-custom-id" \
    -d '{"name":"reqid","width":5,"height":5}' | grep -i 'x-request-id' | tr -d '\r' | awk '{print $2}')
if [ "$REQ_ID" = "my-custom-id" ]; then
    echo "  PASS: X-Request-Id echoed back"
    PASS=$((PASS + 1))
else
    echo "  FAIL: X-Request-Id not echoed (got: $REQ_ID)"
    FAIL=$((FAIL + 1))
fi

echo ""
echo "=== Results: $PASS passed, $FAIL failed ==="
[ "$FAIL" -eq 0 ] || exit 1
