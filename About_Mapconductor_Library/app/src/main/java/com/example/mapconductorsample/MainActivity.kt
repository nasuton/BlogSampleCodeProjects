package com.example.mapconductorsample

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import com.example.mapconductorsample.ui.theme.MapconductorSampleTheme
import com.mapconductor.compose.marker.Marker
import com.mapconductor.core.features.GeoPoint
import com.mapconductor.core.map.MapCameraPosition
import com.mapconductor.core.marker.MarkerState
import com.mapconductor.maplibre.MapLibreDesign
import com.mapconductor.maplibre.MapLibreMapView
import com.mapconductor.maplibre.rememberMapLibreMapViewState

/**
 * アプリ起動時の入口。Compose の画面を作成し、地図を表示する MapScreen を呼び出します。
 */
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // ステータスバー・ナビゲーションバーの領域まで画面を描画できるようにします。
        enableEdgeToEdge()

        // この Activity の UI を Jetpack Compose で定義します。
        setContent {
            // アプリ共通の配色・文字スタイルを適用します。
            MapconductorSampleTheme {
                // 画面全体の枠組みを作り、システムバーを避けるための余白を受け取ります。
                Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
                    MapScreen(
                        modifier = Modifier
                            .fillMaxSize()
                            // 地図の操作 UI がシステムバーと重ならないようにします。
                            .padding(innerPadding)
                    )
                }
            }
        }
    }
}

/**
 * MapConductor の API を通して、MapLibre の地図と東京駅のマーカーを表示します。
 * 処理の流れは「座標の用意 → 地図の状態の作成 → マーカーの状態の作成 → 描画」です。
 */
@Composable
fun MapScreen(modifier: Modifier = Modifier) {
    // GeoPoint の引数は緯度・経度の順です。東京駅の座標を地図の中心とマーカーに使います。
    // remember により、Compose が画面を再描画（再コンポーズ）しても同じ座標オブジェクトを使います。
    val tokyoStation = remember { GeoPoint(35.6812, 139.7671) }

    // 地図のカメラ位置とスタイルを管理する状態を作成します。
    // SDK が状態を保持・保存するため、再コンポーズのたびに初期位置へ戻ることはありません。
    val mapState = rememberMapLibreMapViewState(
        // 初回表示の中心と拡大率です。zoom は大きい値ほど拡大して表示します。
        cameraPosition = MapCameraPosition(
            position = tokyoStation,
            zoom = 17.0,
        ),
        // 日本語ラベルの公開スタイルを選択します。地図データはインターネット経由で取得します。
        mapDesign = MapLibreDesign.OsmBrightJa,
    )

    // 東京駅に置くマーカーの状態です。再コンポーズ時も同じ状態を使うように remember します。
    val stationMarker = remember { MarkerState(position = tokyoStation) }

    // MapConductor の MapLibreMapView がネイティブの地図ビューを表示し、ライフサイクルを管理します。
    // modifier には呼び出し元で指定した画面サイズと余白が渡されます。
    MapLibreMapView(
        state = mapState,
        modifier = modifier,
    ) {
        // このブロックで、表示中の地図にマーカーなどの要素を追加します。
        Marker(state = stationMarker)
    }
}
