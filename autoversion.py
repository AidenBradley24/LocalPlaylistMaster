import os

file = open("version.txt")
version = file.readline()
version = version.strip()
version_name = f"{version} Release"

os.environ["MY_VERSION"] = version
os.environ["MY_VERSION_NAME"] = version_name

github_file = os.getenv("GITHUB_ENV")
if github_file is not None:
    with open(github_file, "a") as file:
        file.write(f"MY_VERSION={version}\n")
        file.write(f"MY_VERSION_NAME={version_name}\n")