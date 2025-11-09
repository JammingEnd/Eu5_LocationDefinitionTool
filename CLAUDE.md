# EU5 Map Tool - CLAUDE.md

## Project Overview

**EU5 Map Tool** is a desktop application for efficiently editing and painting Europa Universalis V (EU5) game mod location definitions. The tool enables creators to modify thousands of provinces and their associated data (topography, vegetation, climate, religion, culture, etc.) without manual editing of text files.

**Repository**: EU5_MapTool (C#/.NET)
**Status**: Official release (v1.0+)
**Target Users**: Game modders creating EU5 location mods

---

## Technology Stack

### Core Framework & Language
- **Language**: C# (.NET 9.0)
- **UI Framework**: Avalonia 11.3.6 (Cross-platform desktop framework)
- **Architecture Pattern**: MVVM (Model-View-ViewModel)
- **Build System**: .NET SDK with MSBuild

### Key Dependencies
- `CommunityToolkit.Mvvm` (8.2.1) - For MVVM patterns and observable properties
- `Avalonia.Desktop` (11.3.6) - Desktop lifetime management
- `Avalonia.Themes.Fluent` (11.3.6) - Fluent UI theme
- `Avalonia.Diagnostics` (11.3.6) - Diagnostics tools (debug-only)
- `Avalonia.Fonts.Inter` (11.3.6) - Inter font support

### Build Output
- **Target Framework**: .NET 9.0
- **Output Type**: Windows Executable (WinExe)
- **Safety Features**: Null safety enabled, unsafe blocks allowed (for media operations)
- **Compiled Bindings**: Enabled

---

## Project Structure

```
Eu5_MapTool/
├── Models/
│   ├── ProvinceInfo.cs           # Main domain model representing a province/location
│   └── StaticConstucts.cs        # Constants for file paths and static maps
│
├── logic/
│   ├── ProvinceLocation.cs       # Struct for location properties (topography, climate, etc.)
│   └── ProvincePopInfo.cs        # Population (Pop) definitions and size tracking
│
├── Services/
│   ├── IAppStorageInterface.cs   # Interface for data loading
│   ├── AppStorageService.cs      # Implements async loading of game data from files
│   ├── IModFileWriter.cs         # Interface for file writing
│   └── ModFileWriterService.cs   # Writes modified data back to mod files
│
├── ViewModels/
│   ├── ViewModelBase.cs          # Base class for MVVM properties
│   ├── MainWindowViewModel.cs    # Main application logic (province painting, selection)
│   └── StartupDialogViewModel.cs # Startup dialog for folder selection
│
├── Views/
│   ├── MainWindow.axaml          # Main UI layout (map viewer, tools, info panels)
│   ├── MainWindow.axaml.cs       # Code-behind with user interaction logic
│   ├── StartupDialogWindow.axaml # Initial folder selection dialog
│   ├── StartupDialogWindow.axaml.cs
│   ├── WriteProcesView.axaml     # Write process indicator
│   └── WriteProcesView.axaml.cs
│
├── cache/
│   └── chacheClasses.cs          # Cache wrapper classes for game definitions (religions, cultures, etc.)
│
├── Settings/
│   └── Settings.cs               # Persistent user settings (JSON-based)
│
├── App.axaml                     # Application root XAML
├── App.axaml.cs                  # Application lifecycle setup
├── Program.cs                    # Entry point
├── ViewLocator.cs                # MVVM view/viewmodel resolution
└── Eu5_MapTool.csproj            # Project configuration
```

---

## Build & Run Commands

### Prerequisites
- .NET 9.0 SDK installed
- Windows/Linux/macOS (Avalonia is cross-platform)

### Build
```bash
# Debug build
dotnet build Eu5_MapTool.sln

# Release build (optimized)
dotnet build Eu5_MapTool.sln -c Release

# Publish as standalone executable
dotnet publish Eu5_MapTool/Eu5_MapTool.csproj -c Release -r win-x64 --self-contained
```

### Run
```bash
# From IDE (Rider)
dotnet run --project Eu5_MapTool/Eu5_MapTool.csproj

# Direct executable
./Eu5_MapTool/bin/Debug/net9.0/Eu5_MapTool.exe
```

### No Testing Framework
- Currently no unit tests or test framework configured
- Testing is manual through GUI interaction
- Critical workflows should be tested with backup game folders per README

---

## High-Level Architecture

### Data Flow Architecture

```
User Input
    ↓
[StartupDialog] → Folder selection (BaseGame & Modded paths)
    ↓
[AppStorageService] → Async file loading
    ├→ Load hex→name mappings (named_locations/*.txt)
    ├→ Load location templates (location_templates.txt)
    ├→ Load pop definitions (pop files)
    └→ Load game definitions (religions, cultures, topography, etc.)
    ↓
[Cache] → Combined base game + mod definitions
    ↓
[MainWindowViewModel] → In-memory province data + user interactions
    ├→ Select tool (view province data)
    ├→ Paint tool (modify location/pop data)
    └→ Painted locations accumulate in _paintedLocations dict
    ↓
[ModFileWriterService] → Write changes back to mod files
    ├→ Write location name mappings
    ├→ Write location templates
    └→ Write pop definitions
    ↓
Mod Files Updated
```

### Three Primary Tools

1. **Select Tool**: Click provinces to view their current data
2. **Paint Tool**: Modify location properties (topography, climate, religion, culture, vegetation, raw materials) or pop information (type, size, culture, religion)
3. **Province Tool**: (Not yet implemented)

### Domain Models

**ProvinceInfo** (Models/ProvinceInfo.cs)
```csharp
public class ProvinceInfo {
    public string Id;              // Hex color code (e.g., "FFF000")
    public string Name;            // Human-readable name or generated ID
    public string OldName;         // Track original name for renames
    public ProvinceLocation LocationInfo;  // Location-specific data
    public ProvincePopInfo PopInfo;        // Population data
}
```

**ProvinceLocation** (logic/ProvinceLocation.cs) - Struct
```csharp
public struct ProvinceLocation {
    public string Topography;              // flatland, forest, etc.
    public string Vegetation;              // jungle, grassland, etc.
    public string Climate;                 // tropical, temperate, etc.
    public string Religion;                // bon, lutheran, etc.
    public string Culture;                 // dakelh_culture, swedish, etc.
    public string RawMaterial;             // horses, wine, etc.
    public string NaturalHarborSuitability; // 0.00 - 1.00
}
```

**ProvincePopInfo** (logic/ProvincePopInfo.cs)
```csharp
public class ProvincePopInfo {
    public List<PopDef> Pops;  // Collection of population groups
}

public class PopDef {
    public string PopType;    // clergy, nobles, townsmen, etc.
    public float Size;        // 0-1 scale
    public string Culture;
    public string Religion;
}
```

### Service Layer (MVVM-friendly)

**IAppStorageInterface** - Async data loading contract
- `LoadBaseGameAsync()` → Load reference game definitions
- `LoadModdedAsync()` → Load user's mod location data
- `LoadMapImageAsync()` → Load province hex map image
- `Load*ListAsync(path)` → Load game definitions (religions, cultures, topography, etc.)

**AppStorageService** - File I/O implementation
- Reads EU5 game format text files (key = value pairs, nested braces)
- Parses location hex-to-name mappings from `named_locations/*.txt`
- Parses location templates from `location_templates.txt`
- Parses population definitions from pop info files
- Implements regex-based parsing for complex nested structures
- Returns `Dictionary<string, ProvinceInfo>` keyed by province hex ID

**IModFileWriter** - Write contract
- `WriteLocationMapAsync()` → Update hex-to-name mapping file
- `WriteLocationInfoAsync()` → Update location_templates.txt
- `WriteProvincePopInfoAsync()` → Update pop definition files

**ModFileWriterService** - File writing implementation
- Merges changes into existing files (preserves unmodified entries)
- Handles province renames and new location creation
- Smart pop merging: matches pops by (type, culture, religion) and updates size
- Implements regex-based value replacement for surgical updates
- Generates random IDs (___XXXXXXXXXX format) for new locations

### Cache Architecture

**Cache** (cache/chacheClasses.cs)
- Holds combined sets of game definitions (base game + mod additions)
- Used for UI autocomplete and validation

Classes:
- `ReligionC`, `CulturesC`, `TopographyC`, `VegetationC`, `ClimateC`, `RawMaterialsC`, `PopTypesC`
- All inherit from `CombinedCacheItem` which merges base + modded sets

### ViewModel Architecture

**MainWindowViewModel** (ViewModels/MainWindowViewModel.cs)
- Holds `Provinces` dictionary (loaded game data)
- Holds `_paintedLocations` dictionary (in-progress user changes)
- Implements three tool handlers:
  - `OnSelect(provinceId)` → Display province info in UI
  - `OnPaint(provinceId, properties...)` → Modify location data
  - `OnPaintPop(provinceId, pops)` → Modify population data
- `WriteChanges()` → Batch write all painted locations to files
- Manages active province display in right panel
- Formats location info for display

**StartupDialogViewModel** (ViewModels/StartupDialogViewModel.cs)
- Handles initial folder selection
- Sets directories for both AppStorageService and ModFileWriterService
- `WasAccepted` flag prevents app launch without proper paths

### View Layer

**MainWindow.axaml & Code-behind**
- Layout: DockPanel with Left (tools), Right (info), Center (map)
- Left panel: Tool selection buttons (Select, Paint, Province)
- Right panel: Scrollable tool settings and province info display
- Center: Interactive map image with zoom/pan controls
- Image interaction:
  - Pointer wheel → zoom (0.1x to 50x)
  - Drag → pan across map
  - Click → Select/Paint based on active tool
- Dynamic UI: Tool options panel updates based on selected tool and paint type

---

## Critical File Formats

### Named Locations Files (`in_game/map_data/named_locations/*.txt`)
```
location_name = FFF000
stockholm = ABABAB
paris = 123456
```

### Location Templates (`in_game/map_data/location_templates.txt`)
```
FFF000 = { topography = flatland vegetation = jungle climate = tropical religion = bon culture = dakelh_culture raw_material = horses natural_harbor_suitability = 0.00 }
```

### Pop Definition Files (`main_menu/setup/start/*pops*.txt`)
```
locations = {
    stockholm = {
        define_pop = { type = clergy size = 0.00021 culture = swedish religion = lutheran }
        define_pop = { type = nobles size = 0.0003 culture = swedish religion = lutheran }
    }
    paris = {
        define_pop = { type = townsmen size = 0.0015 culture = french religion = catholic }
    }
}
```

### Game Definition Files (`in_game/common/*/`)
- `religions/*.txt` - Religion definitions
- `cultures/*.txt` - Culture definitions
- `topography/*.txt` - Topography types
- `climates/*.txt` - Climate definitions
- `vegetation/*.txt` - Vegetation types
- `goods/*.txt` - Raw materials (marked with `category = raw_material`)
- `pop_types/*.txt` - Pop type definitions

---

## Key Workflows

### 1. Application Startup
1. `App.xaml.cs` → `OnFrameworkInitializationCompleted()`
2. Creates `MainWindow` with `MainWindowViewModel`
3. Shows `StartupDialogWindow` (modal)
4. Dialog gets two folder paths: BaseGame path & Modded path
5. Both services initialized with paths
6. Data loading begins asynchronously
7. Map image loaded
8. UI becomes interactive

### 2. Data Loading (Async)
```
StartupDialogViewModel.SaveAndContinue()
  → AppStorageService.SetDirectories()
  → AppStorageService.LoadBaseGameAsync()
  → AppStorageService.LoadModdedAsync()
  → AppStorageService.LoadMapImageAsync()
  → Cache populated with game definitions
  → MainWindowViewModel.LoadProvinces(Dictionary)
  → UI displays map and province data
```

### 3. Province Selection
1. User clicks map at color (province hex)
2. `MainWindow.MapImage_PointerPressed()` extracts pixel color
3. Converts to hex string
4. Calls `MainWindowViewModel.OnSelect(hexColor)`
5. ViewModel populates right panel with current location data
6. If province hasn't been painted, data comes from `Provinces` dict
7. If already painted, data comes from `_paintedLocations` dict

### 4. Painting Location Data
1. User selects Paint tool
2. Selects "Location" paint type
3. Adds filter controls (autocomplete dropdowns for each property)
4. User selects values for: topography, vegetation, climate, religion, culture, raw_material
5. User clicks map
6. `MainWindowViewModel.OnPaint()` called with hex + selected values
7. Creates or updates entry in `_paintedLocations` dictionary
8. Shows generated name (___XXXXXXXXXX) or allows manual rename
9. Right panel updates to show painted values

### 5. Painting Pop Data
1. User selects Paint tool
2. Selects "Pop" paint type
3. Adds filter controls (PopType, Size, Culture, Religion)
4. Repeats: adds new pop definitions to the stack
5. User clicks map
6. `MainWindowViewModel.OnPaintPop()` called with hex + pop list
7. Updates `PopInfo.Pops` in `_paintedLocations[hexId]`
8. Merges duplicate pops (same type/culture/religion) → updates size

### 6. Writing Changes to Files
1. User clicks "WRITE" button
2. `MainWindowViewModel.WriteChanges()` executed
3. Batch writes all `_paintedLocations`:
   - `ModFileWriterService.WriteLocationMapAsync()` → Updates hex-to-name file
   - `ModFileWriterService.WriteLocationInfoAsync()` → Updates location_templates.txt
   - `ModFileWriterService.WriteProvincePopInfoAsync()` → Updates pop files
4. Each method:
   - Reads existing file
   - Merges changes (only modifies entries in `_paintedLocations`)
   - Writes updated file
5. `_paintedLocations` cleared
6. User can continue editing or close

---

## Important Implementation Details

### Parsing Strategy
- **Key-value parsing**: Uses IndexOf('=') to split on equals signs
- **Nested structure handling**: Tracks brace depth for nested definitions
- **Regex patterns**:
  - Location blocks: `@"^([A-Za-z0-9_]+)\s*=\s*\{$"`
  - Pop lines: `type\s*=\s*(\S+)\s+size\s*=\s*(\S+)\s+culture\s*=\s*(\S+)\s+religion\s*=\s*(\S+)`
  - Properties: `({key}\s*=\s*)(\S+)` → Replace value only
- **Case insensitivity**: Dictionaries use `StringComparer.OrdinalIgnoreCase` for EU5 data

### Pop Info Merging
- When painting pops on a province: `MergePopUpdates()` deduplicates
- Matches by (PopType, Culture, Religion) tuple
- Later entries override earlier entries (size updated)
- Result: no duplicate pop definitions for same type/culture/religion

### Province Naming
- New provinces get random generated names: `___XXXXXXXXXX` (3 underscores + 10 random alphanumeric)
- User can rename via NameBox textfield in right panel
- Old name tracked in `ProvinceInfo.OldName` to handle rename in file writes

### File Writing Merge Logic
- **Location map**: Read existing file → build dictionary → add painted changes → write
- **Location info**: Line-by-line update - if province ID found in painted set, rewrite line; otherwise preserve
- **Pop info**: Parse location blocks → update existing blocks or create new ones → write wrapper

### Settings Persistence
- `SettingsService` (Settings/Settings.cs) saves/loads JSON to AppData
- Currently stores last used directory paths
- Used for remembering user's last selections

---

## Known Issues & Limitations

From README.md:
1. **Removing instances in named_locations files** while they have data elsewhere crashes the app
2. **Name Write** reverts to placeholder under specific circumstances
3. **Painting pop info** without location info yields weird results → workaround: paint location info first
4. **Province Tool** not yet implemented

Code comments indicate TODO items:
- `StaticConstucts.cs`: "these things implemented"
- `MainWindowViewModel.cs`: Fill in static map with new values
- `ModFileWriterService.cs`: Using static maps to generate location info

---

## Development Notes & Code Quality

### Positive Aspects
- Clean MVVM separation of concerns
- Async I/O operations (no UI blocking)
- Comprehensive file parsing logic
- Smart merge strategies for file updates
- Cross-platform framework (Avalonia)

### Code Smells & Technical Debt
- Typo: "chacheClasses.cs" should be "CacheClasses.cs"
- Hard-coded paths in StaticConstucts - could be configurable
- `MainWindowViewModel.cs` line 206-207: Comment "I HATE THIS" × 8 over pop merging code
- Limited error handling (some try-catch, but not comprehensive)
- No logging framework (uses Console.WriteLine)
- Large code-behind file in MainWindow.axaml.cs (29KB, contains zoom/pan logic)
- Magic numbers: min scale 0.1, max scale 50.0
- Some unused code paths (commented-out file open dialog in MainWindow)

### Testing Strategy
- Manual testing only
- Recommendation: backup folders before writing
- No automated regression tests

---

## Paint Type Options Management

The UI dynamically creates tool option stacks based on paint type:

**Location Paint Type**:
- Topography (AutoCompleteBox)
- Vegetation (AutoCompleteBox)
- Climate (AutoCompleteBox)
- Religion (AutoCompleteBox)
- Culture (AutoCompleteBox)
- Raw Material (AutoCompleteBox)

**Pop Paint Type**:
- Pop Type (AutoCompleteBox)
- Size (NumericUpDown)
- Culture (AutoCompleteBox)
- Religion (AutoCompleteBox)

Each autocomplete box is populated from the combined Cache (base + mod definitions).

---

## Future Development Path

From roadmap (README.md):
1. Ping system to display recently interacted provinces
2. Overlay for filtering provinces (using ping system)
3. Province creation and view (currently Province tool is stub)

---

## Quick Facts for AI Assistants

- **Entry Point**: Program.cs → BuildAvaloniaApp() → App.xaml.cs
- **State Hub**: MainWindowViewModel (all application state)
- **File I/O**: AppStorageService (read), ModFileWriterService (write)
- **Data Models**: ProvinceInfo, ProvinceLocation, ProvincePopInfo, PopDef
- **UI Platform**: Avalonia 11.3.6 (XAML-based, cross-platform)
- **Default Tool**: Select
- **Unsaved Changes**: Stored in MainWindowViewModel._paintedLocations
- **Crash Prevention**: Closing without WRITE = data lost (by design)

---

## Contact & Issues

From README:
- Report bugs on Discord or via GitHub Issues
- Include reproduction steps

---

*This CLAUDE.md was generated from comprehensive codebase analysis. Last updated: 2025-11-09*
