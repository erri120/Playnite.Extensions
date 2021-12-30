from encodings import utf_8
import sys
import errno
import os
import subprocess
import shutil

configuration = "Debug"
target = "net462"

plugins = [
    "F95ZoneMetadata"
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
    if len(sys.argv) != 2:
        raise ValueError("Not enough arguments!")

    output_path = os.path.abspath(sys.argv[1])
    validate_path(output_path)

    src_path = os.path.abspath("../src")
    validate_path(src_path)

    for plugin in plugins:
        plugin_path = os.path.join(src_path, plugin)
        validate_path(plugin_path)

#         print(f"Building project {plugin}")
#
#         process_result = subprocess.run("dotnet build", shell=True, capture_output=True, check=False, cwd=plugin_path)
#         print(decode_utf8(process_result.stderr))
#         print(decode_utf8(process_result.stdout))
#         process_result.check_returncode()

        build_output_path = get_build_output_path(plugin_path)
        plugin_output_path = os.path.join(output_path, plugin)

        if os.path.exists(plugin_output_path):
            shutil.rmtree(plugin_output_path)

        print(f"Copying build output files from {build_output_path} to {plugin_output_path}")
        shutil.copytree(build_output_path, plugin_output_path)

    pass


if __name__ == "__main__":
    main()
