# Copyright (c) Meta Platforms, Inc. and affiliates.
# All rights reserved.
#
# Licensed under the Oculus SDK License Agreement (the "License");
# you may not use the Oculus SDK except in compliance with the License,
# which is provided at the time of installation or download, or which
# otherwise accompanies this software in either electronic or hard copy form.
#
# You may obtain a copy of the License at
#
# https://developer.oculus.com/licenses/oculussdk/
#
# Unless required by applicable law or agreed to in writing, the Oculus SDK
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

"""Create PyInstaller single file executable

Resources
========

- https://pyinstaller.org/en/stable/usage.html
- https://pyinstaller.org/en/stable/runtime-information.html
- If changing icon, may need to clear OS Icon Cache to see changes.
- https://stackoverflow.com/questions/45507/is-there-a-python-library-for-generating-ico-files
- https://www.internalfb.com/code/fbsource/[7a2fde14a5e9ba0a39e141138c9de31213561f79]/arvr/projects/ariane/aria_research_kit/aria_studio/electron/meta_only/BUCK?lines=70

Please contact [Matt Pare](https://www.internalfb.com/profile/view/1190816847)
with questions or support for this module.
"""

import importlib.resources
import os
import pathlib
import platform
import tempfile

import PyInstaller.__main__

# Workaround for longpath desginator prefixed by some build modes and FB infra...
LONG_PATH_CHARS = r"\?"

if __name__ == "__main__":
    # TODO: Cleanup this section

    here = os.getcwd()
    script_resource = str(here + "/MetricAPI.py").lstrip(LONG_PATH_CHARS)
    print(script_resource)
    if platform.system().lower() == "windows":
        tmp_path = pathlib.Path(tempfile.gettempdir())
    else:
        tmp_path = pathlib.Path(here)

    # The commented out lines of code are options and arguments used during
    # debug of the application build process. It is recommended to retain these
    # in place as documentation and convenience for future use.
    PyInstaller.__main__.run(
        [
            str(script_resource),
            "--onefile",
            # "--onedir",
            f"--name=MetricAPI",
            f"--distpath={tmp_path / 'dist'}",
            f"--workpath={tmp_path / 'build'}",
            f"--specpath={tmp_path}",
            f"--runtime-hook={here + '/pyinstaller_hook.py'}",
            "--clean",
            # "--noconfirm",
            # "--console",
            # "--noupx",
            # "--debug=all",
        ]
    )
