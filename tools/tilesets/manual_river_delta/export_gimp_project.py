"""Run inside GIMP 3's python-fu-eval batch interpreter."""

from gi.repository import Gimp, Gio


def export_xcf(xcf_path, lava_path, composite_path):
    image = Gimp.file_load(
        Gimp.RunMode.NONINTERACTIVE,
        Gio.File.new_for_path(xcf_path),
    )
    if image is None:
        raise RuntimeError(f"GIMP could not load {xcf_path}")

    layers = list(image.get_layers())
    visible = [layer for layer in layers if layer.get_visible()]
    if len(visible) < 2:
        raise RuntimeError("expected visible edited lava and base layers")

    lava_image = image.duplicate()
    lava_layers = list(lava_image.get_layers())
    for layer in lava_layers[1:]:
        lava_image.remove_layer(layer)
    lava_layers[0].set_visible(True)
    if not Gimp.file_save(
        Gimp.RunMode.NONINTERACTIVE,
        lava_image,
        Gio.File.new_for_path(lava_path),
        None,
    ):
        raise RuntimeError(f"failed to export {lava_path}")

    composite_image = image.duplicate()
    composite_image.merge_visible_layers(Gimp.MergeType.CLIP_TO_IMAGE)
    if not Gimp.file_save(
        Gimp.RunMode.NONINTERACTIVE,
        composite_image,
        Gio.File.new_for_path(composite_path),
        None,
    ):
        raise RuntimeError(f"failed to export {composite_path}")

    print(f"EXPORTED_LAVA {lava_path}")
    print(f"EXPORTED_COMPOSITE {composite_path}")
