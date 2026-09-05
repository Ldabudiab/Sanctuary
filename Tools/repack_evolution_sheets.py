from collections import deque
from pathlib import Path
from PIL import Image

FRAME_SIZE = 160
# Explicit source rectangles measured from the approved presentation sheet.
# Repeated rectangles intentionally fill the existing six-frame runtime sequences
# where the source provides fewer clean character poses.
ANIMATIONS = {
    "idle": [(49, 18, 121, 152), (156, 18, 228, 152), (257, 19, 328, 152), (363, 19, 434, 152)],
    "walk": [(38, 174, 117, 306), (141, 176, 216, 308), (236, 177, 312, 306), (334, 175, 408, 308), (432, 176, 501, 306), (522, 176, 595, 307)],
    "run": [(45, 331, 129, 443), (172, 332, 261, 446), (321, 332, 408, 445), (466, 334, 557, 442), (609, 334, 728, 442), (753, 331, 869, 443)],
    "sleep": [(1127, 492, 1219, 590), (1229, 492, 1315, 590), (1323, 493, 1409, 591), (1417, 498, 1502, 591), (1323, 493, 1409, 591), (1229, 492, 1315, 590)],
    "happy": [(39, 468, 107, 594), (130, 468, 202, 594), (39, 468, 107, 594), (130, 468, 202, 594)],
    "eat": [(47, 607, 110, 730), (149, 607, 214, 728), (248, 607, 314, 727), (352, 607, 418, 728), (463, 607, 531, 734), (571, 607, 636, 734)],
    "attack": [(65, 736, 134, 851), (173, 739, 242, 852), (285, 742, 402, 852), (435, 742, 539, 853), (572, 744, 650, 852), (435, 742, 539, 853)],
    "hurt": [(42, 869, 112, 996), (143, 869, 215, 995), (262, 870, 350, 996), (383, 869, 476, 996)],
}


def color_distance(a, b):
    return sum((int(a[i]) - int(b[i])) ** 2 for i in range(3)) ** 0.5


def remove_connected_background(image):
    rgb = image.convert("RGB")
    width, height = rgb.size
    pixels = rgb.load()
    background = bytearray(width * height)
    queue = deque()

    def add(x, y):
        index = y * width + x
        if not background[index]:
            background[index] = 1
            queue.append((x, y))

    for x in range(width):
        add(x, 0)
        add(x, height - 1)
    for y in range(height):
        add(0, y)
        add(width - 1, y)

    while queue:
        x, y = queue.popleft()
        current = pixels[x, y]
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if nx < 0 or ny < 0 or nx >= width or ny >= height:
                continue
            index = ny * width + nx
            if background[index]:
                continue
            if color_distance(current, pixels[nx, ny]) <= 6.0:
                background[index] = 1
                queue.append((nx, ny))

    result = image.convert("RGBA")
    output = result.load()
    for y in range(height):
        for x in range(width):
            if background[y * width + x]:
                output[x, y] = (0, 0, 0, 0)
    return result


def remove_border_fragments(image):
    width, height = image.size
    pixels = image.load()
    visited = bytearray(width * height)
    for start_y in range(height):
        for start_x in range(width):
            start_index = start_y * width + start_x
            if visited[start_index] or pixels[start_x, start_y][3] == 0:
                continue
            queue = deque([(start_x, start_y)])
            visited[start_index] = 1
            component = []
            touches_border = False
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                touches_border |= x == 0 or y == 0 or x == width - 1 or y == height - 1
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        continue
                    index = ny * width + nx
                    if visited[index] or pixels[nx, ny][3] == 0:
                        continue
                    visited[index] = 1
                    queue.append((nx, ny))
            if touches_border:
                for x, y in component:
                    pixels[x, y] = (0, 0, 0, 0)
    return image


def remove_distant_components(image):
    width, height = image.size
    pixels = image.load()
    visited = bytearray(width * height)
    components = []
    for start_y in range(height):
        for start_x in range(width):
            start_index = start_y * width + start_x
            if visited[start_index] or pixels[start_x, start_y][3] == 0:
                continue
            queue = deque([(start_x, start_y)])
            visited[start_index] = 1
            component = []
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        continue
                    index = ny * width + nx
                    if visited[index] or pixels[nx, ny][3] == 0:
                        continue
                    visited[index] = 1
                    queue.append((nx, ny))
            components.append(component)

    if not components:
        return image
    largest = max(components, key=len)
    anchor_x = sum(x for x, _ in largest) / len(largest)
    anchor_y = sum(y for _, y in largest) / len(largest)
    for component in components:
        center_x = sum(x for x, _ in component) / len(component)
        center_y = sum(y for _, y in component) / len(component)
        if component is largest or ((center_x - anchor_x) ** 2 + (center_y - anchor_y) ** 2) ** 0.5 <= 60:
            continue
        for x, y in component:
            pixels[x, y] = (0, 0, 0, 0)
    return image


def crop_frame(source, source_rect):
    left, top, right, bottom = source_rect
    padding = 4
    extracted = source.crop((left - padding, top - padding, right + padding, bottom + padding))
    alpha = extracted.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        return Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE))

    subject = extracted.crop(bounds)
    if subject.width > 128 or subject.height > 138:
        scale = min(128 / subject.width, 138 / subject.height)
        subject = subject.resize(
            (round(subject.width * scale), round(subject.height * scale)),
            Image.Resampling.NEAREST,
        )

    frame = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE))
    x = (FRAME_SIZE - subject.width) // 2
    y = 142 - subject.height
    frame.alpha_composite(subject, (x, y))
    return frame


def repack(source_path, output_path):
    source = remove_connected_background(Image.open(source_path).convert("RGBA"))
    output = Image.new("RGBA", (FRAME_SIZE * 6, FRAME_SIZE * len(ANIMATIONS)))
    for row, (_, source_rects) in enumerate(ANIMATIONS.items()):
        for column, source_rect in enumerate(source_rects):
            output.alpha_composite(crop_frame(source, source_rect), (column * FRAME_SIZE, row * FRAME_SIZE))
    output.save(output_path, optimize=True)


if __name__ == "__main__":
    project = Path(__file__).resolve().parents[1]
    repack(
        project / "Assets/Creatures/Star/Source/approved_star_source.png",
        project / "Assets/Creatures/Star/star_creature_animations.png",
    )
    repack(
        project / "Assets/Creatures/Void/Source/approved_void_source.png",
        project / "Assets/Creatures/Void/void_creature_animations.png",
    )
