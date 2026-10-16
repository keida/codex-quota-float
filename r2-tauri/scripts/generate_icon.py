from __future__ import annotations

from io import BytesIO
from math import cos, radians, sin
from pathlib import Path
from xml.etree import ElementTree

from PIL import Image, ImageDraw, __version__ as PILLOW_VERSION


ROOT = Path(__file__).resolve().parents[1]
ICO_PATH = ROOT / "src-tauri" / "icons" / "icon.ico"
ASSET_DIR = ROOT / "assets" / "icon"
SVG_PATH = ASSET_DIR / "codex-float-r2.svg"
PREVIEW_SIZES = (512, 1024)
ICO_SIZES = (16, 20, 24, 32, 48, 64, 128, 256)
SCALE = 4
EXPECTED_PILLOW_VERSION = "12.3.0"

BACKGROUND = "#152439"
TRACK = "#344B61"
ACCENT = "#4DE0BF"
CANVAS = 512
CENTER = 256
RADIUS = 144
STROKE = 52
START_ANGLE = -90
END_ANGLE = 190
BACKGROUND_INSET = 32
BACKGROUND_RADIUS = 128


def point(angle: float) -> tuple[float, float]:
    theta = radians(angle)
    return CENTER + RADIUS * cos(theta), CENTER + RADIUS * sin(theta)


def svg_source() -> str:
    start_x, start_y = point(START_ANGLE)
    end_x, end_y = point(END_ANGLE)
    sweep = END_ANGLE - START_ANGLE
    large_arc = 1 if sweep > 180 else 0
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {CANVAS} {CANVAS}" role="img" aria-labelledby="title">
  <title id="title">Codex Float R2 quota ring</title>
  <rect x="{BACKGROUND_INSET}" y="{BACKGROUND_INSET}" width="{CANVAS - 2 * BACKGROUND_INSET}" height="{CANVAS - 2 * BACKGROUND_INSET}" rx="{BACKGROUND_RADIUS}" fill="{BACKGROUND}"/>
  <circle cx="{CENTER}" cy="{CENTER}" r="{RADIUS}" fill="none" stroke="{TRACK}" stroke-width="{STROKE}"/>
  <path d="M {start_x:.3f} {start_y:.3f} A {RADIUS} {RADIUS} 0 {large_arc} 1 {end_x:.3f} {end_y:.3f}" fill="none" stroke="{ACCENT}" stroke-width="{STROKE}" stroke-linecap="round"/>
</svg>
'''


def render(size: int) -> Image.Image:
    scale = SCALE
    extent = size * scale
    factor = extent / CANVAS
    image = Image.new("RGBA", (extent, extent), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    def px(value: float) -> int:
        return round(value * factor)

    draw.rounded_rectangle(
        (px(BACKGROUND_INSET), px(BACKGROUND_INSET), px(CANVAS - BACKGROUND_INSET), px(CANVAS - BACKGROUND_INSET)),
        radius=px(BACKGROUND_RADIUS),
        fill=BACKGROUND,
    )
    outline_radius = RADIUS + STROKE / 2
    box = (
        px(CENTER - outline_radius),
        px(CENTER - outline_radius),
        px(CENTER + outline_radius),
        px(CENTER + outline_radius),
    )
    draw.ellipse(box, outline=TRACK, width=px(STROKE))
    draw.arc(box, start=START_ANGLE, end=END_ANGLE, fill=ACCENT, width=px(STROKE))
    cap_radius = STROKE / 2
    for angle in (START_ANGLE, END_ANGLE):
        x, y = point(angle)
        draw.ellipse(
            (px(x - cap_radius), px(y - cap_radius), px(x + cap_radius), px(y + cap_radius)),
            fill=ACCENT,
        )
    return image.resize((size, size), Image.Resampling.LANCZOS)


def verify_png(path: Path, size: int) -> None:
    with Image.open(path) as image:
        assert image.format == "PNG", f"{path.name}: expected PNG"
        assert image.size == (size, size), f"{path.name}: wrong dimensions {image.size}"
        rgba = image.convert("RGBA")
        assert rgba.getpixel((0, 0))[3] == 0, f"{path.name}: top-left must be transparent"
        assert rgba.getpixel((size // 2, size // 2))[3] == 255, f"{path.name}: center must be opaque"
        verify_arc_caps(rgba, size, path.name)


def verify_arc_caps(image: Image.Image, size: int, name: str) -> None:
    radius = max(1, round(3 * size / 256))
    for angle in (START_ANGLE, END_ANGLE):
        x, y = point(angle)
        px_x, px_y = round(x * size / CANVAS), round(y * size / CANVAS)
        found = any(
            (pixel := image.getpixel((sample_x, sample_y)))[1] > 170 and pixel[0] < 130 and pixel[3] > 180
            for sample_x in range(max(0, px_x - radius), min(size, px_x + radius + 1))
            for sample_y in range(max(0, px_y - radius), min(size, px_y + radius + 1))
        )
        assert found, f"{name}: missing accent at arc endpoint {angle} degrees"


def verify_ico(path: Path) -> None:
    with Image.open(path) as icon:
        assert icon.format == "ICO", f"{path.name}: expected ICO"
        sizes = icon.ico.sizes()
        assert sizes == {(size, size) for size in ICO_SIZES}, f"unexpected ICO sizes: {sorted(sizes)}"
        for size in sorted(sizes):
            image = icon.ico.getimage(size).convert("RGBA")
            assert image.getpixel((0, 0))[3] == 0, f"ICO {size}: top-left must be transparent"
            assert image.getpixel((size[0] // 2, size[1] // 2))[3] == 255, f"ICO {size}: center must be opaque"
            verify_arc_caps(image, size[0], f"ICO {size}")


def generate() -> None:
    if PILLOW_VERSION != EXPECTED_PILLOW_VERSION:
        raise RuntimeError(f"Pillow {EXPECTED_PILLOW_VERSION} is required for this raster output; found {PILLOW_VERSION}")
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    ICO_PATH.parent.mkdir(parents=True, exist_ok=True)
    SVG_PATH.write_text(svg_source(), encoding="utf-8", newline="\n")
    master = render(256)
    master.save(ICO_PATH, format="ICO", sizes=[(size, size) for size in ICO_SIZES])
    for size in PREVIEW_SIZES:
        render(size).save(ASSET_DIR / f"codex-float-r2-{size}.png", format="PNG", optimize=False)


def verify() -> None:
    root = ElementTree.parse(SVG_PATH).getroot()
    assert root.attrib.get("viewBox") == f"0 0 {CANVAS} {CANVAS}"
    assert SVG_PATH.read_text(encoding="utf-8") == svg_source(), "SVG artwork differs from the shared vector geometry"
    verify_ico(ICO_PATH)
    for size in PREVIEW_SIZES:
        verify_png(ASSET_DIR / f"codex-float-r2-{size}.png", size)
    print(f"ICO verified: {len(ICO_SIZES)} sizes {ICO_SIZES}; alpha corners=0, centers=255; arc caps match shared SVG geometry; Pillow {PILLOW_VERSION}")
    print(f"Previews verified: {PREVIEW_SIZES}; SVG viewBox=0 0 {CANVAS} {CANVAS}")


if __name__ == "__main__":
    generate()
    verify()
