from encodings import utf_8
import sys
import errno
import os
import shutil
import zipfile
import yaml
import re
from datetime import datetime

configuration = "Debug"
target = "net462"

plugins = [
    "F95ZoneMetadata",
    "DLSiteMetadata",
    "FanzaMetadata",
    "GameManagement"
]

version_regex = re.compile(r"\d.\d.\d")
package_search_string = "<PackageReference Include=\"PlayniteSDK\" Version=\""


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


def validate_version(version) -> str:
    regex_match = version_regex.match(version)
    if regex_match is None:
        raise ValueError(f"Invalid Version: {version}")

    return version


def parse_new_version() -> str:
    new_version = sys.argv[2]
    return validate_version(new_version)


def get_playnite_version(csproj_path: str) -> str:
    validate_path(csproj_path, is_dir=False)

    package_version = None
    with open(csproj_path, "r", encoding="UTF-8") as f:
        for line in f:
            index = line.find(package_search_string)
            if index == -1:
                continue

            package_version = (line[index+len(package_search_string):])[:5]
            break

    if package_version is None:
        raise ValueError(f"Unable to find package reference in {csproj_path}")

    return validate_version(package_version)


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

    zip_output_path = os.path.join(output_path, f"{plugin}.pext")
    print(f"Packing plugin {plugin} to {zip_output_path}")
    with zipfile.ZipFile(zip_output_path, "w", zipfile.ZIP_DEFLATED) as myzip:
        for root, dirs, files in os.walk(build_output_path):
            for file in files:
                myzip.write(os.path.join(root, file), file)


def update_plugin_manifest(plugin: str, build_output_path: str, new_version: str):
    extension_file = os.path.join(build_output_path, "extension.yaml")
    validate_path(extension_file, False)

    print(f"Updating manifest of plugin {plugin} at {extension_file}")

    with open(extension_file, "r", encoding="UTF-8") as f:
        extension_manifest = yaml.safe_load(f)

    extension_manifest["Version"] = new_version

    with open(extension_file, "w", encoding="UTF-8") as f:
        yaml.safe_dump(extension_manifest, f)


def update_installer_manifest(plugin: str, new_version: str, playnite_version: str):
    manifest_path = os.path.join(os.path.abspath("manifests"), f"{plugin}.yaml")
    validate_path(manifest_path, is_dir=False)

    with open(manifest_path, "r", encoding="UTF-8") as f:
        installer_manifest = yaml.safe_load(f)

    packages = installer_manifest["Packages"]
    if packages is not None:
        existing_package = next((package for package in packages if package["Version"] == new_version), None)
        if existing_package is not None:
            print(f"Package already exists for {plugin} v{new_version}")
            return
    else:
        installer_manifest["Packages"] = []
        packages = installer_manifest["Packages"]

    release_date = datetime.now()

    new_package = {
        "Version": new_version,
        "RequiredApiVersion": playnite_version,
        "ReleaseDate": release_date.strftime("%Y-%m-%d"),
        "PackageUrl": f"https://github.com/erri120/Playnite.Extensions/releases/download/v{new_version}/{plugin}.pext"
    }

    packages.insert(0, new_package)
    installer_manifest["Packages"] = packages

    with open(manifest_path, "w", encoding="UTF-8") as f:
        yaml.safe_dump(installer_manifest, f)


def main():
    if len(sys.argv) != 3:
        raise ValueError("Not enough arguments!")

    src_path = os.path.abspath("src")
    validate_path(src_path)

    mode = sys.argv[1]

    csproj_path = os.path.join(src_path, "Extensions.Common", "Extensions.Common.csproj")
    validate_path(csproj_path, is_dir=False)

    if mode == "update":
        playnite_version = get_playnite_version(csproj_path)
        new_version = parse_new_version()
    else:
        playnite_version = ""
        new_version = ""

    for plugin in plugins:
        plugin_path = os.path.join(src_path, plugin)
        validate_path(plugin_path)

        build_output_path = get_build_output_path(plugin_path)

        if mode == "copy":
            copy_plugin(plugin, build_output_path)
        elif mode == "pack":
            pack_plugin(plugin, build_output_path)
        elif mode == "update":
            update_plugin_manifest(plugin, build_output_path, new_version)
            update_installer_manifest(plugin, new_version, playnite_version)
        else:
            raise ValueError(f"Unknown mode: {mode}")


if __name__ == "__main__":
    main()
