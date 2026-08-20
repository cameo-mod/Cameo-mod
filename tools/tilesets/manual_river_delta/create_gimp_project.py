"""Run inside GIMP 3's python-fu-eval batch interpreter."""

from gi.repository import Gimp, Gio


def create_xcf(base_path, lava_path, xcf_path):
    image = Gimp.file_load(
        Gimp.RunMode.NONINTERACTIVE,
        Gio.File.new_for_path(base_path),
    )
    if image is None:
        raise RuntimeError(f"GIMP could not load base {base_path}")

    base = list(image.get_layers())[0]
    base.set_name("Approved cracked base")
    base.set_visible(True)

    original = Gimp.file_load_layer(
        Gimp.RunMode.NONINTERACTIVE,
        image,
        Gio.File.new_for_path(lava_path),
    )
    if original is None:
        raise RuntimeError(f"GIMP could not load lava layer {lava_path}")
    image.insert_layer(original, None, 0)
    original.set_name("Original donor lava - hidden backup")
    original.set_visible(False)

    editable = original.copy()
    image.insert_layer(editable, None, 0)
    editable.set_name("Editable donor lava cutout")
    editable.set_visible(True)

    if not Gimp.file_save(
        Gimp.RunMode.NONINTERACTIVE,
        image,
        Gio.File.new_for_path(xcf_path),
        None,
    ):
        raise RuntimeError(f"failed to save {xcf_path}")

    print(f"CREATED_XCF {xcf_path}")


def inspect_xcf(xcf_path):
    image = Gimp.file_load(
        Gimp.RunMode.NONINTERACTIVE,
        Gio.File.new_for_path(xcf_path),
    )
    if image is None:
        raise RuntimeError(f"GIMP could not inspect {xcf_path}")
    print(f"XCF {xcf_path} {image.get_width()}x{image.get_height()}")
    for index, layer in enumerate(image.get_layers()):
        offset = layer.get_offsets()
        print(
            f"LAYER {index} name={layer.get_name()} "
            f"visible={layer.get_visible()} offset={offset.offset_x},{offset.offset_y}"
        )
