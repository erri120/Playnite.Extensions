from encodings import utf_8
import sys
import errno
import os
import shutil
import zipfile
import yaml

configuration = "Debug"
target = "net462"

plugins = [
    "F95ZoneMetadata",
    "DLSiteMetadata",
    "FanzaMetadata",
    "GameManagement"
]


def validate_path(path: str, is_dir=True):
    if not os.path.exists(path):
        raise FileNotFoundError(errno.ENOENT, os.strerror(errno.ENOENT), path)
    if is_dir and not os.path.isdir(path):
        raise NotADirectoryError(errno.ENOTDIR, os.strerror(errno.ENOTDIR), path)


def decode_utf8(b) -> str:
    return utf_8.decode(b)[0]


def get_build_output_path(plugin_path: str) -> str:
    result = os.path.join(plugin_path, "bin", configuration, target)
    validate_path(result)
    return result


def copy_plugin(plugin: str, build_output_path: str):
    output_path = os.path.abspath(sys.argv[2])
    validate_path(output_path)

    plugin_output_path = os.path.join(output_path, plugin)

    if os.path.exists(plugin_output_path):
        shutil.rmtree(plugin_output_path)

    print(f"Copying build output files from {build_output_path} to {plugin_output_path}")
    shutil.copytree(build_output_path, plugin_output_path)


def pack_plugin(plugin: str, build_output_path: str):
    output_path = os.path.abspath(sys.argv[2])
    validate_path(output_path)

    zip_output_path = os.path.join(output_path, f"{plugin}.zip")
    print(f"Packing plugin {plugin} to {zip_output_path}")
    with zipfile.ZipFile(zip_output_path, "w", zipfile.ZIP_LZMA) as myzip:
        for root, dirs, files in os.walk(build_output_path):
            for file in files:
                myzip.write(os.path.join(root, file), file)


def update_plugin_manifest(plugin: str, build_output_path: str):
    new_version = sys.argv[2]

    extension_file = os.path.join(build_output_path, "extension.yaml")
    validate_path(extension_file, False)

    print(f"Updating manifest of plugin {plugin} at {extension_file}")

    with open(extension_file, "r", encoding="UTF-8") as f:
        extension_manifest = yaml.safe_load(f)
        extension_manifest["Version"] = new_version

    with open(extension_file, "w", encoding="UTF-8") as f:
        yaml.safe_dump(extension_manifest, f)


def main():
    if len(sys.argv) != 3:
        raise ValueError("Not enough arguments!")

    src_path = os.path.abspath("src")
    validate_path(src_path)

    mode = sys.argv[1]

    for plugin in plugins:
        plugin_path = os.path.join(src_path, plugin)
        validate_path(plugin_path)

        build_output_path = get_build_output_path(plugin_path)

        if mode == "copy":
            copy_plugin(plugin, build_output_path)
        elif mode == "pack":
            pack_plugin(plugin, build_output_path)
        elif mode == "update":
            update_plugin_manifest(plugin, build_output_path)
        else:
            raise ValueError(f"Unknown mode: {mode}")


if __name__ == "__main__":
    main()
