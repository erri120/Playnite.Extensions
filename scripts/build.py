from encodings import utf_8
import sys
import errno
import os
import shutil
import zipfile

configuration = "Debug"
target = "net462"

plugins = [
    "F95ZoneMetadata",
    "DLSiteMetadata"
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


def main():
    if len(sys.argv) != 3:
        raise ValueError("Not enough arguments!")

    src_path = os.path.abspath("../src")
    validate_path(src_path)

    mode = sys.argv[1]

    for plugin in plugins:
        plugin_path = os.path.join(src_path, plugin)
        validate_path(plugin_path)

        build_output_path = get_build_output_path(plugin_path)

        if mode == "copy":
            output_path = os.path.abspath(sys.argv[2])
            validate_path(output_path)

            plugin_output_path = os.path.join(output_path, plugin)

            if os.path.exists(plugin_output_path):
                shutil.rmtree(plugin_output_path)

            print(f"Copying build output files from {build_output_path} to {plugin_output_path}")
            shutil.copytree(build_output_path, plugin_output_path)
        elif mode == "pack":
            output_path = os.path.abspath(sys.argv[2])
            validate_path(output_path)

            zip_output_path = os.path.join(output_path, f"{plugin}.zip")
            with zipfile.ZipFile(zip_output_path, "w", zipfile.ZIP_LZMA) as myzip:
                for root, dirs, files in os.walk(build_output_path):
                    for file in files:
                        myzip.write(os.path.join(root, file), file)
        elif mode == "update":
            new_version = sys.argv[2]

            extension_file = os.path.join(build_output_path, "extension.yaml")
            validate_path(extension_file, False)

            lines = []
            with open(extension_file, "r", encoding="UTF-8") as f:
                for line in f:
                    if line.startswith("Version: "):
                        lines.append(f"Version: {new_version}\n")
                    else:
                        lines.append(line)

            with open(extension_file, "w", encoding="UTF-8") as f:
                f.writelines(lines)

        else:
            raise ValueError(f"Unknown mode: {mode}")


if __name__ == "__main__":
    main()
