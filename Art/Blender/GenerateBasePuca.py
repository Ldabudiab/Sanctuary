"""Generate the first 3D Base Puca prototype for Puca Garden.

Run from Blender's Scripting workspace. The model is Z-up, faces Blender -Y,
is centered on X=0, and places the feet on Z=0. The current scene is cleared
before generation. This is an unrigged visual blockout with flat materials.
"""

import bpy
import math


# -----------------------------------------------------------------------------
# Editable proportions (Blender units)
# -----------------------------------------------------------------------------

HEAD_WIDTH = 1.02
HEAD_DEPTH = 0.82
HEAD_HEIGHT = 1.16
HEAD_CENTER_Z = 0.93
HEAD_TAPER = 0.34
HEAD_UPPER_FULLNESS = 0.12
HEAD_FRONT_FLATTEN = 0.91

BODY_SIZE = (0.48, 0.38, 0.46)
BODY_CENTER_Z = 0.39

ARM_LENGTH = 0.25
ARM_THICKNESS = 0.075
ARM_SIDE_OFFSET = 0.27
HAND_SIZE = (0.20, 0.17, 0.18)

LEG_SIZE = (0.14, 0.14, 0.20)
LEG_SIDE_OFFSET = 0.14
FOOT_SIZE = (0.28, 0.32, 0.20)
FOOT_SIDE_OFFSET = 0.19
FOOT_FORWARD_OFFSET = 0.07

EYE_SIZE = (0.205, 0.040, 0.315)
IRIS_SIZE = (0.125, 0.025, 0.225)
EYE_HIGHLIGHT_SIZE = (0.052, 0.014, 0.072)
EYE_HORIZONTAL_OFFSET = 0.205
EYE_HEIGHT = 0.98
FACE_FRONT_Y = -0.395

MOUTH_WIDTH = 0.085
MOUTH_HEIGHT = 0.025

ORB_SIZE = 0.25
ORB_HEIGHT = 1.78


# -----------------------------------------------------------------------------
# Soft placeholder colors
# -----------------------------------------------------------------------------

CREAM_COLOR = (0.90, 0.84, 0.67, 1.0)
GREEN_COLOR = (0.35, 0.58, 0.27, 1.0)
EYE_DARK_COLOR = (0.025, 0.105, 0.12, 1.0)
EYE_TEAL_COLOR = (0.05, 0.44, 0.50, 1.0)
EYE_HIGHLIGHT_COLOR = (0.91, 0.98, 0.91, 1.0)
MOUTH_COLOR = (0.18, 0.10, 0.08, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name == "Base_Puca":
            bpy.data.collections.remove(collection)


def create_material(name, color):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    material.diffuse_color = color
    material.roughness = 0.88
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Roughness"].default_value = 0.88
    return material


def move_to_collection(obj, collection):
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)
    return obj


def smooth_mesh(obj):
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True


def create_ellipsoid(name, location, dimensions, material, collection,
                     rotation=(0.0, 0.0, 0.0), segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=1.0,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = tuple(dimension * 0.5 for dimension in dimensions)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    smooth_mesh(obj)
    return move_to_collection(obj, collection)


def create_teardrop_head(material, collection):
    """Shape one UV sphere into a cohesive, rounded inverted droplet."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=48,
        ring_count=24,
        radius=1.0,
        location=(0.0, 0.0, HEAD_CENTER_Z),
    )
    obj = bpy.context.active_object
    obj.name = "Puca_Head"

    for vertex in obj.data.vertices:
        x, y, z = vertex.co
        normalized_height = (z + 1.0) * 0.5
        lower_taper = 1.0 - HEAD_TAPER * (1.0 - normalized_height) ** 1.7
        upper_fullness = 1.0 + HEAD_UPPER_FULLNESS * math.sin(
            math.pi * normalized_height
        )
        width_factor = lower_taper * upper_fullness
        vertex.co.x = x * width_factor
        vertex.co.y = y * (0.96 + 0.04 * normalized_height)
        if y < 0.0 and z < 0.45:
            vertex.co.y *= HEAD_FRONT_FLATTEN

    obj.scale = (HEAD_WIDTH * 0.5, HEAD_DEPTH * 0.5, HEAD_HEIGHT * 0.5)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    smooth_mesh(obj)
    return move_to_collection(obj, collection)


def create_tapered_limb(name, location, length, radius, material, collection,
                        rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=20,
        radius1=radius * 0.78,
        radius2=radius,
        depth=length,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.active_object
    obj.name = name
    bevel = obj.modifiers.new("Soft_Limb_Ends", "BEVEL")
    bevel.width = radius * 0.55
    bevel.segments = 3
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    obj.data.materials.append(material)
    smooth_mesh(obj)
    return move_to_collection(obj, collection)


def create_mouth(material, collection):
    curve = bpy.data.curves.new("Puca_Mouth_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 0.009
    curve.bevel_resolution = 3
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(2)
    points = [
        (-MOUTH_WIDTH * 0.5, 0.0, MOUTH_HEIGHT * 0.4),
        (0.0, -0.004, -MOUTH_HEIGHT * 0.5),
        (MOUTH_WIDTH * 0.5, 0.0, MOUTH_HEIGHT * 0.4),
    ]
    for point, coordinate in zip(spline.bezier_points, points):
        point.co = coordinate
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new("Puca_Mouth", curve)
    collection.objects.link(obj)
    obj.location = (0.0, FACE_FRONT_Y - 0.025, EYE_HEIGHT - 0.24)
    obj.data.materials.append(material)
    return obj


def build_base_puca():
    clear_scene()

    cream = create_material("Puca_Cream", CREAM_COLOR)
    green = create_material("Puca_Green", GREEN_COLOR)
    eye_dark = create_material("Puca_EyeDark", EYE_DARK_COLOR)
    eye_teal = create_material("Puca_EyeTeal", EYE_TEAL_COLOR)
    eye_highlight = create_material("Puca_EyeHighlight", EYE_HIGHLIGHT_COLOR)
    mouth = create_material("Puca_Mouth", MOUTH_COLOR)

    collection = bpy.data.collections.new("Base_Puca")
    bpy.context.scene.collection.children.link(collection)

    create_teardrop_head(cream, collection)
    create_ellipsoid(
        "Puca_Body", (0.0, 0.035, BODY_CENTER_Z), BODY_SIZE,
        cream, collection, segments=36, rings=18,
    )

    for side, x, tilt in (
        ("L", -ARM_SIDE_OFFSET, -0.28),
        ("R", ARM_SIDE_OFFSET, 0.28),
    ):
        create_tapered_limb(
            f"Puca_Arm_{side}", (x, -0.015, 0.43), ARM_LENGTH,
            ARM_THICKNESS, cream, collection, rotation=(0.0, 0.0, tilt),
        )
        hand_x = x + (-0.035 if side == "L" else 0.035)
        create_ellipsoid(
            f"Puca_Hand_{side}", (hand_x, -0.035, 0.30), HAND_SIZE,
            green, collection, segments=24, rings=12,
        )

    for side, x in (("L", -LEG_SIDE_OFFSET), ("R", LEG_SIDE_OFFSET)):
        create_ellipsoid(
            f"Puca_Leg_{side}", (x, 0.015, 0.18), LEG_SIZE,
            cream, collection, segments=20, rings=10,
        )

    for side, x in (("L", -FOOT_SIDE_OFFSET), ("R", FOOT_SIDE_OFFSET)):
        create_ellipsoid(
            f"Puca_Foot_{side}", (x, -FOOT_FORWARD_OFFSET, FOOT_SIZE[2] * 0.5),
            FOOT_SIZE, green, collection, segments=28, rings=14,
        )

    for side, x in (("L", -EYE_HORIZONTAL_OFFSET), ("R", EYE_HORIZONTAL_OFFSET)):
        create_ellipsoid(
            f"Puca_Eye_{side}", (x, FACE_FRONT_Y, EYE_HEIGHT), EYE_SIZE,
            eye_dark, collection, segments=28, rings=14,
        )
        create_ellipsoid(
            f"Puca_EyeTeal_{side}",
            (x, FACE_FRONT_Y - 0.026, EYE_HEIGHT - 0.025),
            IRIS_SIZE, eye_teal, collection, segments=24, rings=12,
        )
        create_ellipsoid(
            f"Puca_EyeHighlight_{side}",
            (x - 0.026, FACE_FRONT_Y - 0.043, EYE_HEIGHT + 0.070),
            EYE_HIGHLIGHT_SIZE, eye_highlight, collection, segments=16, rings=8,
        )

    create_mouth(mouth, collection)

    # The orb is deliberately a separate object with clear air beneath it so a
    # future evolved-form topper can replace it without changing the body mesh.
    create_ellipsoid(
        "Puca_Orb", (0.0, 0.0, ORB_HEIGHT),
        (ORB_SIZE, ORB_SIZE, ORB_SIZE), green, collection,
        segments=32, rings=16,
    )

    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0.0, 0.0, 0.0))
    root = bpy.context.active_object
    root.name = "BasePuca_Root"
    root.empty_display_size = 0.16
    move_to_collection(root, collection)
    for obj in list(collection.objects):
        if obj != root:
            obj.parent = root

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    bpy.context.view_layer.objects.active = root

    total_height = ORB_HEIGHT + ORB_SIZE * 0.5
    print(
        f"Created Base Puca: {total_height:.2f} units tall, "
        "feet grounded at Z=0, facing -Y."
    )


if __name__ == "__main__":
    build_base_puca()
