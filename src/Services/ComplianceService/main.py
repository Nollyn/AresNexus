from fastapi import FastAPI, Response
from pydantic import BaseModel

app = FastAPI(title="Ares-Nexus Compliance Engine", version="1.0.0")

# Internal counter simulation
# In a real app with multiple workers, we would need a shared counter or use a proper library.
# Since uvicorn runs here, this global variable will work for single-process monitoring.
COMPLIANCE_ERRORS_TOTAL = 0

class Transaction(BaseModel):
    accountId: str
    amount: float
    type: str

@app.post("/validate")
def validate(tx: Transaction):
    global COMPLIANCE_ERRORS_TOTAL
    # Simulate validation logic
    if tx.amount > 1000000:
        COMPLIANCE_ERRORS_TOTAL += 1
        return {"status": "REJECTED", "reason": "Tier-1 transaction requires manual audit"}
    return {"status": "APPROVED", "riskScore": 0.01}

@app.get("/metrics")
def metrics():
    # Manual OpenMetrics/Prometheus implementation
    metrics_data = [
        "# HELP compliance_validation_errors_total Total number of compliance validation errors",
        "# TYPE compliance_validation_errors_total counter",
        f"compliance_validation_errors_total {COMPLIANCE_ERRORS_TOTAL}"
    ]
    return Response(
        content="\n".join(metrics_data) + "\n",
        media_type="text/plain; version=0.0.4; charset=utf-8"
    )

@app.get("/health")
def health():
    return {"status": "UP"}
