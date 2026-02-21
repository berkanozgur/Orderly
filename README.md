# Orderly — Folder Structure Module

## Installation

Drop the `Orderly/` folder into your project under `Assets/ ( or `Assets/Plugins/` because the default template of Orderly has "Plugins" folder).

## Setup

1. Open **Tools > Orderly**
2. Click **Create New Template** and choose a save location
3. Define your folders in the **Folders** tab
4. Click **Generate Folders**
5. Map asset types to folders in the **Rules** tab
6. Create assets in the right place from the **Create** tab

---

## Folders Tab

Build your folder tree and assign a color to each folder. Colors appear as tinted labels in the Project window and persist across sessions via `EditorPrefs`.

- **+ Add Root Folder** adds a top-level folder under `Assets/`
- **+** on any row adds a child subfolder
- **X** removes the folder from the template (never deletes from disk)
- **Generate Folders** creates missing folders, skips existing ones, applies colors
- **Reapply Colors** re-paints colors without creating anything
- **Clear Colors** removes all Orderly color overlays

<img width="856" height="818" alt="image" src="https://github.com/user-attachments/assets/5d4b0306-4985-4c2e-89ba-0cd3f0d8b905" />

## Rules Tab

Maps creatable asset types to folders defined in your template. The dropdown updates automatically if you rename folders. Setting a rule to None disables the Create button for that type.

Supported types: MonoBehaviour, Material, Animator Controller, Animation Clip, VFX Graph*, Shader Graph*

*Requires the relevant Unity package to be installed.

<img width="672" height="723" alt="image" src="https://github.com/user-attachments/assets/02e60926-5129-4917-a578-6cb58e2e20c9" />

## Create Tab

Click **Create** on any type with a rule set. A subfolder picker lets you choose exactly where inside the mapped folder the asset lands.

<img width="544" height="833" alt="image" src="https://github.com/user-attachments/assets/9b609f0a-e0a9-420a-84af-0dcd7feb33aa" />

---

## Templates

Templates are ScriptableObjects you can commit to version control. The whole team gets the same structure and rules. You can maintain multiple templates and switch between them in the window header.



## Default Template

The default template includes: `Scripts`, `Prefabs`, `Scenes`, `Textures`, `Materials`, `Audio`, `Animations`, `Models`, `Plugins`, each with a distinct preset color.

## Author

Berkan Özgür - 2026
