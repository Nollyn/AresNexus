#!/bin/bash

# AresNexus Settlement Demo Script
# Simulation of ISO 20022 traffic (PACS.008) to the settlement-core API.

API_URL="http://localhost:5000/api/v1/transactions"
# Fallback for Gateway API if needed (port 5001)
# API_URL="http://localhost:5001/api/v1/transactions"
DATA_DIR="./scripts/demo-data"
FILES=($DATA_DIR/*.json)
NUM_REQUESTS=$((50 + RANDOM % 51)) # 50-100 requests

echo "Starting high-impact demo: Sending $NUM_REQUESTS requests to $API_URL..."

for ((i=1; i<=NUM_REQUESTS; i++)); do
    # Select a random payload file
    FILE=${FILES[$RANDOM % ${#FILES[@]}]}
    
    # Generate unique Idempotency-Key and Trace/Correlation IDs for each request
    IDEMPOTENCY_KEY=$(cat /proc/sys/kernel/random/uuid 2>/dev/null || python3 -c 'import uuid; print(uuid.uuid4())' 2>/dev/null || echo "demo-key-$i-$RANDOM")
    TRACE_ID="trace-$i-$RANDOM"
    CORR_ID="corr-$i-$RANDOM"

    # Simulate 5% error rate by sending an invalid amount (-100)
    if (( RANDOM % 100 < 5 )); then
        echo "Simulating error for request $i..."
        PAYLOAD=$(jq --arg id "$IDEMPOTENCY_KEY" --arg trace "$TRACE_ID" --arg corr "$CORR_ID" \
                  '.IdempotencyKey = $id | .TraceId = $trace | .CorrelationId = $corr | .Money.Amount = -100' "$FILE")
    else
        PAYLOAD=$(jq --arg id "$IDEMPOTENCY_KEY" --arg trace "$TRACE_ID" --arg corr "$CORR_ID" \
                  '.IdempotencyKey = $id | .TraceId = $trace | .CorrelationId = $corr' "$FILE")
    fi

    # Send request
    curl -s -X POST "$API_URL" \
         -H "Content-Type: application/json" \
         -H "Idempotency-Key: $IDEMPOTENCY_KEY" \
         -H "X-Trace-Id: $TRACE_ID" \
         -H "X-Correlation-Id: $CORR_ID" \
         -d "$PAYLOAD" > /dev/null &

    # Variability: sleep 0.5s between requests to create realistic TPS spikes/valleys
    sleep 0.5
    
    if (( i % 10 == 0 )); then
        echo "Sent $i requests..."
    fi
done

echo "Demo completed. Sent $NUM_REQUESTS requests."