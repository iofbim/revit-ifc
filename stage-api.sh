#!/bin/sh
# Stage the Revit API assemblies that this repo's csproj HintPaths expect.
#
#   ./stage-api.sh 2026
#   ./stage-api.sh 2026 "D:/path/to/Revit 2026"   (if Revit is not installed locally)
#
# The projects reference ..\..\..\API\<year>\ relative to each project dir, which
# resolves to the API/ folder that sits NEXT TO the revit-ifc checkout.
set -e

YEAR="$1"
[ -n "$YEAR" ] || { echo "usage: $0 <2024|2025|2026|2027> [revit-install-dir]" >&2; exit 1; }

SRC="${2:-C:/Program Files/Autodesk/Revit $YEAR}"
DEST="E:/Documents/gitHub/API/$YEAR"

[ -d "$SRC" ] || { echo "ERROR: Revit $YEAR not found at: $SRC" >&2
                   echo "Install Revit $YEAR, or pass its directory as the 2nd argument." >&2
                   exit 1; }

mkdir -p "$DEST"

# RevitAPISteel.dll is only referenced by Revit.IFC.Export; the last three are
# pulled in by IFCExporterUI.props / IFCExporterUIOverride.csproj and are easy to miss.
REQUIRED="RevitAPI.dll RevitAPIIFC.dll RevitAPIUI.dll AddInLoader.dll"
OPTIONAL="RevitAPISteel.dll Autodesk.UI.Windows.dll Autodesk.Weave.Wpf.dll \
          UserInterfaceUtility.dll UIFramework.dll AdWindows.dll"

missing=""
for f in $REQUIRED; do
   if [ -f "$SRC/$f" ]; then cp "$SRC/$f" "$DEST/" && echo "  $f"
   else missing="$missing $f"; fi
done
for f in $OPTIONAL; do
   if [ -f "$SRC/$f" ]; then cp "$SRC/$f" "$DEST/" && echo "  $f"
   else echo "  -- not present in Revit $YEAR: $f"; fi
done

[ -z "$missing" ] || { echo "ERROR: required assemblies missing:$missing" >&2; exit 1; }
echo "Staged Revit $YEAR API into $DEST"
