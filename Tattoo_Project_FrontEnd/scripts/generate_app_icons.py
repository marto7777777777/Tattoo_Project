from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
PUBLIC = ROOT / "public"


def create_icon(size: int, safe_padding: bool = False) -> Image.Image:
    image = Image.new("RGB", (size, size), "#09090d")
    draw = ImageDraw.Draw(image)
    unit = size / 1024

    margin = int((142 if safe_padding else 34) * unit)
    draw.rounded_rectangle(
        (margin, margin, size - margin, size - margin),
        radius=int(205 * unit),
        fill="#171128",
        outline="#34255b",
        width=max(1, int(18 * unit)),
    )
    draw.ellipse(
        (int(182 * unit), int(182 * unit), int(842 * unit), int(842 * unit)),
        outline="#392866",
        width=max(1, int(26 * unit)),
    )

    purple = "#9d79ff"
    light = "#c8b5ff"
    draw.rounded_rectangle(
        (int(330 * unit), int(277 * unit), int(446 * unit), int(747 * unit)),
        radius=int(18 * unit),
        fill=purple,
    )
    draw.rounded_rectangle(
        (int(505 * unit), int(277 * unit), int(621 * unit), int(747 * unit)),
        radius=int(18 * unit),
        fill=purple,
    )
    draw.rounded_rectangle(
        (int(570 * unit), int(277 * unit), int(760 * unit), int(390 * unit)),
        radius=int(52 * unit),
        fill=purple,
    )
    draw.rounded_rectangle(
        (int(570 * unit), int(500 * unit), int(744 * unit), int(613 * unit)),
        radius=int(52 * unit),
        fill=purple,
    )
    draw.polygon(
        [
            (int(650 * unit), int(568 * unit)),
            (int(760 * unit), int(568 * unit)),
            (int(875 * unit), int(747 * unit)),
            (int(735 * unit), int(747 * unit)),
        ],
        fill=purple,
    )
    draw.arc(
        (int(220 * unit), int(720 * unit), int(820 * unit), int(930 * unit)),
        start=195,
        end=338,
        fill=light,
        width=max(2, int(24 * unit)),
    )
    return image


create_icon(192).save(PUBLIC / "pwa-192x192.png", optimize=True)
create_icon(512).save(PUBLIC / "pwa-512x512.png", optimize=True)
create_icon(512, safe_padding=True).save(
    PUBLIC / "pwa-maskable-512x512.png", optimize=True
)
create_icon(180).save(PUBLIC / "apple-touch-icon.png", optimize=True)


def create_splash(width: int, height: int) -> Image.Image:
    splash = Image.new("RGB", (width, height), "#09090d")
    logo_size = min(width, height) * 34 // 100
    logo = create_icon(logo_size)
    splash.paste(logo, ((width - logo_size) // 2, (height - logo_size) // 2))
    return splash


android_res = ROOT / "android" / "app" / "src" / "main" / "res"
if android_res.exists():
    launcher_sizes = {
        "mipmap-mdpi": 48,
        "mipmap-hdpi": 72,
        "mipmap-xhdpi": 96,
        "mipmap-xxhdpi": 144,
        "mipmap-xxxhdpi": 192,
    }
    foreground_sizes = {
        "mipmap-mdpi": 108,
        "mipmap-hdpi": 162,
        "mipmap-xhdpi": 216,
        "mipmap-xxhdpi": 324,
        "mipmap-xxxhdpi": 432,
    }

    for folder, size in launcher_sizes.items():
        destination = android_res / folder
        create_icon(size).save(destination / "ic_launcher.png", optimize=True)
        create_icon(size).save(destination / "ic_launcher_round.png", optimize=True)

    for folder, size in foreground_sizes.items():
        create_icon(size, safe_padding=True).save(
            android_res / folder / "ic_launcher_foreground.png", optimize=True
        )

    for splash_path in android_res.glob("drawable*/splash.png"):
        with Image.open(splash_path) as current_splash:
            create_splash(*current_splash.size).save(splash_path, optimize=True)

ios_assets = ROOT / "ios" / "App" / "App" / "Assets.xcassets"
if ios_assets.exists():
    create_icon(1024).save(
        ios_assets / "AppIcon.appiconset" / "AppIcon-512@2x.png", optimize=True
    )

    for splash_path in (ios_assets / "Splash.imageset").glob("*.png"):
        with Image.open(splash_path) as current_splash:
            create_splash(*current_splash.size).save(splash_path, optimize=True)
