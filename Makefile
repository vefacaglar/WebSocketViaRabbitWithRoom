.PHONY: local-lb local-lb-stop

local-lb:
	./scripts/start-local-lb.sh

local-lb-stop:
	./scripts/stop-local-lb.sh
