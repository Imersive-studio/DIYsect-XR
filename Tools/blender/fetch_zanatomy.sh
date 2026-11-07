#!/usr/bin/env bash
# Run from any directory: bash Tools/blender/fetch_zanatomy.sh
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
source_dir="$root/SourceAssets"
checkout="$source_dir/z-anatomy-upstream"
mkdir -p "$source_dir/Z-Anatomy"
if [ ! -d "$checkout/.git" ]; then
  git clone --depth 1 --filter=blob:none --no-checkout https://github.com/Z-Anatomy/The-blend "$checkout"
fi
git -C "$checkout" checkout HEAD -- Z-Anatomy.zip
unzip -o "$checkout/Z-Anatomy.zip" -d "$source_dir/Z-Anatomy"
git -C "$checkout" rev-parse HEAD > "$source_dir/Z-Anatomy/SOURCE_COMMIT"
find "$source_dir/Z-Anatomy" -name Startup.blend -print
