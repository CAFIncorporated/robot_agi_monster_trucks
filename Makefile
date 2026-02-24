.PHONY: help bake bake-prod bake-test up down logs docker-test test e2e clean generate-spec

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

# --- Docker Bake ---

bake: ## Build all Docker images via docker buildx bake
	docker buildx bake

bake-prod: ## Build production image only
	docker buildx bake prod

bake-test: ## Build test image only
	docker buildx bake test

# --- Docker Compose (local dev) ---

up: ## Start services (app + psql) via compose
	docker compose -f docker/docker-compose.yaml up --build -d

down: ## Stop all compose services
	docker compose -f docker/docker-compose.yaml down

logs: ## Tail compose logs
	docker compose -f docker/docker-compose.yaml logs -f

# --- Testing ---

docker-test: bake-test ## Build and run unit + client tests in Docker
	docker run --rm coordinate-service:test

test: ## Run dotnet tests locally (requires .NET SDK)
	dotnet restore CoordinateService.sln && dotnet test CoordinateService.sln --no-restore --verbosity normal

e2e: up ## Run end-to-end integration tests via curl against compose
	@echo "Waiting for app to be ready..."
	@for i in 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15; do \
		if curl -sf http://localhost:18080/healthz > /dev/null 2>&1; then break; fi; \
		echo "  attempt $$i..."; sleep 2; \
	done
	@bash scripts/e2e-test.sh

# --- OpenAPI ---

generate-spec: up ## Extract OpenAPI spec from running service
	@echo "Waiting for app to be ready..."
	@for i in 1 2 3 4 5 6 7 8 9 10; do \
		if curl -sf http://localhost:18080/healthz > /dev/null 2>&1; then break; fi; \
		sleep 2; \
	done
	curl -s http://localhost:18080/swagger/v1/swagger.json | python3 -m json.tool > openapi.json
	@echo "Saved openapi.json"

# --- Lint ---

lint: ## Lint Dockerfiles with hadolint
	hadolint docker/prod.Dockerfile || true
	hadolint docker/test.Dockerfile || true

# --- Clean ---

clean: down ## Remove compose volumes and dangling images
	docker compose -f docker/docker-compose.yaml down -v
	docker image prune -f
