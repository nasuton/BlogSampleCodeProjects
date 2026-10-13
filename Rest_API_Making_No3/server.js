const express = require("express");
const cors = require("cors");
const mysql = require("mysql");
const bcrypt = require("bcryptjs");

const app = express();

app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

const connection = mysql.createConnection({
    host: process.env.DB_HOST || "localhost",
    user: process.env.DB_USER || "xusTuc",
    password: process.env.DB_PASSWORD || "Thok7BL$iB",
    database: process.env.DB_NAME || "TODOLISTAPP",
});

connection.connect((err) => {
    if (err) {
        console.error("DB connecting error: " + err.stack);
        return;
    }
    console.log("DB connect success");
});

// query を Promise 化（async/await で書けるようにする）
const query = (sql, params = []) =>
    new Promise((resolve, reject) => {
        connection.query(sql, params, (err, results) => {
            if (err) return reject(err);
            resolve(results);
        });
    });

// 共通バリデーション
const isPositiveInt = (v) => Number.isInteger(Number(v)) && Number(v) > 0;
const isValidDate = (v) => typeof v === "string" && !Number.isNaN(Date.parse(v));

// =====================================================
// ユーザー
// =====================================================

// ユーザー登録
app.post("/signup", async (req, res) => {
    const { username, password, email } = req.body;

    if (
        typeof username !== "string" || !username.trim() || username.length > 50 ||
        typeof password !== "string" || password.length < 8 ||
        (email != null && (typeof email !== "string" || email.length > 255))
    ) {
        return res.status(400).json({ message: "パラメータが不正です" });
    }

    try {
        const hash = await bcrypt.hash(password, 10);
        const result = await query(
            "INSERT INTO users (name, email, password) VALUES (?, ?, ?)",
            [username.trim(), email ?? null, hash]
        );
        res.status(201).json({ message: "登録しました", userId: result.insertId });
    } catch (err) {
        if (err.code === "ER_DUP_ENTRY") {
            return res.status(409).json({ message: "すでに存在するユーザー名またはメールアドレスです" });
        }
        console.error(err);
        res.status(500).json({ message: "登録に失敗しました" });
    }
});

// ログイン（パスワード照合）
app.post("/login", async (req, res) => {
    const { username, password } = req.body;
    if (typeof username !== "string" || typeof password !== "string") {
        return res.status(400).json({ message: "パラメータが不正です" });
    }

    try {
        const rows = await query(
            "SELECT id, name, password, is_active FROM users WHERE name = ?",
            [username]
        );
        const user = rows[0];
        if (!user || !user.is_active || !(await bcrypt.compare(password, user.password))) {
            return res.status(401).json({ message: "ユーザー名またはパスワードが違います" });
        }

        await query("UPDATE users SET last_login_at = NOW() WHERE id = ?", [user.id]);
        res.json({ userId: user.id, userName: user.name });
    } catch (err) {
        console.error(err);
        res.status(500).json({ message: "ログインに失敗しました" });
    }
});

// ユーザー取得（GET はクエリ文字列で受け取る: /users?username=xxx）
app.get("/users", async (req, res) => {
    const { username } = req.query;
    if (typeof username !== "string" || !username.trim()) {
        return res.status(400).json({ message: "パラメータが不正です" });
    }

    try {
        const rows = await query(
            "SELECT id, name, email, created_at FROM users WHERE name = ? AND is_active = 1",
            [username]
        );
        if (rows.length === 0) {
            return res.status(404).json({ message: "対象ユーザーは存在しません" });
        }
        res.json({ userId: rows[0].id, userName: rows[0].name, email: rows[0].email });
    } catch (err) {
        console.error(err);
        res.status(500).json({ message: "取得に失敗しました" });
    }
});

// =====================================================
// タスク
// =====================================================

// タスク一覧取得（/tasks?user_id=1[&completed=0|1]）
app.get("/tasks", async (req, res) => {
    const { user_id, completed } = req.query;
    if (!isPositiveInt(user_id)) {
        return res.status(400).json({ message: "user_id が不正です" });
    }

    let sql = `SELECT id, user_id, title, description, due_date, priority,
                      is_completed, completed_at, created_at, updated_at
               FROM tasks WHERE user_id = ?`;
    const params = [Number(user_id)];

    if (completed === "0" || completed === "1") {
        sql += " AND is_completed = ?";
        params.push(Number(completed));
    }
    sql += " ORDER BY is_completed ASC, due_date ASC, priority ASC";

    try {
        const rows = await query(sql, params);
        res.json({ todosList: rows });
    } catch (err) {
        console.error(err);
        res.status(500).json({ message: "取得に失敗しました" });
    }
});

// タスク登録
app.post("/tasks", async (req, res) => {
    const { user_id, title, description, due_date, priority } = req.body;

    if (
        !isPositiveInt(user_id) ||
        typeof title !== "string" || !title.trim() || title.length > 255 ||
        !isValidDate(due_date) ||
        (description != null && typeof description !== "string") ||
        (priority != null && ![1, 2, 3].includes(Number(priority)))
    ) {
        return res.status(400).json({ message: "パラメータが不正です" });
    }

    try {
        const result = await query(
            `INSERT INTO tasks (user_id, title, description, due_date, priority)
             VALUES (?, ?, ?, ?, ?)`,
            [Number(user_id), title.trim(), description ?? null, new Date(due_date), priority ?? 2]
        );
        res.status(201).json({ message: "登録しました", taskId: result.insertId });
    } catch (err) {
        if (err.code === "ER_NO_REFERENCED_ROW_2") {
            return res.status(404).json({ message: "対象ユーザーは存在しません" });
        }
        console.error(err);
        res.status(500).json({ message: "登録に失敗しました" });
    }
});

// タスク更新（部分更新）
app.put("/tasks/:id", async (req, res) => {
    const { id } = req.params;
    const { user_id, title, description, due_date, priority, is_completed } = req.body;

    if (!isPositiveInt(id) || !isPositiveInt(user_id)) {
        return res.status(400).json({ message: "パラメータが不正です" });
    }

    const sets = [];
    const params = [];

    if (title !== undefined) {
        if (typeof title !== "string" || !title.trim() || title.length > 255) {
            return res.status(400).json({ message: "title が不正です" });
        }
        sets.push("title = ?"); params.push(title.trim());
    }
    if (description !== undefined) {
        if (description !== null && typeof description !== "string") {
            return res.status(400).json({ message: "description が不正です" });
        }
        sets.push("description = ?"); params.push(description);
    }
    if (due_date !== undefined) {
        if (!isValidDate(due_date)) {
            return res.status(400).json({ message: "due_date が不正です" });
        }
        sets.push("due_date = ?"); params.push(new Date(due_date));
    }
    if (priority !== undefined) {
        if (![1, 2, 3].includes(Number(priority))) {
            return res.status(400).json({ message: "priority が不正です" });
        }
        sets.push("priority = ?"); params.push(Number(priority));
    }
    if (is_completed !== undefined) {
        const done = is_completed === true || is_completed === 1 || is_completed === "1";
        sets.push("is_completed = ?"); params.push(done ? 1 : 0);
        sets.push("completed_at = ?"); params.push(done ? new Date() : null);
    }

    if (sets.length === 0) {
        return res.status(400).json({ message: "更新項目がありません" });
    }

    params.push(Number(id), Number(user_id));

    try {
        const result = await query(
            `UPDATE tasks SET ${sets.join(", ")} WHERE id = ? AND user_id = ?`,
            params
        );
        if (result.affectedRows === 0) {
            return res.status(404).json({ message: "対象タスクが存在しません" });
        }
        res.json({ message: "更新しました" });
    } catch (err) {
        console.error(err);
        res.status(500).json({ message: "更新に失敗しました" });
    }
});

// タスク削除（/tasks/:id?user_id=1）
app.delete("/tasks/:id", async (req, res) => {
    const { id } = req.params;
    const { user_id } = req.query;

    if (!isPositiveInt(id) || !isPositiveInt(user_id)) {
        return res.status(400).json({ message: "パラメータが不正です" });
    }

    try {
        const result = await query(
            "DELETE FROM tasks WHERE id = ? AND user_id = ?",
            [Number(id), Number(user_id)]
        );
        if (result.affectedRows === 0) {
            return res.status(404).json({ message: "対象タスクが存在しません" });
        }
        res.json({ message: "削除しました" });
    } catch (err) {
        console.error(err);
        res.status(500).json({ message: "削除に失敗しました" });
    }
});

// =====================================================
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Server listening on port ${PORT}`);
});