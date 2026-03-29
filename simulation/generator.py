import json
import random
import os
import uuid
import math
from datetime import datetime, timedelta

# Configuración determinista
RANDOM_SEED = 42
random.seed(RANDOM_SEED)

OUTPUT_DIR = "simulation/data"
TOTAL_PORTFOLIO_VALUE = 5_500_000_000  # $5.5B USD
NUM_ACCOUNTS = 50_000
NUM_POSITIONS = 200_000
NUM_TRANSACTIONS = 1_000_000 # Escala reducida para agilidad, pero escalable

# Distribución de activos según requerimientos
ASSET_DISTRIBUTION = {
    "Equities": 0.40,
    "Bonds": 0.30,
    "Derivatives": 0.10,
    "FX": 0.10,
    "Cash": 0.10
}

def generate_accounts():
    accounts = []
    for i in range(NUM_ACCOUNTS):
        accounts.append({
            "AccountId": f"ACC-{100000 + i}",
            "Owner": f"Institutional_Client_{i}",
            "Type": random.choice(["Institutional", "Retail_HighNetWorth", "Corporate"]),
            "Currency": "USD",
            "CreatedAt": (datetime.now() - timedelta(days=random.randint(365, 3650))).isoformat()
        })
    return accounts

def generate_positions(accounts):
    positions = []
    avg_value_per_position = TOTAL_PORTFOLIO_VALUE / NUM_POSITIONS
    
    # Asignar posiciones a cuentas aleatorias
    for i in range(NUM_POSITIONS):
        asset_class = random.choices(list(ASSET_DISTRIBUTION.keys()), weights=list(ASSET_DISTRIBUTION.values()))[0]
        account = random.choice(accounts)
        
        # Valor de la posición con cierta varianza
        value = avg_value_per_position * random.uniform(0.1, 2.0)
        
        positions.append({
            "PositionId": f"POS-{200000 + i}",
            "AccountId": account["AccountId"],
            "AssetClass": asset_class,
            "Symbol": f"{asset_class[:3].upper()}-{random.randint(100, 999)}",
            "Quantity": random.uniform(10, 10000),
            "MarketValue": round(value, 2),
            "Currency": "USD"
        })
    return positions

def generate_transactions(accounts, positions):
    transactions = []
    start_date = datetime.now() - timedelta(days=30)
    
    for i in range(NUM_TRANSACTIONS):
        pos = random.choice(positions)
        acc = next(a for a in accounts if a["AccountId"] == pos["AccountId"])
        
        # Determinación del tipo de transacción
        tx_type = random.choice(["BUY", "SELL", "DIVIDEND", "INTEREST", "FX_SWAP"])
        amount = pos["MarketValue"] * random.uniform(0.01, 0.05) # 1-5% del valor de la posición
        
        tx_id = str(uuid.uuid4())
        timestamp = start_date + timedelta(seconds=random.randint(0, 30*24*3600))
        
        transactions.append({
            "TransactionId": tx_id,
            "AccountId": acc["AccountId"],
            "PositionId": pos["PositionId"],
            "Symbol": pos["Symbol"],
            "Type": tx_type,
            "Amount": round(amount, 2),
            "Currency": "USD",
            "Timestamp": timestamp.isoformat(),
            "Status": "PENDING"
        })
    return transactions

def save_to_json(data, filename):
    path = os.path.join(OUTPUT_DIR, filename)
    with open(path, 'w') as f:
        json.dump(data, f, indent=2)
    print(f"Saved {len(data)} records to {path}")

def main():
    print(f"Generating synthetic portfolio of ${TOTAL_PORTFOLIO_VALUE/1e9}B USD...")
    
    accounts = generate_accounts()
    save_to_json(accounts, "accounts.json")
    
    positions = generate_positions(accounts)
    save_to_json(positions, "positions.json")
    
    print(f"Generating {NUM_TRANSACTIONS} transactions...")
    # Para ahorrar memoria en la generación masiva, podríamos escribir línea por línea
    # Pero para este volumen en un entorno de desarrollo, el JSON directo suele estar bien si hay RAM.
    # Optaremos por un enfoque de chunks si fuera necesario, pero por ahora directo.
    
    # Reducimos a 100k para el demo por defecto para evitar archivos gigantes en git/repo
    transactions = generate_transactions(accounts, positions[:50000]) 
    save_to_json(transactions, "transactions.json")

if __name__ == "__main__":
    if not os.path.exists(OUTPUT_DIR):
        os.makedirs(OUTPUT_DIR)
    main()
