"""Generate the approved Puca Garden chibi player prototype (visual pass 3).

Run this file from Blender's Scripting workspace. The character is Z-up and
faces Blender's -Y axis. Feet rest on Z=0 and the body is centered on X=0.
The script deliberately clears the current scene before rebuilding the model.
"""

import bpy
import math


# -----------------------------------------------------------------------------
# Core proportions (Blender units)
# -----------------------------------------------------------------------------

HEAD_RADIUS = 0.52
HEAD_WIDTH_SCALE = 1.04
HEAD_DEPTH_SCALE = 0.91
HEAD_CHEEK_BULGE = 0.15
HEAD_FRONT_FLATTEN = 0.88

TORSO_HEIGHT = 0.58
TORSO_WIDTH = 0.64
TORSO_DEPTH = 0.44
SHOULDER_WIDTH = 0.72

ARM_LENGTH = 0.38
ARM_THICKNESS = 0.105
ARM_OUTWARD_OFFSET = 0.035
HAND_RADIUS = 0.115

LEG_LENGTH = 0.34
LEG_THICKNESS = 0.15
LEG_SPACING = 0.17

BOOT_SIZE = (0.36, 0.48, 0.25)  # X width, Y length, Z height
BOOT_SOLE_HEIGHT = 0.055
BOOT_FORWARD_OFFSET = 0.09

SCARF_MAJOR_RADIUS = 0.285
SCARF_THICKNESS = 0.075
SCARF_TAIL_SIZE = (0.18, 0.10, 0.34)

BELT_HEIGHT = 0.09
SATCHEL_SIZE = (0.30, 0.13, 0.32)

EYE_SIZE = (0.205, 0.045, 0.285)
IRIS_SIZE = (0.125, 0.028, 0.195)
EYE_HIGHLIGHT_SIZE = (0.048, 0.018, 0.065)
EYEBROW_SIZE = (0.16, 0.035, 0.035)

PART_OVERLAP = 0.05


# -----------------------------------------------------------------------------
# Flat placeholder colors
# -----------------------------------------------------------------------------

SKIN_COLOR = (0.84, 0.62, 0.47, 1.0)
HAIR_COLOR = (0.16, 0.075, 0.035, 1.0)
EYE_COLOR = (0.035, 0.045, 0.055, 1.0)
IRIS_COLOR = (0.11, 0.34, 0.38, 1.0)
HIGHLIGHT_COLOR = (0.92, 0.96, 0.88, 1.0)
SHIRT_COLOR = (0.78, 0.69, 0.51, 1.0)
SCARF_COLOR = (0.25, 0.39, 0.22, 1.0)
PANTS_COLOR = (0.17, 0.24, 0.15, 1.0)
BELT_COLOR = (0.30, 0.16, 0.075, 1.0)
BOOT_COLOR = (0.24, 0.115, 0.05, 1.0)
SATCHEL_COLOR = (0.36, 0.19, 0.08, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name.startswith("Player_"):
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


def create_rounded_box(name, location, dimensions, material, collection,
                       rotation=(0.0, 0.0, 0.0), bevel=0.06):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel_modifier = obj.modifiers.new("Soft_Rounded_Edges", "BEVEL")
    bevel_modifier.width = min(bevel, min(dimensions) * 0.4)
    bevel_modifier.segments = 3
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel_modifier.name)
    obj.data.materials.append(material)
    smooth_mesh(obj)
    return move_to_collection(obj, collection)


def create_tapered_lock(name, location, width, height, depth, material, collection,
                        rotation=(0.0, 0.0, 0.0), mirror=False):
    """Create a softly beveled, pointed hair lock instead of an oval blob."""
    direction = -1.0 if mirror else 1.0
    profile = [
        (-width * 0.50, height * 0.50),
        (width * 0.50, height * 0.50),
        (width * 0.42, height * 0.06),
        (width * 0.10 * direction, -height * 0.50),
        (-width * 0.28, -height * 0.20),
    ]
    vertices = []
    for y in (-depth * 0.5, depth * 0.5):
        vertices.extend((x, y, z) for x, z in profile)
    faces = [
        (0, 4, 3, 2, 1),
        (5, 6, 7, 8, 9),
        (0, 1, 6, 5),
        (1, 2, 7, 6),
        (2, 3, 8, 7),
        (3, 4, 9, 8),
        (4, 0, 5, 9),
    ]
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    obj.data.materials.append(material)
    bevel_modifier = obj.modifiers.new("Soft_Hair_Edges", "BEVEL")
    bevel_modifier.width = min(width, height, depth) * 0.18
    bevel_modifier.segments = 3
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel_modifier.name)
    smooth_mesh(obj)
    return obj


def create_tapered_limb(name, location, length, upper_radius, lower_radius,
                        material, collection, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=20,
        radius1=lower_radius,
        radius2=upper_radius,
        depth=length,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(material)
    bevel_modifier = obj.modifiers.new("Soft_Limb_Ends", "BEVEL")
    bevel_modifier.width = min(upper_radius, lower_radius) * 0.55
    bevel_modifier.segments = 3
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel_modifier.name)
    smooth_mesh(obj)
    return move_to_collection(obj, collection)


def create_boot(name, location, material, collection, mirror=False):
    """Create a beveled shoe with a broad toe, narrow heel, and flat sole."""
    width, length, height = BOOT_SIZE
    heel_width = width * 0.72
    toe_width = width
    y_front = -length * 0.56
    y_back = length * 0.44
    z_bottom = BOOT_SOLE_HEIGHT
    z_top = height
    vertices = [
        (-toe_width / 2, y_front, z_bottom),
        (toe_width / 2, y_front, z_bottom),
        (heel_width / 2, y_back, z_bottom),
        (-heel_width / 2, y_back, z_bottom),
        (-toe_width / 2, y_front, z_top * 0.72),
        (toe_width / 2, y_front, z_top * 0.72),
        (heel_width / 2, y_back, z_top),
        (-heel_width / 2, y_back, z_top),
    ]
    faces = [
        (0, 1, 2, 3), (4, 7, 6, 5),
        (0, 4, 5, 1), (1, 5, 6, 2),
        (2, 6, 7, 3), (3, 7, 4, 0),
    ]
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.location = location
    bevel_modifier = obj.modifiers.new("Rounded_Boot", "BEVEL")
    bevel_modifier.width = 0.055
    bevel_modifier.segments = 3
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel_modifier.name)
    obj.data.materials.append(material)
    smooth_mesh(obj)
    return obj


def create_smile(name, location, material, collection):
    curve = bpy.data.curves.new(f"{name}_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 0.012
    curve.bevel_resolution = 3
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(2)
    points = [(-0.055, 0.0, 0.012), (0.0, -0.006, -0.018), (0.055, 0.0, 0.012)]
    for point, coordinate in zip(spline.bezier_points, points):
        point.co = coordinate
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    collection.objects.link(obj)
    obj.location = location
    obj.data.materials.append(material)
    return obj


def create_torus(name, location, major_radius, minor_radius, material, collection,
                 scale=(1.0, 1.0, 1.0), rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=32,
        minor_segments=10,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    smooth_mesh(obj)
    return move_to_collection(obj, collection)


def create_chibi_head(name, location, material, collection):
    """Shape a UV sphere into a broad-cheeked, softly flattened chibi head."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=48,
        ring_count=24,
        radius=1.0,
        location=location,
    )
    obj = bpy.context.active_object
    obj.name = name

    for vertex in obj.data.vertices:
        x, y, z = vertex.co
        cheek_zone = math.exp(-((z + 0.18) / 0.48) ** 2)
        vertex.co.x = x * (1.0 + HEAD_CHEEK_BULGE * cheek_zone)
        if y < 0.0 and z < 0.30:
            vertex.co.y = y * HEAD_FRONT_FLATTEN
        if z < -0.72:
            vertex.co.x *= 0.88
            vertex.co.y *= 0.94

    obj.scale = (
        HEAD_RADIUS * HEAD_WIDTH_SCALE,
        HEAD_RADIUS * HEAD_DEPTH_SCALE,
        HEAD_RADIUS,
    )
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    smooth_mesh(obj)
    return move_to_collection(obj, collection)


def build_chibi_player():
    clear_scene()

    skin = create_material("MAT_Player_Skin", SKIN_COLOR)
    hair = create_material("MAT_Player_Hair", HAIR_COLOR)
    eyes = create_material("MAT_Player_Eyes", EYE_COLOR)
    irises = create_material("MAT_Player_Irises", IRIS_COLOR)
    highlights = create_material("MAT_Player_EyeHighlights", HIGHLIGHT_COLOR)
    shirt = create_material("MAT_Player_CreamShirt", SHIRT_COLOR)
    scarf = create_material("MAT_Player_GreenScarf", SCARF_COLOR)
    pants = create_material("MAT_Player_GreenPants", PANTS_COLOR)
    belt = create_material("MAT_Player_BrownBelt", BELT_COLOR)
    boots = create_material("MAT_Player_BrownBoots", BOOT_COLOR)
    satchel = create_material("MAT_Player_Satchel", SATCHEL_COLOR)

    root = bpy.data.collections.new("Player_Chibi_Blockout")
    body_collection = bpy.data.collections.new("Player_Body")
    face_hair_collection = bpy.data.collections.new("Player_Hair_and_Face")
    outfit_collection = bpy.data.collections.new("Player_Outfit")
    bpy.context.scene.collection.children.link(root)
    root.children.link(body_collection)
    root.children.link(face_hair_collection)
    root.children.link(outfit_collection)

    for side, x in (("L", -LEG_SPACING), ("R", LEG_SPACING)):
        create_boot(
            f"Player_Boot_{side}",
            (x, -BOOT_FORWARD_OFFSET, 0.0),
            boots,
            outfit_collection,
        )

    leg_bottom = BOOT_SIZE[2] - PART_OVERLAP
    leg_center_z = leg_bottom + LEG_LENGTH * 0.5
    for side, x in (("L", -LEG_SPACING), ("R", LEG_SPACING)):
        create_ellipsoid(
            f"Player_Leg_{side}",
            (x, 0.0, leg_center_z),
            (LEG_THICKNESS * 2.0, LEG_THICKNESS * 2.0, LEG_LENGTH),
            pants,
            body_collection,
        )

    torso_bottom = leg_bottom + LEG_LENGTH - PART_OVERLAP
    torso_center_z = torso_bottom + TORSO_HEIGHT * 0.5
    create_ellipsoid(
        "Player_Pants_Waist",
        (0.0, 0.015, torso_bottom + 0.06),
        (TORSO_WIDTH * 0.88, TORSO_DEPTH * 0.88, 0.22),
        pants,
        body_collection,
    )
    create_ellipsoid(
        "Player_Torso",
        (0.0, 0.0, torso_center_z),
        (TORSO_WIDTH, TORSO_DEPTH, TORSO_HEIGHT),
        shirt,
        body_collection,
    )
    create_ellipsoid(
        "Player_Shoulders",
        (0.0, 0.005, torso_center_z + TORSO_HEIGHT * 0.22),
        (SHOULDER_WIDTH, TORSO_DEPTH * 0.92, 0.25),
        shirt,
        body_collection,
    )

    arm_x = SHOULDER_WIDTH * 0.5 + ARM_OUTWARD_OFFSET
    arm_z = torso_center_z - 0.01
    for side, x, tilt in (("L", -arm_x, -0.12), ("R", arm_x, 0.12)):
        create_tapered_limb(
            f"Player_Arm_{side}",
            (x, 0.0, arm_z),
            ARM_LENGTH,
            ARM_THICKNESS,
            ARM_THICKNESS * 0.78,
            skin,
            body_collection,
            rotation=(0.0, 0.0, tilt),
        )
        hand_x = x + (-0.025 if side == "L" else 0.025)
        create_ellipsoid(
            f"Player_Hand_{side}",
            (hand_x, -0.01, arm_z - ARM_LENGTH * 0.46),
            (HAND_RADIUS * 1.8, HAND_RADIUS * 1.6, HAND_RADIUS * 1.8),
            skin,
            body_collection,
            segments=20,
            rings=10,
        )

    belt_z = torso_bottom + TORSO_HEIGHT * 0.24
    create_torus(
        "Player_Belt",
        (0.0, 0.0, belt_z),
        TORSO_WIDTH * 0.45,
        BELT_HEIGHT * 0.5,
        belt,
        outfit_collection,
        scale=(1.0, TORSO_DEPTH / TORSO_WIDTH, 1.0),
    )

    neck_z = torso_bottom + TORSO_HEIGHT - PART_OVERLAP * 0.5
    create_torus(
        "Player_Scarf_Wrap",
        (0.0, 0.0, neck_z),
        SCARF_MAJOR_RADIUS,
        SCARF_THICKNESS,
        scarf,
        outfit_collection,
        scale=(1.08, 0.82, 0.82),
    )
    create_rounded_box(
        "Player_Scarf_Tail",
        (0.20, -0.24, neck_z - 0.18),
        SCARF_TAIL_SIZE,
        scarf,
        outfit_collection,
        rotation=(0.10, -0.12, -0.18),
        bevel=0.045,
    )

    head_bottom = neck_z - PART_OVERLAP
    head_center_z = head_bottom + HEAD_RADIUS
    head = create_chibi_head(
        "Player_Head", (0.0, -0.025, head_center_z), skin, body_collection
    )

    # One broad rear/crown mass establishes a continuous hairstyle. Smaller
    # custom tapered meshes overlap it to create bangs, crown tufts, and sides.
    create_ellipsoid(
        "Player_Hair_BackCap",
        (0.0, 0.09, head_center_z + 0.10),
        (HEAD_RADIUS * 2.08, HEAD_RADIUS * 1.88, HEAD_RADIUS * 1.94),
        hair,
        face_hair_collection,
        segments=40,
        rings=20,
    )

    # Small ears are mostly hidden beneath the side hair.
    for side, x in (("L", -0.515), ("R", 0.515)):
        create_ellipsoid(
            f"Player_Ear_{side}",
            (x, -0.015, head_center_z - 0.035),
            (0.15, 0.09, 0.20),
            skin,
            face_hair_collection,
            segments=20,
            rings=10,
        )

    # name, x/y/z offset, width, height, depth, Z tilt, mirrored profile
    hair_locks = [
        ("Top_L", -0.22, -0.02, 0.46, 0.26, 0.34, 0.24, -0.34, False),
        ("Top_C", 0.00, -0.06, 0.50, 0.25, 0.38, 0.23, 0.04, False),
        ("Top_R", 0.23, -0.01, 0.45, 0.25, 0.32, 0.24, 0.32, True),
        ("Bang_L", -0.25, -0.425, 0.24, 0.23, 0.38, 0.12, -0.18, False),
        ("Bang_C", -0.02, -0.445, 0.27, 0.24, 0.40, 0.11, 0.03, True),
        ("Bang_R", 0.23, -0.425, 0.23, 0.22, 0.36, 0.12, 0.20, True),
        ("Temple_L", -0.48, -0.10, 0.03, 0.20, 0.43, 0.27, -0.12, False),
        ("Temple_R", 0.48, -0.09, 0.02, 0.20, 0.43, 0.27, 0.12, True),
        ("Back_L", -0.39, 0.23, -0.08, 0.25, 0.42, 0.30, -0.18, False),
        ("Back_R", 0.39, 0.23, -0.08, 0.25, 0.42, 0.30, 0.18, True),
    ]
    for lock_name, x, y, z, width, height, depth, tilt, mirror in hair_locks:
        point_upward = lock_name.startswith("Top")
        create_tapered_lock(
            f"Player_Hair_{lock_name}",
            (x, y, head_center_z + z),
            width,
            height,
            depth,
            hair,
            face_hair_collection,
            rotation=(math.pi if point_upward else 0.0, tilt * 0.18, tilt),
            mirror=mirror,
        )

    face_y = -0.455
    eye_z = head_center_z - 0.015
    for side, x in (("L", -0.19), ("R", 0.19)):
        create_ellipsoid(
            f"Player_Eye_{side}",
            (x, face_y, eye_z),
            EYE_SIZE,
            eyes,
            face_hair_collection,
            segments=24,
            rings=12,
        )
        create_ellipsoid(
            f"Player_Iris_{side}",
            (x, face_y - 0.027, eye_z - 0.012),
            IRIS_SIZE,
            irises,
            face_hair_collection,
            segments=20,
            rings=10,
        )
        highlight_x = x - 0.025
        create_ellipsoid(
            f"Player_EyeHighlight_{side}",
            (highlight_x, face_y - 0.045, eye_z + 0.055),
            EYE_HIGHLIGHT_SIZE,
            highlights,
            face_hair_collection,
            segments=16,
            rings=8,
        )
        create_rounded_box(
            f"Player_Eyebrow_{side}",
            (x, face_y - 0.006, eye_z + 0.185),
            EYEBROW_SIZE,
            hair,
            face_hair_collection,
            rotation=(0.04, 0.0, -0.05 if side == "L" else 0.05),
            bevel=0.015,
        )

    create_smile(
        "Player_Mouth",
        (0.0, face_y - 0.035, head_center_z - 0.225),
        eyes,
        face_hair_collection,
    )

    # Chunky cross-body satchel: one diagonal strap and one rounded hip bag.
    create_rounded_box(
        "Player_Satchel_Strap",
        (-0.03, -0.238, torso_center_z + 0.03),
        (0.075, 0.045, 0.72),
        belt,
        outfit_collection,
        rotation=(0.0, 0.0, -0.55),
        bevel=0.018,
    )
    create_rounded_box(
        "Player_Satchel_Bag",
        (0.39, -0.24, torso_center_z - 0.19),
        SATCHEL_SIZE,
        satchel,
        outfit_collection,
        rotation=(0.0, 0.0, -0.05),
        bevel=0.065,
    )

    # A single, grounded export root keeps the generated pieces together while
    # leaving every mesh independently editable in Blender.
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0.0, 0.0, 0.0))
    model_root = bpy.context.active_object
    model_root.name = "ChibiPlayerPrototype_Root"
    model_root.empty_display_size = 0.20
    move_to_collection(model_root, root)
    for collection in (body_collection, face_hair_collection, outfit_collection):
        for obj in collection.objects:
            # Preserve the approved geometry while placing the boot soles on
            # the export root's Z=0 ground plane.
            obj.location.z -= BOOT_SOLE_HEIGHT
            obj.parent = model_root

    bpy.ops.object.select_all(action="DESELECT")
    for collection in (body_collection, face_hair_collection, outfit_collection):
        for obj in collection.objects:
            obj.select_set(True)
    model_root.select_set(True)
    bpy.context.view_layer.objects.active = model_root

    overall_height = head_center_z + HEAD_RADIUS
    print(
        "Created Puca Garden chibi player pass 3: "
        f"{overall_height:.2f} units tall, facing -Y."
    )


if __name__ == "__main__":
    build_chibi_player()
