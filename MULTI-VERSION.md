# Revit 2024 / 2025 / 2026 / 2027 için paketleme

Bu fork (tr-TR locale düzeltmesi + EK-7 aktör bilgisi) şu an **Revit 2027** için
`master` üzerinde paketleniyor. Diğer sürümler aynı repodan, aynı daldan
üretilemez. Nedeni ve yapılması gerekenler aşağıda.

## Neden tek daldan 4 sürüm çıkmıyor

Sürümler arasında yalnızca `RevitAPI.dll` yolu değişmiyor; hedef framework ve
proje sistemi de değişiyor:

| Revit | Dal                      | Target framework | Proje formatı                |
|-------|--------------------------|------------------|------------------------------|
| 2024  | `origin/Release_24.x.x`  | .NET Framework **v4.8** | eski MSBuild (`ToolsVersion="4.0"`) |
| 2025  | `origin/Release_25.x.x`  | **net8.0-windows** | SDK-style                  |
| 2026  | `origin/Release_26.x.x`  | **net8.0-windows** | SDK-style                  |
| 2027  | `master` (bu dal)        | **net10.0-windows** | SDK-style                 |

2024 ayrı bir dünya: .NET Framework 4.8 + eski proje sistemi. Bu `#if` ile
yönetilebilecek bir fark değil, bu yüzden Autodesk da her sürüm için ayrı dal
tutuyor. Doğru yöntem: **her sürüm dalına port**.

Ayrıca kaynak kod sürümler arasında kaymış durumda. `master` ile sürüm dalları
arasındaki fark, bizim dokunduğumuz dosyalarda:

| Dosya | vs 2026 | vs 2025 | vs 2024 |
|---|---|---|---|
| `Exporter.cs` | 500+/398− | 712+/589− | 1075+/744− |
| `IFCCommandOverrideApplication.cs` | 114+/120− | 144+/98− | 217+/127− |
| `IFCExporterUIWindow.xaml.cs` | 173+/114− | 187+/50− | 401+/76− |
| `ExportOptionsCache.cs` | 124+/105− | 139+/134− | 236+/149− |

Yani `git cherry-pick` ya da düz `git merge` çalışmaz; değişiklikleri elle
yerleştirmek gerekir. **2026'dan başlayın** — `master`'a en yakın olan o;
2024 en zoru.

## Ön koşul: her sürümün SDK'sı ayrı lazım

Derleme, her Revit sürümünün kendi API assembly'lerini gerektirir. Bunlar
repoda değil, checkout'un **yanındaki** `API/<yıl>/` klasöründen okunur
(csproj'lardaki `..\..\..\API\<yıl>\` yolu oraya çıkar):

    E:\Documents\gitHub\API\2024\   <- şu an YOK
    E:\Documents\gitHub\API\2025\   <- şu an YOK
    E:\Documents\gitHub\API\2026\   <- şu an YOK
    E:\Documents\gitHub\API\2027\   <- var

Bu makinede yalnızca Revit 2027 kurulu. 2024/2025/2026 DLL'lerini elde etmenin
iki yolu var:

1. O Revit sürümünü kur, sonra `./stage-api.sh 2026`
2. Kurulu olan başka bir makineden `C:\Program Files\Autodesk\Revit 2026\`
   klasörünü kopyala, sonra `./stage-api.sh 2026 "D:/kopyaladigim/Revit 2026"`

`stage-api.sh` gereken 10 DLL'i kopyalar ve eksik olanı söyler. Bunlardan
`UIFramework.dll`, `AdWindows.dll`, `UserInterfaceUtility.dll` `IFCExporterUI.props`
içinden referanslanır — eksik olursa hata `IFCPropertyMappingModel.cs` içinde,
yaptığınız değişiklikle alakasız görünen bir yerde patlar.

> Not: Revit sürümlerini aynı makinede yan yana kurmak sorun değil, ayrı
> klasörlere kurulurlar.

## Bir sürümü port etme adımları (örnek: 2026)

### 1. Dalı aç

    git fetch origin
    git checkout -b epys/2026 origin/Release_26.x.x

### 2. API'yi hazırla

    ./stage-api.sh 2026

### 3. Yeni dosyaları kopyala (çakışma yok)

Bu 6 dosya tamamen bizim, hiçbir upstream dosyasını ezmiyor:

    git checkout fix/turkish-locale-invariant-ifc-identifiers -- \
      Source/Revit.IFC.Common/Utility/EPYSActorInfo.cs \
      Source/Revit.IFC.Common/Utility/EPYSProjectRequirements.cs \
      Source/Revit.IFC.Export/Exporter/PropertySet/Calculators/ProjectTypeCalculator.cs \
      Source/IFCExporterUIOverride/EPYSActorInfoTab.xaml \
      Source/IFCExporterUIOverride/EPYSActorInfoTab.xaml.cs \
      Source/IFCExporterUIOverride/Properties/Resources.tr.resx

Eski dallarda `.csproj`'lar dosyaları tek tek listeliyorsa (özellikle 2024),
bu yeni dosyaları ilgili `.csproj` içine `<Compile Include=...>` /
`<Page Include=...>` olarak eklemeyi unutmayın.

### 4. tr-TR locale düzeltmesini uygula

Bu kısım mekanik ve her dalda tutar: 19 dosyada `ToUpper()` → `ToUpperInvariant()`,
`ToLower()` → `ToLowerInvariant()`, kültüre duyarlı `string.Compare`/`Parse`
çağrılarına `CultureInfo.InvariantCulture`. Referans olarak:

    git show e20096d

Dosyaların çoğu eski dallarda da aynı isimde duruyor. Satır numaraları
tutmayacak, ama değişikliğin kendisi birebir aynı.

### 5. EK-7 entegrasyon noktalarını elle yerleştir

Asıl iş burada (~1026 satır). Şu bağlanma noktalarının hepsinin karşılığı
2024/2025/2026 dallarında **mevcut** (kontrol edildi), sadece yerleri farklı:

| Dosya | Ne yapılacak | Anchor |
|---|---|---|
| `Revit.IFC.Export/Utility/IFCExportConfiguration.cs` | `EPYSActorInfo` string property + serileştirme | mevcut `#region` blokları (COBie'nin yaptığının aynısı) |
| `Revit.IFC.Export/Utility/ExportOptionsCache.cs` | `EPYSActorInfo` property + `Create()` içinde `GetNamedStringOption(options, "EPYSActorInfo")` ile okuma | `static ... ExportOptionsCache Create` |
| `Revit.IFC.Export/Toolkit/IFCInstanceExporter.cs` | aktör/organizasyon oluşturma yardımcıları | mevcut `CreateActorRole` / `CreateOrganization` |
| `Revit.IFC.Export/Exporter/Exporter.cs` | `CreateEPYSActorRole` + adresler, OwnerHistory/Application yazımı | `epysActorInfo` kullanılan 2 blok |
| `IFCExporterUIOverride/IFCExporterUIWindow.xaml.cs` | `InitEPYSActorInfoUI()` çağrısı + sekme | 2026'da `Init*UI()` deseni var; 2024/2025'te yok, sekme ekleme kodunu COBie sekmesinin yanına koyun |
| `IFCExporterUIOverride/IFCCommandOverrideApplication.cs` | `ValidateEPYSRequirements()` ve export öncesi çağrısı | export akışındaki doğrulama bloğu |

Ayrıca `Properties/Resources.resx` + `Resources.Designer.cs` içine EK-7 string'leri
(`EPYSTabName` vb.) eklenecek. `Designer.cs` elle değil, `.resx`'i düzenleyip
yeniden üretmek daha güvenli.

`master`'daki halini yan yana görmek için:

    git diff fix/turkish-locale-invariant-ifc-identifiers -- <dosya>

### 6. Derle

    dotnet build Revit.IFC.sln -c Release -p:Platform=x64

2024 dalı .NET Framework 4.8 olduğu için `dotnet build` yerine `msbuild`
gerekebilir:

    & "C:\Program Files\Microsoft Visual Studio\2022\...\MSBuild\Current\Bin\MSBuild.exe" `
      Revit.IFC.sln /p:Configuration=Release /p:Platform=x64

Tek proje derlerken `SolutionDir` şart, yoksa MSB4019 alırsınız:

    dotnet build Source/Revit.IFC.Export/Revit.IFC.Export.csproj -c Release \
      -p:Platform=x64 -p:SolutionDir="E:/Documents/gitHub/revit-ifc/"

Kaynak dosyalar **CRLF + UTF-8 BOM**. `\n` yazan bir script'le düzenlemek her
satırı yeniden yazar ve 700 satırlık diff'i 16.000 satıra çevirir.

### 7. Sürüm metinlerini güncelle

`2027` geçen her yeri o sürüme çevirin:

- `Install/Program Files to Install/bundle/PackageContents.xml`
  — `SeriesMin`/`SeriesMax` (`R2026`), `Components Description`,
    `ComponentEntry` içindeki `AppName` / `ModuleName` / `AppDescription`,
    ve `AppVersion` (2026 için `26.x.x.x`)
- `Install/RevitIFCSetupWix/Product.wxs` — `Name`, `Version`, dizin adları
  (`IFC 2026.bundle`, `DIR_Addins_2026`)
- `Source/IFCExporterUIOverride/IFCExporterUIOverride.csproj:110` —
  `BaseOutputPath` doğrudan `IFC 2027.bundle` içine yazıyor. **Her dalda bunu
  o sürüme çevirin**, yoksa 2026 build'i 2027 bundle'ını ezer.

> `ProductCode` ve `UpgradeCode` her sürüm için **farklı** olmalı; aksi halde
> 2026 kurulumu 2027'yi upgrade sayıp kaldırır. Sürümler yan yana durmalı.

### 8. Deploy / test

    ./deploy-epys.sh 2026

Revit kapalı olmalı. Betik, `API/2026` yoksa yanlış sürüm deploy etmenizi
engeller.

### 9. MSI

`buildInstaller.bat` içindeki WiX yolu bu makinede yanlış; `candle.exe` /
`light.exe`'yi doğrudan çağırın (WiX 3.11,
`C:\Program Files (x86)\WiX Toolset v3.11\bin`). Beş `-d...TargetDir` bayrağı
zorunlu. `OfficialBuild` **tanımlamayın** — repoda olmayan `.sig` dosyalarını ister.

## Revit'in kendi exporter'ıyla çakışma

Her Revit sürümü kendi IFC exporter'ını `...\Revit <yıl>\AddIns\IFCExporterUI\`
altında getiriyor. İkisi de aynı `FullClassName`'i ve `ID_EXPORT_IFC`
binding'ini kullandığı için **ikinci yüklenen sessizce `Result.Failed` döner**.
Fork'un kazanması için her sürümde yerleşiğin manifestini
`.addin.disabled` yapın (admin gerekir). Hangisinin kazandığı journal'dan
görülür:

    C:\Users\<user>\AppData\Local\Autodesk\Revit\Autodesk Revit <yıl>\Journals\
    "Replacing command id 'ID_EXPORT_IFC'" -> kazanan DLL

## Önerilen sıra

1. **2026** — `master`'a en yakın, aynı proje sistemi, `Init*UI()` deseni bile var
2. **2025** — 2026 ile aynı framework; 2026 portunu referans alın
3. **2024** — en zoru; .NET Framework 4.8'e geçiş, eski csproj'lara dosya ekleme

Her biri ayrı dal olarak kalsın (`epys/2026`, `epys/2025`, `epys/2024`);
birleştirmeye çalışmayın.

---

## Birleşik MSI (tek kurulum, birden çok Revit sürümü)

`./build-epys-msi.sh` tek bir MSI üretir: her Revit sürümü için **ayrı bundle**,
her biri kendi API'sine derlenmiş DLL'lerle. Revit yalnızca `PackageContents.xml`'i
çalışan sürümle eşleşen bundle'ı yükler, bu yüzden dosya adları aynı olsa da
çakışmazlar.

    C:\ProgramData\Autodesk\ApplicationPlugins\
      IFC 2026.bundle\Contents\2026\   <- Revit 2026 API'sine derli
      IFC 2027.bundle\Contents\2027\   <- Revit 2027 API'sine derli

Parçalar:

| Dosya | İşi |
|---|---|
| `Install/RevitIFCSetupWix/ProductMulti.wxs` | birleşik MSI'ın WiX kaynağı (sürüm başına bir `Feature`) |
| `build-epys-msi.sh` | derler, doğrular, paketler |
| `Install/epys-stage/<yıl>/` | sürüm başına derlenmiş DLL'ler |
| `Install/epys-manifests/<yıl>/` | sürüm başına `PackageContents.xml` + `Revit.IFC.addin` |

Kullanım:

    ./build-epys-msi.sh --stage    # her dalı derleyip yeniden stage'ler, sonra paketler
    ./build-epys-msi.sh            # mevcut stage'den paketler

`--stage` her sürümün dalını sırayla checkout edip derler, sonra başladığınız dala
döner. Çalışma alanı kirliyse durur (dal değiştirmek değişikliklerinizi bozar),
bu yüzden önce commit veya stash edin.

Betik paketlemeden **önce** şunları doğrular; biri tutmazsa durur:

- her sürüm için 7 zorunlu DLL ve 3 dil satellite'ı mevcut mu
- `PackageContents.xml` ve `Revit.IFC.addin` mevcut mu
- iki sürümün `Revit.IFC.Export.dll`'i **birbirinden farklı** mı (aynı olmaları,
  birinin yanlış daldan derlendiği anlamına gelir — Revit sessizce hiçbir şey
  yüklemez, bu yüzden en değerli kontrol budur)
- her `PackageContents.xml` yalnızca kendi `SeriesMin="R<yıl>"`ini bildiriyor mu

Yeni bir sürüm (örn. 2025) eklemek için: `build-epys-msi.sh` içindeki `VERSIONS`
satırına `2025:epys/2025` ekleyin, `ProductMulti.wxs`'e 2026 bloğunu kopyalayıp
yılı ve GUID'leri değiştirin, bir de `Feature_2025` tanımlayın.

### Sürüm başına farklılıklar (dikkat)

- **Assembly adı**: `Revit.IFC.Common` 2026'da `Revit.IFC.Common.dll`, 2027'de
  `Revit.IFC.Common.OpenSource.dll` olarak derlenir. `ProductMulti.wxs` ikisini
  de doğru adla paketler; yeni sürüm eklerken kontrol edin.
- **`ProductCode` ve `UpgradeCode`**: `PackageContents.xml` içindeki `ProductCode`
  her sürüm için ayrı (2026: `{4E8A1D62-...}`, 2027: `{B3D7E41C-...}`).
- **Birleşik MSI'ın `UpgradeCode`'u** (`5F2B8E94-...`), `Product.wxs`'in ürettiği
  tek sürümlük E-PYS MSI'ından **farklı**. Yani ikisi birbirini upgrade etmez;
  her ikisini de kurmuşsanız biri diğerini kaldırmaz ama aynı dosyaları yazarlar
  — birini kurmadan önce diğerini kaldırın.
- **`Resources` klasörü**: `PackageContents.xml` ikonu ve yardım dosyasını
  `.\Contents\Resources\` içinde arar, yani her bundle kendi kopyasını taşır.
