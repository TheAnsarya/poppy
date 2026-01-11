# Makefile for retro game project

# Project name
PROJECT = game

# Directories
SRC_DIR = src
BUILD_DIR = build
ASSETS_DIR = assets

# Source files
SOURCES = $(wildcard $(SRC_DIR)/*.asm)

# Output
OUTPUT = $(BUILD_DIR)/$(PROJECT).rom

.PHONY: all clean

all: $(OUTPUT)

$(OUTPUT): $(SOURCES)
	@mkdir -p $(BUILD_DIR)
	@echo "Building $(PROJECT)..."
	# TODO: Add actual build command here
	@echo "Build complete: $(OUTPUT)"

clean:
	@rm -rf $(BUILD_DIR)
	@echo "Clean complete"
