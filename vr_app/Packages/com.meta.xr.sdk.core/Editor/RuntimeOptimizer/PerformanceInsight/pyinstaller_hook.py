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

import os

from PyInstaller.utils.hooks import collect_data_files

# Collect all data files from perfetto package
datas = collect_data_files("perfetto")

# Hidden imports to ensure all modules are included
hiddenimports = [
    "perfetto",
    "perfetto.trace_processor",
    "perfetto.trace_processor.api",
    "perfetto.trace_processor.platform",
    "perfetto.trace_processor.protos",
    "perfetto.trace_processor.shell",
    "numpy",
    "pandas",
    "psutil",
]

# Optional: manually add specific files if collect_data_files misses them
try:
    import perfetto.trace_processor

    perfetto_path = os.path.dirname(perfetto.trace_processor.__file__)
    descriptor_file = os.path.join(perfetto_path, "trace_processor.descriptor")
    if os.path.exists(descriptor_file):
        datas.append((descriptor_file, "perfetto/trace_processor"))
except Exception:
    pass
