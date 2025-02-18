﻿SHELL := pwsh.exe
.SHELLFLAGS := -Command

init:
	git submodule init
	git submodule update
	
restore: init
	dotnet restore ./src/

install_velopack:
	dotnet tool update -g vpk

velopack: clean build
	vpk pack -u 'PlayGames-RichPresence' -v '$(VERSION)' -e 'PlayGames RichPresence Standalone.exe' -o 'velopack' --packTitle 'Play Games - Rich Presence' -p 'bin' --shortcuts 'StartMenuRoot'

clean:
	-rm -Recurse -ErrorAction SilentlyContinue bin
	-rm -Recurse -ErrorAction SilentlyContinue velopack

test:
	dotnet test ./src/

build: init
	dotnet publish ./src/PlayGames_RichPresence/ --runtime win-x64 --output ./bin/
	
help:
	@echo "Usage: make <target>"
	@echo ""
	@echo "Targets:"
	@echo "  build                 Build the application"
	@echo "  test                  Run tests on the application"
	@echo "  install_velopack      Installs the toolset for auto-updates"
	@echo "  velopack              Build the application with auto-updates"
	@echo "  init                  Initializes git submodules"
	@echo "  restore               Restores dependencies"
	@echo "  clean                 Cleans build artifact directories"
	@echo "  help                  Show this help message"