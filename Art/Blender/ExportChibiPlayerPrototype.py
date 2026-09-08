"""Regenerate and export the complete chibi player prototype as a Godot GLB."""

from pathlib import Path
import runpy

import bpy


SCRIPT_DIRECTORY = Path(__file__).resolve().parent
PROJECT_DIRECTORY = SCRIPT_DIRECTORY.parents[1]
GENERATOR_SCRIPT = SCRIPT_DIRECTORY / "GenerateChibiPlayerBlockout.py"
EXPORT_PATH = PROJECT_DIRECTORY / "Art" / "Models" / "Player" / "ChibiPlayerPrototype.glb"


def export_chibi_player():
    runpy.run_path(str(GENERATOR_SCRIPT), run_name="__main__")
    EXPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    # The generated scene contains only the player. Exporting the whole scene
    # prevents selection state from silently omitting a character piece.
    bpy.ops.export_scene.gltf(
        filepath=str(EXPORT_PATH),
        export_format="GLB",
        use_selection=False,
        export_yup=True,
    )
    print(f"Exported complete chibi player prototype to: {EXPORT_PATH}")


if __name__ == "__main__":
    export_chibi_player()
