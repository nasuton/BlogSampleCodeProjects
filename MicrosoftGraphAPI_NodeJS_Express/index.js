const express = require("express");
const app = express();
const axios = require('axios');
const auth = require('./auth');

app.use(express.json());
app.use(express.urlencoded({ extended: true }));

async function callGraphAPI(endpoint) {
    console.log(`endPointURL:${endpoint}`);
    const authResponse = await auth.getToken(auth.tokenRequest);

    const options = {
        headers: {
            Authorization: `Bearer ${authResponse.accessToken}`
        }
    };

    try {
        const response = await axios.get(endpoint, options);
        console.log(response.data);
        return response.data;
    } catch (error) {
        console.log(error)
        return error;
    }
}

app.get("/getAllUser", (req, res) => {
    var apiURL = "/v1.0/users";
    var select = "";
    if(req.query.select != null)  {
        select = `$select=${req.query.select}`
    }

    if(select != "") {
        apiURL = `${apiURL}?${select}`;
    }

    callGraphAPI(`${auth.apiConfig.uri}${apiURL}`).then(result => {
        res.json({ responseData: result });
    });
});

app.get("/getCalendarView", (req, res) => {
    var targetId = req.query.targetId;
    var startDay = req.query.startDay;
    var endDay = req.query.endDay;
    var select = "";
    if(req.query.select != null)  {
        select = `$select=${req.query.select}`
    }

    // 必須となるデータが未定義の場合
    if(targetId == null || startDay == null || endDay == null) {
        res.status(400).send("パラメータが不正です");
        return;
    }

    var apiURL = `/v1.0/users/${targetId}/calendarView?startDateTime=${startDay}T00:00:00&endDateTime=${endDay}T23:59:59`;

    if(select != "") {
        apiURL = `${apiURL}&${select}`;
    }

    callGraphAPI(`${auth.apiConfig.uri}${apiURL}`).then(result => {
        res.json({ responseData: result });
    });
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`${PORT} Open. Listen Start !!`);
});