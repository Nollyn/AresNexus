.PHONY: up down test demo clean

# Task 3: One-Command Setup (Makefile)
# Pulls/Builds everything and starts the stack
up:
	docker compose up --build -d

# Stops and removes all containers
down:
	docker compose down

# Runs all Unit and Integration tests
test:
	dotnet test AresNexus.slnx

# Task 3: Sends a burst of 50 sample ISO 20022 transactions to the Gateway
# Note: Using settlement-core port (5001) as it's the backend for transactions
demo:
	powershell -Command "for ($i=1; $i -le 50; $i++) { \
		Invoke-RestMethod -Uri 'http://localhost:5001/api/transactions/process' \
		-Method Post \
		-ContentType 'application/json' \
		-Body (@{ \
			accountId = 'd0000000-0000-0000-0000-000000000001'; \
			amount = (Get-Random -Minimum 100 -Maximum 5000); \
			currency = 'CHF'; \
			reference = 'ISO20022-DEMO-PAYMENT-' + $i; \
			idempotencyKey = [Guid]::NewGuid().ToString() \
		} | ConvertTo-Json); \
		Write-Host 'Processed transaction ' $i; \
	}"

clean:
	docker compose down -v
	dotnet clean
