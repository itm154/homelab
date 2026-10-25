set default-list := true

down COMPOSE_FILE:
	docker compose --env-file ./.env -f {{COMPOSE_FILE}} down


up COMPOSE_FILE:
	docker compose --env-file ./.env -f {{COMPOSE_FILE}} up -d
