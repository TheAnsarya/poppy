// ============================================================================
// Program.cs - Poppy Compiler CLI Entry Point
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Project;
using Poppy.Core.Semantics;

namespace Poppy.CLI;

/// <summary>
/// Main entry point for the Poppy compiler CLI.
/// </summary>
internal static class Program {
	private static readonly string Version = "0.1.0";
	private static readonly string AppName = "Poppy Compiler";

	/// <summary>
	/// Main entry point.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns>Exit code (0 for success).</returns>
	public static int Main(string[] args) {
		// Parse command line arguments
		var options = ParseArguments(args);

		if (options.ShowHelp) {
			ShowHelp();
			return 0;
		}

		if (options.ShowVersion) {
			ShowVersion();
			return 0;
		}

		// Handle clean command
		if (options.Command == CommandType.Clean) {
			return CleanProject(options);
		}

		// Handle init command
		if (options.Command == CommandType.Init) {
			return InitProject(options);
		}

		// Handle pack command
		if (options.Command == CommandType.Pack) {
			return PackProject(options);
		}

		// Handle unpack command
		if (options.Command == CommandType.Unpack) {
			return UnpackArchive(options);
		}

		// Handle validate command
		if (options.Command == CommandType.Validate) {
			return ValidateArchive(options);
		}

		// Project-based build
		if (options.ProjectPath is not null) {
			return BuildProject(options);
		}

		if (options.InputFile is null) {
			Console.Error.WriteLine("Error: No input file specified.");
			Console.Error.WriteLine("Use --help for usage information.");
			return 1;
		}

		// Watch mode or single compilation
		if (options.Watch) {
			return WatchAndCompile(options);
		}

		// Compile the file
		return Compile(options);
	}

	/// <summary>
	/// Watches source files and recompiles on changes.
	/// </summary>
	private static int WatchAndCompile(CompilerOptions options) {
		var inputFile = Path.GetFullPath(options.InputFile!);
		var directory = Path.GetDirectoryName(inputFile) ?? ".";
		var fileName = Path.GetFileName(inputFile);

		// Check input file exists
		if (!File.Exists(inputFile)) {
			Console.Error.WriteLine($"Error: Input file not found: {inputFile}");
			return 1;
		}

		Console.WriteLine($"{AppName} v{Version} - Watch Mode");
		Console.WriteLine($"Watching: {inputFile}");
		Console.WriteLine("Press Ctrl+C to stop.");
		Console.WriteLine();

		// Initial compilation
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var result = Compile(options);
		stopwatch.Stop();
		Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Compilation {(result == 0 ? "succeeded" : "failed")} ({stopwatch.ElapsedMilliseconds}ms)");
		Console.WriteLine();

		// Set up file watcher
		using var watcher = new FileSystemWatcher(directory) {
			Filter = "*.pasm",
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
			IncludeSubdirectories = true,
			EnableRaisingEvents = true
		};

		// Also watch .inc files
		using var incWatcher = new FileSystemWatcher(directory) {
			Filter = "*.inc",
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
			IncludeSubdirectories = true,
			EnableRaisingEvents = true
		};

		// Debounce timer
		Timer? debounceTimer = null;
		var lockObj = new object();

		void OnFileChanged(object sender, FileSystemEventArgs e) {
			lock (lockObj) {
				debounceTimer?.Dispose();
				debounceTimer = new Timer(_ => {
					lock (lockObj) {
						Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] File changed: {e.Name}");
						stopwatch.Restart();
						result = Compile(options);
						stopwatch.Stop();
						Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Compilation {(result == 0 ? "succeeded" : "failed")} ({stopwatch.ElapsedMilliseconds}ms)");
						Console.WriteLine();
					}
				}, null, 200, Timeout.Infinite); // 200ms debounce
			}
		}

		watcher.Changed += OnFileChanged;
		watcher.Created += OnFileChanged;
		incWatcher.Changed += OnFileChanged;
		incWatcher.Created += OnFileChanged;

		// Wait for Ctrl+C
		var exitEvent = new ManualResetEvent(false);
		Console.CancelKeyPress += (_, e) => {
			e.Cancel = true;
			exitEvent.Set();
		};

		exitEvent.WaitOne();
		Console.WriteLine();
		Console.WriteLine("Watch mode stopped.");

		return 0;
	}

	/// <summary>
	/// Builds a project from a poppy.json file.
	/// </summary>
	private static int BuildProject(CompilerOptions options) {
		// Find project file
		var projectPath = options.ProjectPath!;
		string projectFile;

		if (Directory.Exists(projectPath)) {
			// Directory provided - look for poppy.json
			projectFile = Path.Combine(projectPath, "poppy.json");
		} else if (File.Exists(projectPath)) {
			// File provided directly
			projectFile = projectPath;
		} else {
			Console.Error.WriteLine($"Error: Project not found: {projectPath}");
			return 1;
		}

		if (!File.Exists(projectFile)) {
			Console.Error.WriteLine($"Error: Project file not found: {projectFile}");
			return 1;
		}

		// Load project
		ProjectFile project;
		try {
			project = ProjectFile.Load(projectFile);
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error loading project file: {ex.Message}");
			return 1;
		}

		// Validate project
		var errors = project.Validate();
		if (errors.Count > 0) {
			Console.Error.WriteLine("Project validation errors:");
			foreach (var error in errors) {
				Console.Error.WriteLine($"  - {error}");
			}

			return 1;
		}

		var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectFile)) ?? ".";

		// Get effective configuration (merges base with config-specific settings)
		var configName = options.Configuration ?? project.DefaultConfiguration;
		var config = project.GetEffectiveConfiguration(configName);

		if (options.Verbose) {
			Console.WriteLine($"🌸 Building project: {project.Name}");
			Console.WriteLine($"   Target: {project.Target}");
			Console.WriteLine($"   Configuration: {configName}");
			Console.WriteLine($"   Directory: {projectDir}");
		}

		// Determine main source file
		string mainFile;
		if (!string.IsNullOrEmpty(project.Main)) {
			mainFile = Path.IsPathRooted(project.Main)
				? project.Main
				: Path.Combine(projectDir, project.Main);
		} else if (project.Sources.Count > 0) {
			// Use first source file as main
			var pattern = project.Sources[0];
			var files = Directory.GetFiles(projectDir, pattern, SearchOption.AllDirectories);
			if (files.Length == 0) {
				Console.Error.WriteLine($"Error: No source files match pattern: {pattern}");
				return 1;
			}

			mainFile = files[0];
		} else {
			Console.Error.WriteLine("Error: No main file or sources specified in project");
			return 1;
		}

		if (!File.Exists(mainFile)) {
			Console.Error.WriteLine($"Error: Main source file not found: {mainFile}");
			return 1;
		}

		// Build include paths from project
		var includePaths = new List<string>(options.IncludePaths);
		foreach (var inc in project.Includes) {
			var incPath = Path.IsPathRooted(inc) ? inc : Path.Combine(projectDir, inc);
			if (Directory.Exists(incPath)) {
				includePaths.Add(incPath);
			}
		}

		// Add project directory as include path
		includePaths.Add(projectDir);

		// Use config-based paths (falling back to project defaults)
		var outputPath = config.Output ?? project.Output ?? $"{project.Name}.bin";
		var symbolsPath = config.Symbols ?? project.Symbols;
		var listingPath = config.Listing ?? project.Listing;
		var mapFilePath = config.MapFile ?? project.MapFile;

		// Build with project settings
		var compileOptions = new CompilerOptions {
			InputFile = mainFile,
			OutputFile = options.OutputFile ?? (outputPath is not null
				? (Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(projectDir, outputPath))
				: Path.Combine(projectDir, $"{project.Name}.bin")),
			SymbolFile = options.SymbolFile ?? (symbolsPath is not null
				? (Path.IsPathRooted(symbolsPath) ? symbolsPath : Path.Combine(projectDir, symbolsPath))
				: null),
			ListingFile = options.ListingFile ?? (listingPath is not null
				? (Path.IsPathRooted(listingPath) ? listingPath : Path.Combine(projectDir, listingPath))
				: null),
			MapFile = options.MapFile ?? (mapFilePath is not null
				? (Path.IsPathRooted(mapFilePath) ? mapFilePath : Path.Combine(projectDir, mapFilePath))
				: null),
			Target = project.TargetArchitecture,
			Verbose = options.Verbose,
			AutoGenerateLabels = options.AutoGenerateLabels || project.AutoLabels
		};

		// Copy include paths
		foreach (var path in includePaths) {
			compileOptions.IncludePaths.Add(path);
		}

		// Compile
		var result = Compile(compileOptions);

		if (result == 0 && options.Verbose) {
			Console.WriteLine($"🌸 Build successful: {compileOptions.OutputFile}");
		}

		return result;
	}

	/// <summary>
	/// Cleans build artifacts from a project.
	/// </summary>
	private static int CleanProject(CompilerOptions options) {
		// Clean requires a project path
		var projectPath = options.ProjectPath ?? ".";
		string projectFile;

		if (Directory.Exists(projectPath)) {
			projectFile = Path.Combine(projectPath, "poppy.json");
		} else if (File.Exists(projectPath)) {
			projectFile = projectPath;
		} else {
			Console.Error.WriteLine($"Error: Project not found: {projectPath}");
			return 1;
		}

		if (!File.Exists(projectFile)) {
			Console.Error.WriteLine($"Error: Project file not found: {projectFile}");
			return 1;
		}

		// Load project
		ProjectFile project;
		try {
			project = ProjectFile.Load(projectFile);
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error loading project file: {ex.Message}");
			return 1;
		}

		var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectFile)) ?? ".";
		var deletedFiles = new List<string>();
		var deletedDirs = new List<string>();

		if (options.Verbose) {
			Console.WriteLine($"🌸 Cleaning project: {project.Name}");
			Console.WriteLine($"   Directory: {projectDir}");
			if (options.CleanAll) {
				Console.WriteLine("   Mode: All configurations");
			} else {
				Console.WriteLine($"   Configuration: {options.Configuration ?? project.DefaultConfiguration}");
			}
		}

		// Collect files to clean
		var filesToClean = new HashSet<string>();

		if (options.CleanAll) {
			// Clean base project outputs
			AddOutputFiles(filesToClean, projectDir, project.Output, project.Symbols, project.Listing, project.MapFile);

			// Clean all configuration outputs
			foreach (var (configName, config) in project.Configurations) {
				AddOutputFiles(filesToClean, projectDir, config.Output, config.Symbols, config.Listing, config.MapFile);
			}
		} else {
			// Clean specific configuration
			var configName = options.Configuration ?? project.DefaultConfiguration;
			var config = project.GetEffectiveConfiguration(configName);
			AddOutputFiles(filesToClean, projectDir, config.Output, config.Symbols, config.Listing, config.MapFile);
		}

		// Delete files
		foreach (var file in filesToClean) {
			if (File.Exists(file)) {
				try {
					File.Delete(file);
					deletedFiles.Add(file);
					if (options.Verbose) {
						Console.WriteLine($"   Deleted: {Path.GetRelativePath(projectDir, file)}");
					}
				} catch (Exception ex) {
					Console.Error.WriteLine($"   Warning: Could not delete {file}: {ex.Message}");
				}
			}
		}

		// Try to clean empty directories
		var dirsToCheck = filesToClean
			.Select(f => Path.GetDirectoryName(f))
			.Where(d => d is not null && d != projectDir)
			.Distinct()
			.OrderByDescending(d => d!.Length)  // Process deepest first
			.ToList();

		foreach (var dir in dirsToCheck) {
			if (dir is not null && Directory.Exists(dir)) {
				try {
					if (!Directory.EnumerateFileSystemEntries(dir).Any()) {
						Directory.Delete(dir);
						deletedDirs.Add(dir);
						if (options.Verbose) {
							Console.WriteLine($"   Removed empty directory: {Path.GetRelativePath(projectDir, dir)}");
						}
					}
				} catch {
					// Ignore directory deletion errors
				}
			}
		}

		// Summary
		if (deletedFiles.Count == 0 && deletedDirs.Count == 0) {
			Console.WriteLine("🌸 Nothing to clean.");
		} else {
			Console.WriteLine($"🌸 Cleaned {deletedFiles.Count} file(s)");
		}

		return 0;
	}

	/// <summary>
	/// Initializes a new project from a template.
	/// </summary>
	private static int InitProject(CompilerOptions options) {
		// Available platforms and their templates
		var platforms = new Dictionary<string, (string displayName, string cpu, string defaultExt)> {
			["nes"] = ("NES", "6502", "nes"),
			["snes"] = ("SNES", "65816", "sfc"),
			["gb"] = ("Game Boy", "sm83", "gb"),
			["genesis"] = ("Sega Genesis", "m68000", "bin"),
			["gba"] = ("Game Boy Advance", "arm7tdmi", "gba"),
			["sms"] = ("Sega Master System", "z80", "sms"),
			["tg16"] = ("TurboGrafx-16", "huc6280", "pce"),
			["a2600"] = ("Atari 2600", "6507", "a26"),
			["lynx"] = ("Atari Lynx", "65sc02", "lnx"),
			["ws"] = ("WonderSwan", "v30mz", "ws"),
			["spc700"] = ("SPC700 (SNES Audio)", "spc700", "spc")
		};

		string projectName;
		string platformKey;

		// Interactive mode if no project name provided
		if (options.InitProjectName is null) {
			Console.WriteLine("🌸 Poppy Project Initializer");
			Console.WriteLine();

			// Get project name
			Console.Write("Project name: ");
			projectName = Console.ReadLine()?.Trim() ?? "";
			if (string.IsNullOrEmpty(projectName)) {
				Console.Error.WriteLine("Error: Project name is required.");
				return 1;
			}

			// Show platform options
			Console.WriteLine();
			Console.WriteLine("Available platforms:");
			var platformList = platforms.ToList();
			for (int i = 0; i < platformList.Count; i++) {
				Console.WriteLine($"  {i + 1,2}. {platformList[i].Value.displayName,-25} ({platformList[i].Key})");
			}

			Console.WriteLine();
			Console.Write("Select platform (1-11 or name): ");
			var input = Console.ReadLine()?.Trim() ?? "";

			// Parse platform selection
			if (int.TryParse(input, out int index) && index >= 1 && index <= platformList.Count) {
				platformKey = platformList[index - 1].Key;
			} else if (platforms.ContainsKey(input.ToLowerInvariant())) {
				platformKey = input.ToLowerInvariant();
			} else {
				Console.Error.WriteLine($"Error: Invalid platform selection: {input}");
				return 1;
			}
		} else {
			projectName = options.InitProjectName;

			// Platform from command line or default to NES
			if (options.InitPlatform is not null) {
				var key = options.InitPlatform.ToLowerInvariant();
				if (!platforms.ContainsKey(key)) {
					Console.Error.WriteLine($"Error: Unknown platform: {options.InitPlatform}");
					Console.Error.WriteLine("Valid platforms: " + string.Join(", ", platforms.Keys));
					return 1;
				}
				platformKey = key;
			} else {
				// Prompt for platform
				Console.WriteLine("Available platforms:");
				var platformList = platforms.ToList();
				for (int i = 0; i < platformList.Count; i++) {
					Console.WriteLine($"  {i + 1,2}. {platformList[i].Value.displayName,-25} ({platformList[i].Key})");
				}

				Console.WriteLine();
				Console.Write("Select platform (1-11 or name): ");
				var input = Console.ReadLine()?.Trim() ?? "";

				if (int.TryParse(input, out int index) && index >= 1 && index <= platformList.Count) {
					platformKey = platformList[index - 1].Key;
				} else if (platforms.ContainsKey(input.ToLowerInvariant())) {
					platformKey = input.ToLowerInvariant();
				} else {
					Console.Error.WriteLine($"Error: Invalid platform selection: {input}");
					return 1;
				}
			}
		}

		var platform = platforms[platformKey];
		var targetDir = Path.GetFullPath(projectName);

		// Check if directory already exists
		if (Directory.Exists(targetDir)) {
			Console.Error.WriteLine($"Error: Directory already exists: {targetDir}");
			return 1;
		}

		// Create project structure
		try {
			Console.WriteLine();
			Console.WriteLine($"Creating {platform.displayName} project: {projectName}");

			// Create directories
			Directory.CreateDirectory(targetDir);
			Directory.CreateDirectory(Path.Combine(targetDir, "src"));

			// Create poppy.json
			var projectJson = $$"""
{
	"name": "{{projectName}}",
	"version": "1.0.0",
	"description": "{{platform.displayName}} game project",
	"target": "{{platformKey}}",
	"cpu": "{{platform.cpu}}",
	"output": {
		"format": "{{platformKey}}",
		"filename": "{{projectName}}.{{platform.defaultExt}}"
	},
	"sources": [
		"src/main.pasm"
	],
	"defaultConfiguration": "debug",
	"configurations": {
		"debug": {
			"listing": "{{projectName}}.lst",
			"symbols": "{{projectName}}.sym"
		},
		"release": {}
	}
}
""";
			File.WriteAllText(Path.Combine(targetDir, "poppy.json"), projectJson);
			Console.WriteLine("  Created poppy.json");

			// Create main.pasm
			var mainAsm = GetTemplateMainFile(platformKey, platform.cpu);
			File.WriteAllText(Path.Combine(targetDir, "src", "main.pasm"), mainAsm);
			Console.WriteLine("  Created src/main.pasm");

			// Create README.md
			var readme = $"""
# {projectName}

A {platform.displayName} project created with Poppy Compiler.

## Building

```bash
poppy --project
```

Or with a specific configuration:

```bash
poppy --project -c release
```

## Project Structure

- `poppy.json` - Project configuration
- `src/main.pasm` - Main source file

## Resources

- [Poppy User Manual](https://github.com/TheAnsarya/poppy/blob/main/docs/user-manual.md)
- [{platform.displayName} Reference](https://github.com/TheAnsarya/poppy/blob/main/docs/resources.md)
""";
			File.WriteAllText(Path.Combine(targetDir, "README.md"), readme);
			Console.WriteLine("  Created README.md");

			Console.WriteLine();
			Console.WriteLine($"🌸 Project created successfully!");
			Console.WriteLine();
			Console.WriteLine("Next steps:");
			Console.WriteLine($"  cd {projectName}");
			Console.WriteLine("  poppy --project");
			Console.WriteLine();

			return 0;
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error creating project: {ex.Message}");
			if (options.Verbose) {
				Console.Error.WriteLine(ex.StackTrace);
			}
			return 1;
		}
	}

	/// <summary>
	/// Gets a basic template main.pasm file for the given platform.
	/// </summary>
	private static string GetTemplateMainFile(string platform, string cpu) {
		return platform switch {
			"nes" => """
; =============================================================================
; NES ROM Template
; =============================================================================

.target "nes"
.cpu "6502"

; iNES Header
.db "NES", $1a	; Magic number
.db $02			; 2 * 16KB PRG ROM
.db $01			; 1 * 8KB CHR ROM
.db $00			; Flags 6: Mapper 0, horizontal mirroring
.db $00			; Flags 7
.ds 8, $00		; Padding

; =============================================================================
; Vectors
; =============================================================================

.org $c000

reset:
	sei				; Disable interrupts
	cld				; Clear decimal mode
	ldx #$ff
	txs				; Set up stack

	; Wait for PPU to stabilize
	bit $2002
@wait1:
	bit $2002
	bpl @wait1
@wait2:
	bit $2002
	bpl @wait2

main_loop:
	jmp main_loop

nmi:
	rti

irq:
	rti

; =============================================================================
; Vector Table
; =============================================================================

.org $fffa
	.dw nmi			; NMI vector
	.dw reset		; Reset vector
	.dw irq			; IRQ vector
""",

			"snes" => """
; =============================================================================
; SNES ROM Template
; =============================================================================

.target "snes"
.cpu "65816"

; =============================================================================
; ROM Header (LoROM)
; =============================================================================

.org $008000

; Code starts here
reset:
	sei
	clc
	xce					; Switch to native mode
	rep #$30			; 16-bit A, X, Y

	; Initialize stack
	lda #$1fff
	tcs

main_loop:
	wai					; Wait for interrupt
	bra main_loop

nmi:
	rti

irq:
	rti

; =============================================================================
; Vectors (Native mode)
; =============================================================================

.org $00ffe4
	.dw $0000			; COP
	.dw $0000			; BRK
	.dw $0000			; ABORT
	.dw nmi				; NMI
	.dw $0000			; unused
	.dw irq				; IRQ

; =============================================================================
; Vectors (Emulation mode)
; =============================================================================

.org $00fff4
	.dw $0000			; COP
	.dw $0000			; unused
	.dw $0000			; ABORT
	.dw nmi				; NMI
	.dw reset			; RESET
	.dw irq				; IRQ
""",

			"gb" => """
; =============================================================================
; Game Boy ROM Template
; =============================================================================

.target "gb"
.cpu "sm83"

; =============================================================================
; Entry Point
; =============================================================================

.org $0100
	nop
	jp start

; Nintendo logo (required)
.org $0104
	.db $ce, $ed, $66, $66, $cc, $0d, $00, $0b
	.db $03, $73, $00, $83, $00, $0c, $00, $0d
	.db $00, $08, $11, $1f, $88, $89, $00, $0e
	.db $dc, $cc, $6e, $e6, $dd, $dd, $d9, $99
	.db $bb, $bb, $67, $63, $6e, $0e, $ec, $cc
	.db $dd, $dc, $99, $9f, $bb, $b9, $33, $3e

.org $0134
	.db "MYGAME", 0, 0, 0, 0, 0	; Title

.org $0150

start:
	di
	ld sp, $fffe

	; Wait for VBlank
@wait_vblank:
	ldh a, [$44]
	cp 144
	jr c, @wait_vblank

	; Disable LCD
	xor a
	ldh [$40], a

main_loop:
	halt
	nop
	jr main_loop
""",

			"genesis" => $$"""
; =============================================================================
; Sega Genesis ROM Template
; =============================================================================

.target "genesis"
.cpu "m68000"

; =============================================================================
; Vector Table
; =============================================================================

.org $000000

	.dl $00ff0000		; Initial stack pointer
	.dl start			; Reset vector
	.ds 62 * 4, 0		; Other vectors

; =============================================================================
; Header
; =============================================================================

.org $000100
	.db "SEGA MEGA DRIVE "					; Console name
	.db "(C)YOUR 2025.JAN"					; Copyright
	.ds 96, ' '								; Game names
	.db "GM 00000000-00"					; Product code
	.dw $0000								; Checksum
	.db "J               "					; I/O support
	.dl $00000000, $000fffff				; ROM addresses
	.dl $00ff0000, $00ffffff				; RAM addresses
	.ds 52, ' '								; Reserved
	.db "JUE             "					; Region

; =============================================================================
; Main Code
; =============================================================================

.org $000200

start:
	; TMSS
	move.b	$a10001, d0
	andi.b	#$0f, d0
	beq.s	@no_tmss
	move.l	#'SEGA', $a14000
@no_tmss:

main_loop:
	bra.s	main_loop
""",

			_ => $$"""
; =============================================================================
; {{cpu.ToUpperInvariant()}} ROM Template
; =============================================================================

.target "{{platform}}"
.cpu "{{cpu}}"

; Main entry point
start:
	; TODO: Add initialization code

main_loop:
	; TODO: Add main loop
	jmp main_loop
"""
		};
	}

	/// <summary>
	/// Packs a project into a .poppy archive.
	/// </summary>
	private static int PackProject(CompilerOptions options) {
		var projectPath = options.ProjectPath ?? ".";

		if (options.Verbose) {
			Console.WriteLine($"🌸 Packing project from: {projectPath}");
		}

		try {
			var packOptions = new ArchiveHandler.PackOptions {
				OutputPath = options.OutputFile,
				IncludeBuild = false,  // Don't include build artifacts by default
				CalculateChecksums = true
			};

			var archivePath = ArchiveHandler.Pack(projectPath, packOptions);

			var fileInfo = new FileInfo(archivePath);
			Console.WriteLine($"🌸 Created archive: {archivePath}");
			Console.WriteLine($"   Size: {FormatFileSize(fileInfo.Length)}");

			return 0;
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error packing project: {ex.Message}");
			if (options.Verbose) {
				Console.Error.WriteLine(ex.StackTrace);
			}
			return 1;
		}
	}

	/// <summary>
	/// Unpacks a .poppy archive.
	/// </summary>
	private static int UnpackArchive(CompilerOptions options) {
		if (options.InputFile is null) {
			Console.Error.WriteLine("Error: No archive file specified.");
			Console.Error.WriteLine("Usage: poppy unpack <archive.poppy> [-o <directory>]");
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"🌸 Unpacking archive: {options.InputFile}");
		}

		try {
			var unpackOptions = new ArchiveHandler.UnpackOptions {
				TargetDirectory = options.OutputFile,  // Reuse -o flag for target directory
				Overwrite = options.CleanAll,  // Reuse --all flag as overwrite
				ValidateChecksums = true,
				ValidateManifest = true
			};

			var extractPath = ArchiveHandler.Unpack(options.InputFile, unpackOptions);

			Console.WriteLine($"🌸 Extracted to: {extractPath}");

			// Count files
			var fileCount = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories).Length;
			Console.WriteLine($"   Files: {fileCount}");

			return 0;
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error unpacking archive: {ex.Message}");
			if (options.Verbose) {
				Console.Error.WriteLine(ex.StackTrace);
			}
			return 1;
		}
	}

	/// <summary>
	/// Validates a .poppy archive.
	/// </summary>
	private static int ValidateArchive(CompilerOptions options) {
		if (options.InputFile is null) {
			Console.Error.WriteLine("Error: No archive file specified.");
			Console.Error.WriteLine("Usage: poppy validate <archive.poppy>");
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"🌸 Validating archive: {options.InputFile}");
		}

		try {
			var errors = ArchiveHandler.Validate(options.InputFile);

			if (errors.Count == 0) {
				Console.WriteLine("🌸 Archive is valid.");
				return 0;
			} else {
				Console.Error.WriteLine($"❌ Archive validation failed with {errors.Count} error(s):");
				foreach (var error in errors) {
					Console.Error.WriteLine($"   - {error}");
				}
				return 1;
			}
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error validating archive: {ex.Message}");
			if (options.Verbose) {
				Console.Error.WriteLine(ex.StackTrace);
			}
			return 1;
		}
	}

	/// <summary>
	/// Formats a file size for display.
	/// </summary>
	private static string FormatFileSize(long bytes) {
		if (bytes < 1024) return $"{bytes} bytes";
		if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
		if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
		return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
	}

	/// <summary>
	/// Adds output file paths to the set of files to clean.
	/// </summary>
	private static void AddOutputFiles(HashSet<string> files, string projectDir, params string?[] paths) {
		foreach (var path in paths) {
			if (path is not null) {
				var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(projectDir, path);
				files.Add(Path.GetFullPath(fullPath));
			}
		}
	}

	/// <summary>
	/// Compiles a source file.
	/// </summary>
	private static int Compile(CompilerOptions options) {
		var inputFile = options.InputFile!;

		// Check input file exists
		if (!File.Exists(inputFile)) {
			Console.Error.WriteLine($"Error: Input file not found: {inputFile}");
			return 1;
		}

		// Read source file
		string source;
		try {
			source = File.ReadAllText(inputFile);
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error reading input file: {ex.Message}");
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"Compiling: {inputFile}");
		}

		// Preprocessing (handles .include directives)
		var preprocessor = new Preprocessor(options.IncludePaths);
		var tokens = preprocessor.Process(source, inputFile);

		if (preprocessor.HasErrors) {
			foreach (var error in preprocessor.Errors) {
				Console.Error.WriteLine($"{error.Location.FilePath}:{error.Location.Line}:{error.Location.Column}: error: {error.Message}");
			}

			return 1;
		}

		// Check for lexer errors (Error tokens)
		var lexerErrors = tokens.Where(t => t.Type == TokenType.Error).ToList();
		if (lexerErrors.Count > 0) {
			foreach (var error in lexerErrors) {
				Console.Error.WriteLine($"{error.Location.FilePath}:{error.Location.Line}:{error.Location.Column}: error: {error.Text}");
			}

			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Tokenized: {tokens.Count} tokens");
		}

		// Parsing
		var parser = new Parser(tokens);
		ProgramNode program;
		try {
			program = parser.Parse();
		} catch (ParseException ex) {
			Console.Error.WriteLine($"{inputFile}:{ex.Location.Line}:{ex.Location.Column}: error: {ex.Message}");
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Parsed: {program.Statements.Count} statements");
		}

		// Semantic analysis
		var analyzer = new SemanticAnalyzer(options.Target);
		analyzer.AutoGenerateRoutineLabels = options.AutoGenerateLabels;
		analyzer.Analyze(program);

		if (analyzer.HasErrors) {
			foreach (var error in analyzer.Errors) {
				Console.Error.WriteLine($"{inputFile}:{error.Location.Line}:{error.Location.Column}: error: {error.Message}");
			}

			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Analyzed: {analyzer.SymbolTable.Symbols.Count} symbols defined");
		}

		// Code generation
		var generator = new CodeGenerator(analyzer, options.Target);
		var code = generator.Generate(program);

		if (generator.HasErrors) {
			foreach (var error in generator.Errors) {
				Console.Error.WriteLine($"{inputFile}:{error.Location.Line}:{error.Location.Column}: error: {error.Message}");
			}

			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Generated: {code.Length} bytes");
		}

		// Determine output file
		var outputFile = options.OutputFile ?? Path.ChangeExtension(inputFile, ".bin");

		// Write output
		try {
			File.WriteAllBytes(outputFile, code);
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error writing output file: {ex.Message}");
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Output: {outputFile}");
		}

		// Write listing file if requested
		if (options.ListingFile is not null) {
			try {
				WriteListing(options.ListingFile, program, analyzer, code);
				if (options.Verbose) {
					Console.WriteLine($"  Listing: {options.ListingFile}");
				}
			} catch (Exception ex) {
				Console.Error.WriteLine($"Error writing listing file: {ex.Message}");
				return 1;
			}
		}

		// Write symbol file if requested
		if (options.SymbolFile is not null) {
			try {
				var symbolExporter = new SymbolExporter(analyzer.SymbolTable, analyzer.Target);
				symbolExporter.Export(options.SymbolFile);
				if (options.Verbose) {
					Console.WriteLine($"  Symbols: {options.SymbolFile}");
				}
			} catch (Exception ex) {
				Console.Error.WriteLine($"Error writing symbol file: {ex.Message}");
				return 1;
			}
		}

		// Write memory map file if requested
		if (options.MapFile is not null) {
			try {
				var mapGenerator = new MemoryMapGenerator(generator.Segments, analyzer.SymbolTable, analyzer.Target);
				mapGenerator.Export(options.MapFile);
				if (options.Verbose) {
					Console.WriteLine($"  Map: {options.MapFile}");
				}
			} catch (Exception ex) {
				Console.Error.WriteLine($"Error writing map file: {ex.Message}");
				return 1;
			}
		}

		// Write CDL (Code/Data Log) file if requested
		if (options.CdlFile is not null) {
			try {
				var cdlFormat = options.CdlFormat.ToLowerInvariant() switch {
					"fceux" or "fce" => CdlGenerator.CdlFormat.FCEUX,
					_ => CdlGenerator.CdlFormat.Mesen
				};
				var cdlGenerator = new CdlGenerator(analyzer.SymbolTable, analyzer.Target, generator.Segments);
				cdlGenerator.Export(options.CdlFile, code.Length, cdlFormat);
				if (options.Verbose) {
					Console.WriteLine($"  CDL: {options.CdlFile} ({options.CdlFormat} format)");
				}
			} catch (Exception ex) {
				Console.Error.WriteLine($"Error writing CDL file: {ex.Message}");
				return 1;
			}
		}

		// Write DIZ (DiztinGUIsh) project file if requested
		if (options.DizFile is not null) {
			try {
				var projectName = Path.GetFileNameWithoutExtension(options.DizFile);
				var dizGenerator = new DizGenerator(analyzer.SymbolTable, analyzer.Target, generator.Segments, projectName);
				dizGenerator.Export(options.DizFile, code);
				if (options.Verbose) {
					Console.WriteLine($"  DIZ: {options.DizFile}");
				}
			} catch (Exception ex) {
				Console.Error.WriteLine($"Error writing DIZ file: {ex.Message}");
				return 1;
			}
		}

		Console.WriteLine($"Assembled {inputFile} -> {outputFile} ({code.Length} bytes)");
		return 0;
	}

	/// <summary>
	/// Writes a listing file.
	/// </summary>
	private static void WriteListing(string filename, ProgramNode program, SemanticAnalyzer analyzer, byte[] code) {
		using var writer = new StreamWriter(filename);

		writer.WriteLine($"; {AppName} v{Version} Listing");
		writer.WriteLine($"; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
		writer.WriteLine();

		writer.WriteLine("; Symbol Table");
		writer.WriteLine("; " + new string('-', 60));

		foreach (var symbol in analyzer.SymbolTable.Symbols.Values.OrderBy(s => s.Name)) {
			var valueStr = symbol.Value.HasValue ? $"${symbol.Value.Value:x4}" : "(undefined)";
			writer.WriteLine($"; {symbol.Name,-20} = {valueStr,-10} ({symbol.Type})");
		}

		writer.WriteLine();
		writer.WriteLine($"; Total code size: {code.Length} bytes (${code.Length:x})");
	}

	/// <summary>
	/// Parses command line arguments.
	/// </summary>
	private static CompilerOptions ParseArguments(string[] args) {
		var options = new CompilerOptions();

		for (int i = 0; i < args.Length; i++) {
			var arg = args[i];

			switch (arg) {
				case "clean":
					// Clean command
					options.Command = CommandType.Clean;
					break;

				case "init":
					// Init command - next arg is project name
					options.Command = CommandType.Init;
					if (i + 1 < args.Length && !args[i + 1].StartsWith('-')) {
						options.InitProjectName = args[++i];
					}

					break;

			case "pack":
				// Pack command
				options.Command = CommandType.Pack;
				break;

			case "unpack":
				// Unpack command
				options.Command = CommandType.Unpack;
				break;

			case "validate":
				// Validate command
				options.Command = CommandType.Validate;
				break;

				case "-h":
				case "--help":
					options.ShowHelp = true;
					break;

				case "-v":
				case "--version":
					options.ShowVersion = true;
					break;

				case "-V":
				case "--verbose":
					options.Verbose = true;
					break;

				case "-o":
				case "--output":
					if (i + 1 < args.Length) {
						options.OutputFile = args[++i];
					}

					break;

				case "-l":
				case "--listing":
					if (i + 1 < args.Length) {
						options.ListingFile = args[++i];
					}

					break;

				case "-s":
				case "--symbols":
					if (i + 1 < args.Length) {
						options.SymbolFile = args[++i];
					}

					break;

				case "-m":
				case "--mapfile":
					if (i + 1 < args.Length) {
						options.MapFile = args[++i];
					}

					break;

				case "--cdl":
					if (i + 1 < args.Length) {
						options.CdlFile = args[++i];
					}

					break;

				case "--cdl-format":
					if (i + 1 < args.Length) {
						options.CdlFormat = args[++i].ToLowerInvariant();
					}

					break;

				case "--diz":
					if (i + 1 < args.Length) {
						options.DizFile = args[++i];
					}

					break;

				case "-t":
				case "--target":
					if (i + 1 < args.Length) {
						options.Target = args[++i].ToLowerInvariant() switch {
							"6502" or "nes" => TargetArchitecture.MOS6502,
							"65816" or "snes" => TargetArchitecture.WDC65816,
							"sm83" or "gb" or "gameboy" => TargetArchitecture.SM83,
							_ => options.Target
						};
					}

					break;

				case "-I":
				case "--include":
					if (i + 1 < args.Length) {
						options.IncludePaths.Add(args[++i]);
					}

					break;

				case "-a":
				case "--auto-labels":
					options.AutoGenerateLabels = true;
					break;

				case "-w":
				case "--watch":
					options.Watch = true;
					break;

				case "-p":
				case "--project":
					// Check if next arg is a path or another flag
					if (i + 1 < args.Length && !args[i + 1].StartsWith('-')) {
						options.ProjectPath = args[++i];
					} else {
						// Use current directory
						options.ProjectPath = ".";
					}

					break;

				case "-c":
				case "--config":
					if (i + 1 < args.Length) {
						options.Configuration = args[++i];
					}

					break;

				case "--template":
					if (i + 1 < args.Length) {
						options.TemplateName = args[++i];
					}

					break;

				case "--platform":
					if (i + 1 < args.Length) {
						options.InitPlatform = args[++i];
					}

					break;

				default:
					if (!arg.StartsWith('-')) {
						options.InputFile = arg;
					}

					break;
			}
		}

		return options;
	}

	/// <summary>
	/// Shows help message.
	/// </summary>
	private static void ShowHelp() {
		Console.WriteLine($"🌸 {AppName} v{Version}");
		Console.WriteLine();
		Console.WriteLine("Usage: poppy [options] <input.pasm>");
		Console.WriteLine("       poppy --project [path] [--config <name>]");
		Console.WriteLine("       poppy init <name> [--platform <platform>]");
		Console.WriteLine("       poppy clean --project [path] [--all]");
		Console.WriteLine("       poppy pack [path] [-o <output.poppy>]");
		Console.WriteLine("       poppy unpack <archive.poppy> [-o <directory>]");
		Console.WriteLine("       poppy validate <archive.poppy>");
		Console.WriteLine();
		Console.WriteLine("Commands:");
		Console.WriteLine("  init <name>          Create a new project from template");
		Console.WriteLine("  clean                Remove build artifacts from a project");
		Console.WriteLine("  pack                 Pack project into .poppy archive");
		Console.WriteLine("  unpack               Extract .poppy archive");
		Console.WriteLine("  validate             Validate .poppy archive integrity");
		Console.WriteLine();
		Console.WriteLine("Options:");
		Console.WriteLine("  -h, --help           Show this help message");
		Console.WriteLine("  -v, --version        Show version information");
		Console.WriteLine("  -V, --verbose        Enable verbose output");
		Console.WriteLine("  -o, --output <file>  Output file (default: input.bin)");
		Console.WriteLine("  -l, --listing <file> Generate listing file");
		Console.WriteLine("  -s, --symbols <file> Generate symbol file (.nl, .mlb, .sym)");
		Console.WriteLine("  -m, --mapfile <file> Generate memory map file");
		Console.WriteLine("  --cdl <file>         Generate CDL (Code/Data Log) file");
		Console.WriteLine("  --cdl-format <fmt>   CDL format: mesen (default), fceux");
		Console.WriteLine("  --diz <file>         Generate DiztinGUIsh project file (.diz)");
		Console.WriteLine("  -a, --auto-labels    Auto-generate labels for JSR/JMP targets");
		Console.WriteLine("  -w, --watch          Watch mode: recompile on file changes");
		Console.WriteLine("  -I, --include <path> Add include search path");
		Console.WriteLine("  -p, --project [path] Build from project file (poppy.json)");
		Console.WriteLine("  -c, --config <name>  Build configuration (debug, release, etc.)");
		Console.WriteLine("  --platform <name>    Platform for init (nes, snes, gb, genesis, etc.)");
		Console.WriteLine("  --all                Clean all configurations (with clean command)");
		Console.WriteLine("                       or overwrite when unpacking");
		Console.WriteLine("  -t, --target <arch>  Target architecture:");
		Console.WriteLine("                         6502, nes     - MOS 6502 (default)");
		Console.WriteLine("                         65816, snes   - WDC 65816");
		Console.WriteLine("                         sm83, gb      - Sharp SM83 (Game Boy)");
		Console.WriteLine();
		Console.WriteLine("Platforms (for init):");
		Console.WriteLine("  nes       NES (6502)              snes      SNES (65816)");
		Console.WriteLine("  gb        Game Boy (SM83)         genesis   Sega Genesis (M68000)");
		Console.WriteLine("  gba       Game Boy Advance (ARM)  sms       Master System (Z80)");
		Console.WriteLine("  tg16      TurboGrafx-16           a2600     Atari 2600 (6507)");
		Console.WriteLine("  lynx      Atari Lynx (65SC02)     ws        WonderSwan (V30MZ)");
		Console.WriteLine("  spc700    SPC700 (SNES Audio)");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  poppy game.pasm                    Assemble to game.bin");
		Console.WriteLine("  poppy -o rom.nes game.pasm         Assemble to rom.nes");
		Console.WriteLine("  poppy -t snes -l game.lst game.pasm");
		Console.WriteLine("  poppy init my-game                 Create project (interactive)");
		Console.WriteLine("  poppy init my-game --platform nes  Create NES project");
		Console.WriteLine("  poppy --project                    Build from ./poppy.json");
		Console.WriteLine("  poppy --project path/to/game       Build from project directory");
		Console.WriteLine("  poppy --project -c release         Build release configuration");
		Console.WriteLine("  poppy clean --project              Clean default config outputs");
		Console.WriteLine("  poppy clean --project --all        Clean all config outputs");
		Console.WriteLine("  poppy pack my-game                 Pack project into my-game.poppy");
		Console.WriteLine("  poppy pack . -o custom.poppy       Pack current directory");
		Console.WriteLine("  poppy unpack game.poppy            Extract to ./game");
		Console.WriteLine("  poppy unpack game.poppy -o mydir   Extract to ./mydir");
		Console.WriteLine("  poppy validate game.poppy          Check archive integrity");
	}

	/// <summary>
	/// Shows version information.
	/// </summary>
	private static void ShowVersion() {
		Console.WriteLine($"{AppName} v{Version}");
		Console.WriteLine("Target architectures: 6502, 65816, SM83");
		Console.WriteLine("Copyright (c) 2024");
	}
}

/// <summary>
/// Command type for CLI operations.
/// </summary>
internal enum CommandType {
	/// <summary>Build/compile (default).</summary>
	Build,

	/// <summary>Clean build artifacts.</summary>
	Clean,

	/// <summary>Initialize new project.</summary>
	Init,

	/// <summary>Pack project into .poppy archive.</summary>
	Pack,

	/// <summary>Unpack .poppy archive.</summary>
	Unpack,

	/// <summary>Validate .poppy archive.</summary>
	Validate
}

/// <summary>
/// Compiler options parsed from command line.
/// </summary>
internal sealed class CompilerOptions {
	/// <summary>Command to execute.</summary>
	public CommandType Command { get; set; } = CommandType.Build;

	/// <summary>Input source file.</summary>
	public string? InputFile { get; set; }

	/// <summary>Output binary file.</summary>
	public string? OutputFile { get; set; }

	/// <summary>Listing file path.</summary>
	public string? ListingFile { get; set; }

	/// <summary>Symbol file path.</summary>
	public string? SymbolFile { get; set; }

	/// <summary>Memory map file path.</summary>
	public string? MapFile { get; set; }

	/// <summary>CDL (Code/Data Log) output file path.</summary>
	public string? CdlFile { get; set; }

	/// <summary>CDL format (mesen or fceux).</summary>
	public string CdlFormat { get; set; } = "mesen";

	/// <summary>DIZ (DiztinGUIsh) project file path.</summary>
	public string? DizFile { get; set; }

	/// <summary>Project file or directory path.</summary>
	public string? ProjectPath { get; set; }

	/// <summary>Build configuration name (debug, release, etc.).</summary>
	public string? Configuration { get; set; }

	/// <summary>Clean all configurations.</summary>
	public bool CleanAll { get; set; }

	/// <summary>Include search paths.</summary>
	public List<string> IncludePaths { get; } = [];

	/// <summary>Target architecture.</summary>
	public TargetArchitecture Target { get; set; } = TargetArchitecture.MOS6502;

	/// <summary>Show help message.</summary>
	public bool ShowHelp { get; set; }

	/// <summary>Show version.</summary>
	public bool ShowVersion { get; set; }

	/// <summary>Enable verbose output.</summary>
	public bool Verbose { get; set; }

	/// <summary>Auto-generate labels for JSR/JMP targets.</summary>
	public bool AutoGenerateLabels { get; set; }

	/// <summary>Watch mode for automatic recompilation.</summary>
	public bool Watch { get; set; }

	/// <summary>Template name for init command.</summary>
	public string? TemplateName { get; set; }

	/// <summary>Project name for init command.</summary>
	public string? InitProjectName { get; set; }

	/// <summary>Platform/target for init command.</summary>
	public string? InitPlatform { get; set; }
}
