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

# Executes the high-impact ISO 20022 demo script
demo:
	bash ./scripts/run-demo.sh

clean:
	docker compose down -v
	dotnet clean
