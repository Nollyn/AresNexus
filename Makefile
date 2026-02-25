.PHONY: build test run-api docker-compose-up compliance

SOLUTION=./AresNexus.slnx
API_DIR=./apps/settlement-core/src/AresNexus.Settlement.Api

build:
	dotnet restore $(SOLUTION)
	dotnet build $(SOLUTION) -c Release /warnaserror

dev:
	dotnet run --project $(API_DIR) --launch-profile "http"

test:
	dotnet test $(SOLUTION) -c Release --no-build

api:
	dotnet run --project $(API_DIR)

docker-compose-up:
	docker compose up -d --build

compliance:
	docker build -t aresnexus/compliance-engine:latest ./apps/compliance-engine
	docker run --rm -p 8080:8080 aresnexus/compliance-engine:latest
