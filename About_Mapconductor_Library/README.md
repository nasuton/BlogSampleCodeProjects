# MapConductor Sample

MapConductor と MapLibre を使って、東京駅の地図とマーカーを表示する Kotlin / Jetpack Compose の Android サンプルです。

## サンプルの機能

- 東京駅（緯度 `35.6812`、経度 `139.7671`）を中心に表示
- 初期ズーム `17.0` で駅付近を拡大
- 日本語ラベルの地図スタイル `MapLibreDesign.OsmBrightJa` を使用
- 東京駅にマーカーを表示
- ドラッグによる移動、ピンチによる拡大・縮小

API キーの設定は不要です。地図データの取得にはインターネット接続が必要です。現在地は取得しません。

## 使用環境

| 項目 | バージョン・設定 |
| --- | --- |
| MapConductor BOM / core / compose / for-maplibre | 1.3.1 |
| MapLibre Android SDK | 13.3.1（MapConductor経由の依存関係） |
| Android Gradle Plugin | 9.0.1 |
| Kotlin Compose Plugin | 2.2.10 |
| Compose BOM | 2025.02.00 |
| Gradle | 9.2.1 |
| JDK | 21 |
| compileSdk | 36.1 |
| minSdk / targetSdk | 36 / 36 |

このアプリの実行には API 36（Android 16）以上の端末またはエミュレーターを使用します。

## Android Studio で実行する

1. このプロジェクトの `settings.gradle.kts` があるフォルダーを Android Studio で開きます。
2. SDK Manager で Android SDK 36.1 を用意します。
3. Gradle JDK に JDK 21 を設定し、Gradle Sync を実行します。
4. API 36 以上の端末またはエミュレーターを選択し、`app` を実行します。

SDK の場所を指定する `local.properties` は各自の環境で用意します。Android Studio が生成する設定を利用でき、このファイルは Git 管理から除外しています。

## コマンドラインでビルドする

JDK 21 と Android SDK を設定した環境で、プロジェクトのルートから実行します。Android SDK の場所は `local.properties` の `sdk.dir` または環境変数 `ANDROID_HOME` で指定します。

Windows:

```powershell
.\gradlew.bat :app:assembleDebug
```

macOS / Linux:

```sh
chmod +x gradlew
./gradlew :app:assembleDebug
```

APK は `app/build/outputs/apk/debug/app-debug.apk` に生成されます。

Lint と既存のユニットテストも実行する場合:

```powershell
.\gradlew.bat :app:assembleDebug :app:lintDebug :app:testDebugUnitTest
```

Windows で HTTPS 検査用の証明書が Windows に登録されている環境では、Java の証明書設定によって依存関係の取得に失敗する場合があります。その場合は、今回の Gradle 実行で Windows の信頼済み証明書を参照できます。

```powershell
.\gradlew.bat "-Djavax.net.ssl.trustStoreType=Windows-ROOT" "-Djavax.net.ssl.trustStore=NONE" :app:assembleDebug --no-daemon
```

## コードの読み方

[MainActivity.kt](app/src/main/java/com/example/mapconductorsample/MainActivity.kt) に、画面の作成と地図表示の処理をまとめています。

1. `GeoPoint` で東京駅の緯度・経度を用意します。
2. `rememberMapLibreMapViewState` で初期カメラ位置と地図スタイルを設定します。
3. `MarkerState` で東京駅に置くマーカーの状態を用意します。
4. `MapLibreMapView` と `Marker` で地図とマーカーを表示します。

表示位置や拡大率は、`MapScreen` 内の次の設定を変更してください。

```kotlin
val tokyoStation = remember { GeoPoint(35.6812, 139.7671) }

val mapState = rememberMapLibreMapViewState(
    cameraPosition = MapCameraPosition(
        position = tokyoStation,
        zoom = 17.0,
    ),
    mapDesign = MapLibreDesign.OsmBrightJa,
)
```

`zoom` は大きい値ほど拡大して表示します。保存済みのカメラ状態が復元される場合は、初期値の変更を確認するためにアプリを最近使ったアプリから終了して起動し直してください。

## 地図データについて

このサンプルでは `tile.openstreetmap.jp` の公開スタイルを使用します。地図の帰属表示は MapLibre の標準 UI から確認できます。本番利用では、用途に合うタイル配信元と利用条件を確認してください。

## 共有するファイル

ソースコード、リソース、テスト、Gradle 設定、Gradle Wrapper を含めています。`gradle/wrapper/gradle-wrapper.jar` はビルドに必要なため、Git 管理に含めます。

ローカルの SDK 設定、IDE 設定、キャッシュ、ビルド成果物、署名鍵などは `.gitignore` で除外しています。

## 公式ドキュメント

- [MapConductor Android SDK](https://github.com/MapConductor/android-sdk)
- [MapLibre のセットアップ](https://mapconductor.com/setup/android/maplibre/)
