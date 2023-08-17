const settings = require('./appSettings');
const graphHelper = require('./graphHelper');

const express = require("express");
const app = express();

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

function initializeGraph(settings) {
  graphHelper.initializeGraphForAppOnlyAuth(settings);
}

// ユーザー一覧取得APIURL
app.get("/getAllUser", (req, res, next) => {
  (async () => {
    const userPage = await graphHelper.getUsersAsync();
    const users = userPage.value;

    // 1回のリクエストで取得できる数が決まっているので、
    // 以下の値がNullでなければまだ情報が取得できる
    const moreAvailable = userPage['@odata.nextLink'] != undefined;
    console.log(`\nMore users available? ${moreAvailable}`);

    res.json({ responseData: users });

  })().catch(next);
});

// カレンダー情報一覧取得APIURL
app.get("/getCalendarView", (req, res, next) => {
  (async () => {
    // クエリから情報を取得
    var targetId = req.query.targetId;
    var startDay = req.query.startDay;
    var endDay = req.query.endDay;

    const calendarView = await graphHelper.getCalendarViewAsync(targetId, startDay, endDay);
    const calendars = calendarView.value;

    // 1回のリクエストで取得できる数が決まっているので、
    // 以下の値がNullでなければまだ情報が取得できる
    const moreAvailable = calendars['@odata.nextLink'] != undefined;
    console.log(`\nMore users available? ${moreAvailable}`);

    res.json({ responseData: calendars });

  })().catch(next);
});

// エラー発生時の返却用
app.use((err, req, res) => {
  console.error(err);
  res.status(500).send('Internal Server Error'); // ステータスコード500でレスポンスを返す
});

initializeGraph(settings);

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`${PORT} Open. Listen Start !!`);
});