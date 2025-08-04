﻿SHELL := pwsh.exe
.SHELLFLAGS := -Command

init:
	git submodule init
	git submodule update
	
restore: init
	dotnet restore ./src/

install_velopack:
	dotnet tool update -g vpk

velopack: install_velopack clean build
	vpk pack -u 'PlayGames-RichPresence' -v '$(VERSION)' -e 'PlayGames RichPresence Standalone.exe' -o 'velopack' --packTitle 'Play Games - Rich Presence' -p 'bin' --shortcuts 'StartMenuRoot' --framework net9-x64-desktop

clean:
	-rm -Recurse -ErrorAction SilentlyContinue bin
	-rm -Recurse -ErrorAction SilentlyContinue velopack

test:
	dotnet test ./src/

build: init
	dotnet publish ./src/PlayGames_RichPresence/ --runtime win-x64 --output ./bin/
	
help:
	$(info Usage: make <target>)
	$(info )
	$(info Targets: )
	$(info   build                 Build the application )
	$(info   test                  Run tests on the application )
	$(info   install_velopack      Installs the toolset for auto-updates )
	$(info   velopack              Build the application with auto-updates )
	$(info   init                  Initializes git submodules )
	$(info   restore               Restores dependencies )
	$(info   clean                 Cleans build artifact directories )
	$(info   help                  Show this help message )