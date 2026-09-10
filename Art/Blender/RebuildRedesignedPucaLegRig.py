"""Rebuild the redesigned Puca's two leg/foot rigs from first principles.

This deliberately discards all Meshy leg/foot weights. It preserves the mesh,
materials, upper-body rig, and visible rest shape. Run first with SAVE_RESULT
False to produce validation renders, inspect them, then set it True to save.
"""

import bpy
import colorsys
import math
from collections import defaultdict, deque
from mathutils import Quaternion, Vector


ARMATURE_NAME = "UniRigArmature"
MESH_NAME = "output_unwrapped"
COLOR_IMAGE_NAME = "texture_0"
ACTION_NAME = "Puca_LegRigTest"

LEFT_LEG = "Bone_003"
LEFT_FOOT = "Bone_002"
RIGHT_LEG = "Bone_005"
RIGHT_FOOT = "Bone_004"
LIMB_BONES = (LEFT_LEG, LEFT_FOOT, RIGHT_LEG, RIGHT_FOOT)
BODY_BONE = "Bone_001"

FORWARD = Vector((0.0, -1.0, 0.0))
LATERAL = Vector((1.0, 0.0, 0.0))

# Complete connected islands matching these rest-space/color constraints form
# each rounded green foot. Selection does not consult old bone weights.
FOOT_MAX_COMPONENT_Z = 0.18
FOOT_MAX_COMPONENT_MIN_Z = 0.13
FOOT_MIN_GREEN_FRACTION = 0.20
FOOT_MIN_ABS_CENTER_X = 0.012

# Explicit short-leg envelope and controlled hip transition.
LEG_CORE_RADIUS = 0.105
LEG_OUTER_RADIUS = 0.135
LEG_MIN_Z = 0.075
LEG_MAX_Z = 0.245
LEG_MAX_ABS_Y = 0.17
HIP_FULL_LEG_Z = 0.155
HIP_ZERO_LEG_Z = 0.225

# One-frame diagnostic pose. These values are intentionally unchanged from the
# approved positional test; the rebuilt pivots make the motion mechanically clean.
TEST_LEG_SWING_DEGREES = 68.0
TEST_FOOT_PITCH_DEGREES = 52.0

VALIDATION_DIR = "C:/Dev/SanctuaryGame/sanctuary-game/Art/Models/Puca/Source/LegRigValidation"
SAVE_RESULT = True


def require_objects():
    armature = bpy.data.objects.get(ARMATURE_NAME)
    mesh_object = bpy.data.objects.get(MESH_NAME)
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError(f"Missing armature {ARMATURE_NAME!r}")
    if mesh_object is None or mesh_object.type != "MESH":
        raise RuntimeError(f"Missing mesh {MESH_NAME!r}")
    modifier = next((item for item in mesh_object.modifiers if item.type == "ARMATURE"), None)
    if modifier is None or modifier.object != armature:
        raise RuntimeError("output_unwrapped is not skinned to UniRigArmature")
    return armature, mesh_object


def sampled_vertex_green(mesh):
    image = bpy.data.images.get(COLOR_IMAGE_NAME)
    if image is None:
        raise RuntimeError(f"Missing baked image {COLOR_IMAGE_NAME!r}")
    uv_layer = mesh.uv_layers.active
    if uv_layer is None:
        raise RuntimeError("Mesh has no active UV layer")
    width, height = image.size
    pixels = list(image.pixels)
    samples = defaultdict(list)
    for polygon in mesh.polygons:
        for loop_index in polygon.loop_indices:
            vertex_index = mesh.loops[loop_index].vertex_index
            uv = uv_layer.data[loop_index].uv
            x = min(width - 1, max(0, int((uv.x % 1.0) * width)))
            y = min(height - 1, max(0, int((uv.y % 1.0) * height)))
            offset = (y * width + x) * 4
            rgb = tuple(pixels[offset:offset + 3])
            hue, saturation, _value = colorsys.rgb_to_hsv(*rgb)
            samples[vertex_index].append(0.18 <= hue <= 0.48 and saturation >= 0.18)
    return [sum(samples[index]) / max(1, len(samples[index])) for index in range(len(mesh.vertices))]


def connected_components(mesh):
    adjacency = [[] for _ in mesh.vertices]
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].append(b)
        adjacency[b].append(a)
    remaining = set(range(len(mesh.vertices)))
    result = []
    while remaining:
        seed = remaining.pop()
        queue = deque([seed])
        component = [seed]
        while queue:
            current = queue.popleft()
            for neighbor in adjacency[current]:
                if neighbor in remaining:
                    remaining.remove(neighbor)
                    queue.append(neighbor)
                    component.append(neighbor)
        result.append(component)
    return result


def discover_feet(mesh, green_values):
    left, right = set(), set()
    for component in connected_components(mesh):
        points = [mesh.vertices[index].co for index in component]
        min_z = min(point.z for point in points)
        max_z = max(point.z for point in points)
        center_x = sum(point.x for point in points) / len(points)
        green_fraction = sum(green_values[index] for index in component) / len(component)
        if (min_z <= FOOT_MAX_COMPONENT_MIN_Z
                and max_z <= FOOT_MAX_COMPONENT_Z
                and green_fraction >= FOOT_MIN_GREEN_FRACTION):
            if center_x >= FOOT_MIN_ABS_CENTER_X:
                left.update(component)
            elif center_x <= -FOOT_MIN_ABS_CENTER_X:
                right.update(component)
    if len(left) < 400 or len(right) < 400:
        raise RuntimeError(f"Unsafe foot discovery result: left={len(left)}, right={len(right)}")
    if left & right:
        raise RuntimeError("Left/right foot discovery overlapped")
    return left, right


def bounds(mesh, indices):
    points = [mesh.vertices[index].co for index in indices]
    return (
        Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points))),
        Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points))),
    )


def rebuild_rest_bones(armature, mesh, left_foot, right_foot):
    left_min, left_max = bounds(mesh, left_foot)
    right_min, right_max = bounds(mesh, right_foot)

    def landmarks(minimum, maximum):
        center_x = (minimum.x + maximum.x) * 0.5
        depth = maximum.y - minimum.y
        ankle = Vector((center_x, maximum.y - depth * 0.24, maximum.z - 0.018))
        hip = Vector((center_x, 0.018, 0.225))
        toe = Vector((center_x, minimum.y + depth * 0.20, minimum.z + 0.055))
        return hip, ankle, toe

    left_hip, left_ankle, left_toe = landmarks(left_min, left_max)
    right_hip, right_ankle, right_toe = landmarks(right_min, right_max)

    previous_mode = armature.mode
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    edit = armature.data.edit_bones
    body = edit[BODY_BONE]
    for leg_name, foot_name, hip, ankle, toe in (
        (LEFT_LEG, LEFT_FOOT, left_hip, left_ankle, left_toe),
        (RIGHT_LEG, RIGHT_FOOT, right_hip, right_ankle, right_toe),
    ):
        leg = edit[leg_name]
        foot = edit[foot_name]
        leg.parent = body
        leg.use_connect = False
        leg.head = hip
        leg.tail = ankle
        foot.parent = leg
        foot.use_connect = True
        foot.head = ankle
        foot.tail = toe
        leg.roll = 0.0
        foot.roll = 0.0
    bpy.ops.object.mode_set(mode="OBJECT")
    if previous_mode == "POSE":
        bpy.ops.object.mode_set(mode="POSE")
    print("Rebuilt rest pivots:")
    print(f"  left hip={tuple(round(v, 4) for v in left_hip)} ankle={tuple(round(v, 4) for v in left_ankle)}")
    print(f"  right hip={tuple(round(v, 4) for v in right_hip)} ankle={tuple(round(v, 4) for v in right_ankle)}")
    return (left_hip, left_ankle), (right_hip, right_ankle)


def remove_limb_weights(mesh_object, vertex_index):
    for name in LIMB_BONES:
        group = mesh_object.vertex_groups.get(name)
        if group is not None:
            try:
                group.remove([vertex_index])
            except RuntimeError:
                pass


def normalized_non_limb_weights(mesh_object, vertex_index):
    vertex = mesh_object.data.vertices[vertex_index]
    names = {group.index: group.name for group in mesh_object.vertex_groups}
    values = []
    for membership in vertex.groups:
        if names[membership.group] not in LIMB_BONES:
            values.append((membership.group, membership.weight))
    total = sum(weight for _index, weight in values)
    if total <= 1.0e-8:
        return {BODY_BONE: 1.0}
    return {names[index]: weight / total for index, weight in values}


def replace_weights(mesh_object, vertex_index, assignments):
    for group in mesh_object.vertex_groups:
        try:
            group.remove([vertex_index])
        except RuntimeError:
            pass
    for name, weight in assignments.items():
        if weight > 1.0e-8:
            mesh_object.vertex_groups[name].add([vertex_index], weight, "REPLACE")


def distance_to_segment(point, start, end):
    direction = end - start
    factor = max(0.0, min(1.0, (point - start).dot(direction) / direction.length_squared))
    return (point - (start + direction * factor)).length


def smoothstep(value):
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def rebuild_weights(mesh_object, left_foot, right_foot, left_chain, right_chain):
    mesh = mesh_object.data
    left_leg_count = right_leg_count = stable_count = 0
    left_hip, left_ankle = left_chain
    right_hip, right_ankle = right_chain

    # Snapshot the valid upper/body weighting before globally deleting all old
    # Meshy leg/foot influences.
    stable_weights = [normalized_non_limb_weights(mesh_object, index) for index in range(len(mesh.vertices))]
    for index in range(len(mesh.vertices)):
        remove_limb_weights(mesh_object, index)

    for vertex in mesh.vertices:
        index = vertex.index
        if index in left_foot:
            replace_weights(mesh_object, index, {LEFT_FOOT: 1.0})
            continue
        if index in right_foot:
            replace_weights(mesh_object, index, {RIGHT_FOOT: 1.0})
            continue

        side = None
        chain = None
        leg_name = None
        if vertex.co.x > 0.035:
            side, chain, leg_name = "left", (left_hip, left_ankle), LEFT_LEG
        elif vertex.co.x < -0.035:
            side, chain, leg_name = "right", (right_hip, right_ankle), RIGHT_LEG

        leg_weight = 0.0
        if (side is not None and LEG_MIN_Z <= vertex.co.z <= LEG_MAX_Z
                and abs(vertex.co.y) <= LEG_MAX_ABS_Y):
            distance = distance_to_segment(vertex.co, chain[0], chain[1])
            radial = 1.0 - smoothstep((distance - LEG_CORE_RADIUS) / (LEG_OUTER_RADIUS - LEG_CORE_RADIUS))
            hip_fade = 1.0 - smoothstep(
                (vertex.co.z - HIP_FULL_LEG_Z) / (HIP_ZERO_LEG_Z - HIP_FULL_LEG_Z)
            )
            leg_weight = radial * hip_fade

        base = stable_weights[index]
        if leg_weight > 0.02:
            assignments = {name: weight * (1.0 - leg_weight) for name, weight in base.items()}
            assignments[leg_name] = leg_weight
            replace_weights(mesh_object, index, assignments)
            if side == "left":
                left_leg_count += 1
            else:
                right_leg_count += 1
        else:
            replace_weights(mesh_object, index, base)
            stable_count += 1

    print("New weight structure:")
    print(f"  left foot:  {len(left_foot)} vertices at 1.0 {LEFT_FOOT}")
    print(f"  right foot: {len(right_foot)} vertices at 1.0 {RIGHT_FOOT}")
    print(f"  left leg transition vertices:  {left_leg_count}")
    print(f"  right leg transition vertices: {right_leg_count}")
    print(f"  stable non-leg vertices: {stable_count}")


def armature_axis_to_bone_axis(pose_bone, axis):
    return (pose_bone.bone.matrix_local.to_3x3().inverted() @ axis).normalized()


def create_test_action(armature):
    if armature.animation_data:
        armature.animation_data.action = None
    old = bpy.data.actions.get(ACTION_NAME)
    if old is not None:
        bpy.data.actions.remove(old, do_unlink=True)
    for pose_bone in armature.pose.bones:
        pose_bone.matrix_basis.identity()

    action = bpy.data.actions.new(ACTION_NAME)
    armature.animation_data_create()
    armature.animation_data.action = action
    leg = armature.pose.bones[LEFT_LEG]
    foot = armature.pose.bones[LEFT_FOOT]
    leg.rotation_mode = "QUATERNION"
    foot.rotation_mode = "QUATERNION"
    leg.rotation_quaternion = Quaternion(
        armature_axis_to_bone_axis(leg, LATERAL), math.radians(-TEST_LEG_SWING_DEGREES)
    )
    foot.rotation_quaternion = Quaternion(
        armature_axis_to_bone_axis(foot, LATERAL), math.radians(TEST_FOOT_PITCH_DEGREES)
    )
    leg.keyframe_insert("rotation_quaternion", frame=1, group=LEFT_LEG)
    foot.keyframe_insert("rotation_quaternion", frame=1, group=LEFT_FOOT)
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 1
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    print(f"Recreated Action: {ACTION_NAME}")


def validate_weights(mesh_object, left_foot, right_foot):
    group_names = {group.index: group.name for group in mesh_object.vertex_groups}
    for label, indices, expected in (
        ("left", left_foot, LEFT_FOOT), ("right", right_foot, RIGHT_FOOT)
    ):
        for index in indices:
            assignments = {
                group_names[item.group]: item.weight
                for item in mesh_object.data.vertices[index].groups
            }
            if set(assignments) != {expected} or abs(assignments[expected] - 1.0) > 1.0e-6:
                raise RuntimeError(f"{label} foot vertex {index} is not rigidly assigned to {expected}")

    evaluated = mesh_object.evaluated_get(bpy.context.evaluated_depsgraph_get())
    left_points = [evaluated.data.vertices[index].co for index in left_foot]
    right_points = [evaluated.data.vertices[index].co for index in right_foot]
    left_min_z = min(point.z for point in left_points)
    right_min_z = min(point.z for point in right_points)
    if left_min_z <= 0.002:
        raise RuntimeError(f"Lifted foot did not clear the floor: min Z={left_min_z:.5f}")
    if abs(right_min_z) > 0.005:
        raise RuntimeError(f"Planted foot moved off the floor: min Z={right_min_z:.5f}")
    print(f"Pose bounds: lifted left min Z={left_min_z:.4f}, planted right min Z={right_min_z:.4f}")


def render_validation_views():
    import os
    os.makedirs(VALIDATION_DIR, exist_ok=True)
    scene = bpy.context.scene
    scene.camera = None
    for existing in list(bpy.data.objects):
        if existing.name.startswith("LegRigValidation"):
            bpy.data.objects.remove(existing, do_unlink=True)
    camera_data = bpy.data.cameras.new("LegRigValidationCamera")
    camera = bpy.data.objects.new("LegRigValidationCamera", camera_data)
    scene.collection.objects.link(camera)
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 2.15
    scene.camera = camera
    light_data = bpy.data.lights.new("LegRigValidationLight", "AREA")
    light_data.energy = 900
    light_data.size = 4.0
    light = bpy.data.objects.new("LegRigValidationLight", light_data)
    scene.collection.objects.link(light)
    light.location = (-2.0, -3.0, 4.0)
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 600
    scene.render.resolution_y = 600
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    target = Vector((0.0, 0.0, 0.65))
    views = {
        "front": Vector((0.0, -4.5, 0.85)),
        "side": Vector((4.5, 0.0, 0.85)),
        "underside": Vector((0.0, -3.5, -1.3)),
    }
    for name, position in views.items():
        camera.location = position
        camera.rotation_euler = (target - position).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = f"{VALIDATION_DIR}/{name}.png"
        bpy.ops.render.render(write_still=True)
        print(f"Rendered {name}: {scene.render.filepath}")
    # Validation helpers are intentionally not part of the repaired asset.
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.objects.remove(light, do_unlink=True)
    bpy.data.cameras.remove(camera_data)
    bpy.data.lights.remove(light_data)


def main():
    armature, mesh_object = require_objects()
    green = sampled_vertex_green(mesh_object.data)
    left_foot, right_foot = discover_feet(mesh_object.data, green)
    left_chain, right_chain = rebuild_rest_bones(
        armature, mesh_object.data, left_foot, right_foot
    )
    rebuild_weights(mesh_object, left_foot, right_foot, left_chain, right_chain)
    create_test_action(armature)
    validate_weights(mesh_object, left_foot, right_foot)
    render_validation_views()
    if SAVE_RESULT:
        # Preserve the user's existing .blend1 exactly as requested.
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
        print(f"Saved rebuilt rig: {bpy.data.filepath}")
    else:
        print("DRY RUN ONLY: validation rendered; main .blend was not saved")


if __name__ == "__main__":
    main()
