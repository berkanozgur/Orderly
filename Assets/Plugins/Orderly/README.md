# Orderly — Folder Structure Module

## Installation

Drop the `Orderly/` folder into your project under `Assets/ ( or `Assets/Plugins/` because the default template of Orderly has "Plugins" folder).

## Usage

### 1. Open the Window
`Tools > Orderly`

### 2. Create a Template
Click **Create New Template** and choose a save location. The template is a ScriptableObject (`.asset`) you can commit to version control and share across your team.

### 3. Define Your Folder Structure
- **+ Add Root Folder** — adds a top-level folder under `Assets/`
- **+** (per row) — adds a child/sub-folder
- **Color swatch** — pick a color; it appears as a tinted label in the Project window
- **✕** — removes the folder from the template (does NOT delete it from disk)

### 4. Generate
Click **⚡ Generate Folders**. Orderly will:
- Create any folders that don't exist yet
- Skip folders that already exist (never overwrites or deletes)
- Apply the color to every folder (existing or newly created)

### 5. Reapply Colors
If you reopen the project and colors are gone, click **🎨 Reapply Colors**.  
Colors are also stored in `EditorPrefs` and reload automatically on domain reload.


## Default Template

The default template includes: `Scripts`, `Prefabs`, `Scenes`, `Textures`, `Materials`, `Audio`, `Animations`, `Models`, `Plugins`, — each with a distinct preset color.
