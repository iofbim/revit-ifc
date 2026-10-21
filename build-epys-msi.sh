#!/bin/sh
# Build one E-PYS MSI per Revit version.
#
#   ./build-epys-msi.sh              # build from what is already staged
#   ./build-epys-msi.sh --stage      # also rebuild each branch and restage first
#   ./build-epys-msi.sh 2027         # build only that version
#   ./build-epys-msi.sh --stage 2027 # restage and build only that version
#
# Each Revit version needs its own branch, because the target framework and the
# referenced RevitAPI differ (see MULTI-VERSION.md). --stage checks out each branch
# in turn, builds it, and copies the result into Install/epys-stage/<year>. Without
# --stage, the existing staged files are used as they are.
#
# Every version produces a separate installer with its own UpgradeCode, so the
# versions install side by side and each upgrades only its own earlier builds.
set -e

REPO="E:/Documents/gitHub/revit-ifc"
STAGE="$REPO/Install/epys-stage"
MANIFESTS="$REPO/Install/epys-manifests"
OUT="$REPO/Install/Releasex64"
WIX="C:/Program Files (x86)/WiX Toolset v3.11/bin"

# One record per supported Revit version:
#   year : branch : product version : common assembly name : UpgradeCode
#
# The common assembly was renamed on the 2027 line. The UpgradeCodes are fixed and
# must never change for a given year: Windows uses them to recognise an earlier
# install of the same product, and a changed code would install a second copy
# alongside the old one instead of upgrading it.
VERSIONS="
2026:epys/2026:26.5.0.1:Revit.IFC.Common.dll:666432A7-4D2A-5D4E-8EAB-BD13107B9E02
2027:fix/turkish-locale-invariant-ifc-identifiers:27.3.12.3:Revit.IFC.Common.OpenSource.dll:A83F383D-342C-540E-98BF-855731D1A87B
"

field() { echo "$1" | cut -d: -f"$2"; }

# ------------------------------------------------------------- arguments ----
DO_STAGE=no
ONLY=""
for arg in "$@"; do
   case "$arg" in
      --stage) DO_STAGE=yes ;;
      20[0-9][0-9]) ONLY="$arg" ;;
      *) echo "ERROR: unknown argument '$arg'" >&2; exit 1 ;;
   esac
done

selected() {
   [ -z "$ONLY" ] || [ "$ONLY" = "$1" ]
}

if [ -n "$ONLY" ]; then
   found=no
   for rec in $VERSIONS; do
      [ "$(field "$rec" 1)" = "$ONLY" ] && found=yes
   done
   [ "$found" = yes ] || { echo "ERROR: $ONLY is not a supported version." >&2; exit 1; }
fi

[ -x "$WIX/candle.exe" ] || { echo "ERROR: WiX 3.11 not found at $WIX" >&2; exit 1; }

# ---------------------------------------------------------------- staging ----
if [ "$DO_STAGE" = yes ]; then
   ORIGINAL_BRANCH=$(git -C "$REPO" branch --show-current)
   [ -n "$ORIGINAL_BRANCH" ] || { echo "ERROR: detached HEAD; checkout a branch first." >&2; exit 1; }

   if [ -n "$(git -C "$REPO" status --porcelain)" ]; then
      echo "ERROR: working tree is dirty. Commit or stash first -- staging switches" >&2
      echo "       branches and would otherwise disturb your changes." >&2
      exit 1
   fi

   for rec in $VERSIONS; do
      year=$(field "$rec" 1)
      selected "$year" || continue
      branch=$(field "$rec" 2)
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
for rec in $VERSIONS; do
   year=$(field "$rec" 1)
   selected "$year" || continue
   common=$(field "$rec" 4)
   upgrade=$(field "$rec" 5)
   d="$STAGE/$year"

   [ -d "$d" ] || { echo "ERROR: $d missing. Run with --stage." >&2; exit 1; }
   for f in IFCExporterUIOverride.dll Revit.IFC.Export.dll Revit.IFC.Import.dll \
            Revit.IFC.Import.Core.dll Newtonsoft.Json.dll Antlr4.Runtime.dll \
            ICSharpCode.SharpZipLib.dll "$common"; do
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

   # Each version's bundle must declare only its own Revit release, or Revit would
   # load assemblies built against a different API.
   grep -q "SeriesMin=\"R$year\"" "$MANIFESTS/$year/PackageContents.xml" || {
      echo "ERROR: $MANIFESTS/$year/PackageContents.xml does not declare SeriesMin R$year." >&2
      exit 1; }

   # The bundle's UpgradeCode must be this year's. The Autodesk plug-in loader treats
   # UpgradeCode as the package identity, so two bundles sharing one are seen as the
   # same product in two versions: it keeps whichever it loads and silently ignores
   # the other, with nothing in the journal to say so.
   grep -q "UpgradeCode=\"{$upgrade}\"" "$MANIFESTS/$year/PackageContents.xml" || {
      echo "ERROR: $MANIFESTS/$year/PackageContents.xml does not declare" >&2
      echo "       UpgradeCode={$upgrade}. Each year needs its own, or Revit loads" >&2
      echo "       only one of the bundles." >&2
      exit 1; }
done

# Cross-check: no two years may share a bundle ProductCode or UpgradeCode.
if [ -z "$ONLY" ]; then
   for field in ProductCode UpgradeCode; do
      dupes=$(for rec in $VERSIONS; do
                 grep -oE "$field=\"[^\"]*\"" "$MANIFESTS/$(field "$rec" 1)/PackageContents.xml"
              done | sort | uniq -d)
      [ -z "$dupes" ] || {
         echo "ERROR: bundles share the same $field: $dupes" >&2
         echo "       Revit would load only one of them. Give each year its own." >&2
         exit 1; }
   done
fi

# Two versions sharing a byte-identical exporter means one was staged from the
# wrong branch -- they reference different RevitAPI assemblies and cannot match.
# Only meaningful when building every version, since it compares across them.
if [ -z "$ONLY" ]; then
   prev_year=""; prev_sum=""
   for rec in $VERSIONS; do
      year=$(field "$rec" 1)
      sum=$(md5sum "$STAGE/$year/Revit.IFC.Export.dll" | cut -d' ' -f1)
      if [ -n "$prev_sum" ] && [ "$sum" = "$prev_sum" ]; then
         echo "ERROR: $prev_year and $year staged the SAME Revit.IFC.Export.dll." >&2
         echo "       One of them was built from the wrong branch. Run with --stage." >&2
         exit 1
      fi
      prev_year="$year"; prev_sum="$sum"
   done
fi

# ---------------------------------------------------------------- packaging --
mkdir -p "$OUT"
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

BUILT=""
for rec in $VERSIONS; do
   year=$(field "$rec" 1)
   selected "$year" || continue
   version=$(field "$rec" 3)
   common=$(field "$rec" 4)
   upgrade=$(field "$rec" 5)

   echo "=== packaging Revit $year ($version) ==="
   "$WIX/candle.exe" -nologo -arch x64 -ext WixUtilExtension \
      -dProjectDir="$REPO/Install/RevitIFCSetupWix/" \
      -dStageDir="$STAGE/" \
      -dManifestDir="$MANIFESTS/" \
      -dYear="$year" \
      -dYearVersion="$version" \
      -dYearUpgradeCode="$upgrade" \
      -dCommonAssembly="$common" \
      -out "$WORK/Product-$year.wixobj" \
      "$REPO/Install/RevitIFCSetupWix/ProductVersion.wxs"

   MSI="$OUT/RevitIFC-EPYS-$year-$version.msi"
   # ICE61 fires because this package deliberately allows same-version reinstall
   # over itself.
   "$WIX/light.exe" -nologo -ext WixUtilExtension -ext WixUIExtension \
      -sice:ICE61 \
      -out "$MSI" "$WORK/Product-$year.wixobj"

   BUILT="$BUILT$MSI
"
done

echo
echo "Built:"
echo "$BUILT" | sed '/^$/d' | while read -r m; do
   printf '  %-52s %s bytes\n' "$(basename "$m")" "$(stat -c%s "$m")"
done
