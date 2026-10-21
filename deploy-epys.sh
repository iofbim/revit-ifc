#!/bin/sh
# Deploy the built IFC exporter into a Revit plugin bundle.
# Revit must be CLOSED: it locks the assemblies while running.
#
#   ./deploy-epys.sh          -> deploys for $DEFAULT_YEAR (below)
#   ./deploy-epys.sh 2026     -> deploys for Revit 2026
#
# The year must match the branch you built: each release branch targets one
# Revit version (2024 = Release_24.x.x, 2025 = Release_25.x.x,
# 2026 = Release_26.x.x, 2027 = master). See MULTI-VERSION.md.
set -e

DEFAULT_YEAR=2027
YEAR="${1:-$DEFAULT_YEAR}"
case "$YEAR" in
   2024|2025|2026|2027) ;;
   *) echo "usage: $0 [2024|2025|2026|2027]" >&2; exit 1 ;;
esac

REPO="E:/Documents/gitHub/revit-ifc"
UI="$REPO/Source/IFCExporterUIOverride/bin/x64/Release"
DEST="C:/ProgramData/Autodesk/ApplicationPlugins/IFC $YEAR.bundle/Contents/$YEAR"

if tasklist 2>/dev/null | grep -qi "^Revit.exe"; then
   echo "ERROR: Revit is running - close it first (it locks the DLLs)." >&2
   exit 1
fi

# Guard against deploying a build made against a different Revit API: the staged
# API folder for $YEAR must exist, otherwise the build cannot have targeted it.
[ -d "$REPO/../API/$YEAR" ] || {
   echo "ERROR: $REPO/../API/$YEAR does not exist, so this build is not against" >&2
   echo "       Revit $YEAR. Run ./stage-api.sh $YEAR and rebuild." >&2
   exit 1; }

mkdir -p "$DEST/de" "$DEST/fr" "$DEST/tr"

for f in IFCExporterUIOverride.dll Revit.IFC.Export.dll Revit.IFC.Common.OpenSource.dll \
         Newtonsoft.Json.dll Antlr4.Runtime.dll; do
   cp "$UI/$f" "$DEST/" && echo "  $f"
done
cp "$REPO/Source/Revit.IFC.Import/bin/x64/Release/Revit.IFC.Import.dll" "$DEST/" && echo "  Revit.IFC.Import.dll"

for c in de fr tr; do
   cp "$UI/$c/IFCExporterUIOverride.resources.dll" "$DEST/$c/" && echo "  $c/IFCExporterUIOverride.resources.dll"
done

# The .addin manifest is what Revit actually discovers; without it the DLLs are inert.
cp "$REPO/Install/Program Files to Install/Revit.IFC.addin" "$DEST/" && echo "  Revit.IFC.addin"

echo "Deployed for Revit $YEAR. Restart Revit."
