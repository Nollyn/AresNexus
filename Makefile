.PHONY: up down test demo clean check-health

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

# Executes the high-impact ISO 20022 demo script
demo:
	bash ./scripts/run-demo.sh

# Health check verification: pings the /metrics endpoint to confirm it's emitting data
check-health:
	@echo "Checking Settlement Core metrics..."
	@curl.exe -s http://localhost:5001/metrics | findstr /R "settlement_total_count settlement_processing_seconds" || (echo "Metrics not found!" && exit 1)
	@echo "Metrics are being emitted successfully."

clean:
	docker compose down -v
	dotnet clean
