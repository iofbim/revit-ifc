#!/bin/sh
# Build the combined E-PYS MSI that installs the IFC exporter for several Revit
# versions at once (one bundle per version, each built against its own API).
#
#   ./build-epys-msi.sh              # build the MSI from what is already staged
#   ./build-epys-msi.sh --stage      # also rebuild each branch and restage first
#
# Each Revit version needs its own branch, because the target framework and the
# referenced RevitAPI differ (see MULTI-VERSION.md). --stage checks out each
# branch in turn, builds it, and copies the result into Install/epys-stage/<year>.
# Without --stage, the existing staged files are used as they are.
set -e

REPO="E:/Documents/gitHub/revit-ifc"
STAGE="$REPO/Install/epys-stage"
MANIFESTS="$REPO/Install/epys-manifests"
OUT="$REPO/Install/Releasex64"
WIX="C:/Program Files (x86)/WiX Toolset v3.11/bin"
VERSION="27.3.12.3"

# year:branch pairs. Add a line here (and a Feature in ProductMulti.wxs) to cover
# another Revit version.
VERSIONS="2026:epys/2026 2027:fix/turkish-locale-invariant-ifc-identifiers"

[ -x "$WIX/candle.exe" ] || { echo "ERROR: WiX 3.11 not found at $WIX" >&2; exit 1; }

# ---------------------------------------------------------------- staging ----
if [ "$1" = "--stage" ]; then
   ORIGINAL_BRANCH=$(git -C "$REPO" branch --show-current)
   [ -n "$ORIGINAL_BRANCH" ] || { echo "ERROR: detached HEAD; checkout a branch first." >&2; exit 1; }

   if [ -n "$(git -C "$REPO" status --porcelain)" ]; then
      echo "ERROR: working tree is dirty. Commit or stash first -- staging switches" >&2
      echo "       branches and would otherwise disturb your changes." >&2
      exit 1
   fi

   for pair in $VERSIONS; do
      year=${pair%%:*}
      branch=${pair#*:}
      echo "=== staging $year from $branch ==="

      [ -d "$REPO/../API/$year" ] || {
         echo "ERROR: $REPO/../API/$year missing. Run ./stage-api.sh $year first." >&2
         git -C "$REPO" checkout -q "$ORIGINAL_BRANCH"
         exit 1; }

      git -C "$REPO" checkout -q "$branch"
      ( cd "$REPO" && dotnet build Revit.IFC.sln -c Release -p:Platform=x64 \
           >"$STAGE/../build-$year.log" 2>&1 ) || {
         echo "ERROR: build failed for $year; see Install/build-$year.log" >&2
         git -C "$REPO" checkout -q "$ORIGINAL_BRANCH"
         exit 1; }

      d="$STAGE/$year"
      rm -rf "$d"; mkdir -p "$d/de" "$d/fr" "$d/tr"
      UI="$REPO/Source/IFCExporterUIOverride/bin/x64/Release"

      # The common assembly is renamed on the 2027 line, so copy whichever exists.
      for f in IFCExporterUIOverride.dll Revit.IFC.Export.dll Newtonsoft.Json.dll \
               Antlr4.Runtime.dll Revit.IFC.Common.dll Revit.IFC.Common.OpenSource.dll; do
         [ -f "$UI/$f" ] && cp "$UI/$f" "$d/"
      done
      cp "$REPO/Source/Revit.IFC.Import/bin/x64/Release/Revit.IFC.Import.dll" "$d/"
      cp "$REPO/Source/Revit.IFC.Import/bin/x64/Release/ICSharpCode.SharpZipLib.dll" "$d/"
      cp "$REPO/Source/Revit.IFC.Import.Core/bin/x64/Release/Revit.IFC.Import.Core.dll" "$d/"
      for c in de fr tr; do
         cp "$UI/$c/IFCExporterUIOverride.resources.dll" "$d/$c/"
      done
      echo "  staged $(ls "$d"/*.dll | wc -l) assemblies"
   done

   git -C "$REPO" checkout -q "$ORIGINAL_BRANCH"
   echo "=== restored branch $ORIGINAL_BRANCH ==="
fi

# ------------------------------------------------------------ verification ---
# A wrong-version assembly here produces an installer that loads nothing, with no
# error from Revit, so check the staging before packaging rather than after.
for pair in $VERSIONS; do
   year=${pair%%:*}
   d="$STAGE/$year"
   [ -d "$d" ] || { echo "ERROR: $d missing. Run with --stage." >&2; exit 1; }
   for f in IFCExporterUIOverride.dll Revit.IFC.Export.dll Revit.IFC.Import.dll \
            Revit.IFC.Import.Core.dll Newtonsoft.Json.dll Antlr4.Runtime.dll \
            ICSharpCode.SharpZipLib.dll; do
      [ -f "$d/$f" ] || { echo "ERROR: $d/$f missing. Run with --stage." >&2; exit 1; }
   done
   for c in de fr tr; do
      [ -f "$d/$c/IFCExporterUIOverride.resources.dll" ] || {
         echo "ERROR: $d/$c satellite missing (is Resources.$c.resx in the csproj?)" >&2
         exit 1; }
   done
   [ -f "$MANIFESTS/$year/PackageContents.xml" ] || {
      echo "ERROR: $MANIFESTS/$year/PackageContents.xml missing." >&2; exit 1; }
   [ -f "$MANIFESTS/$year/Revit.IFC.addin" ] || {
      echo "ERROR: $MANIFESTS/$year/Revit.IFC.addin missing." >&2; exit 1; }
done

# Two versions sharing a byte-identical exporter means one was staged from the
# wrong branch -- they reference different RevitAPI assemblies and cannot match.
set -- $VERSIONS
if [ $# -ge 2 ]; then
   first=${1%%:*}; second=${2%%:*}
   a=$(md5sum "$STAGE/$first/Revit.IFC.Export.dll" | cut -d' ' -f1)
   b=$(md5sum "$STAGE/$second/Revit.IFC.Export.dll" | cut -d' ' -f1)
   [ "$a" != "$b" ] || {
      echo "ERROR: $first and $second staged the SAME Revit.IFC.Export.dll." >&2
      echo "       One of them was built from the wrong branch. Run with --stage." >&2
      exit 1; }
fi

# Each version's bundle must declare only its own Revit release, or Revit would
# load assemblies built against a different API.
for pair in $VERSIONS; do
   year=${pair%%:*}
   grep -q "SeriesMin=\"R$year\"" "$MANIFESTS/$year/PackageContents.xml" || {
      echo "ERROR: $MANIFESTS/$year/PackageContents.xml does not declare SeriesMin R$year." >&2
      exit 1; }
done

# ---------------------------------------------------------------- packaging --
mkdir -p "$OUT"
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

echo "=== candle ==="
"$WIX/candle.exe" -nologo -arch x64 -ext WixUtilExtension \
   -dProjectDir="$REPO/Install/RevitIFCSetupWix/" \
   -dStageDir="$STAGE/" \
   -dManifestDir="$MANIFESTS/" \
   -out "$WORK/ProductMulti.wixobj" \
   "$REPO/Install/RevitIFCSetupWix/ProductMulti.wxs"

echo "=== light ==="
MSI="$OUT/RevitIFC-EPYS-Multi-$VERSION.msi"
# ICE61 fires because this package deliberately allows same-version reinstall
# over itself; ICE82/ICE91 relate to the shared files installed per bundle.
"$WIX/light.exe" -nologo -ext WixUtilExtension -ext WixUIExtension \
   -sice:ICE61 \
   -out "$MSI" "$WORK/ProductMulti.wixobj"

echo
echo "Built: $MSI"
ls -la "$MSI"
