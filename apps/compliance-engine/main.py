from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI(title="Ares-Nexus Compliance Engine", version="1.0.0")

class Transaction(BaseModel):
    accountId: str
    amount: float
    type: str

@app.post("/validate")
def validate(tx: Transaction):
    return {"status": "APPROVED", "riskScore": 0.01}
