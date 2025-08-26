const express = require('express');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;

// 静的ファイルの配信
app.use(express.static('public'));
app.use(express.json());

// EJSテンプレートエンジンの設定
app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, 'views'));

// サンプルのガンチャートデータ
const ganttData = [
    {
        id: 1,
        taskName: 'プロジェクト企画',
        startDate: '2025-01-01',
        endDate: '2025-01-15',
        progress: 100,
        color: '#3498db',
        assignee: '田中'
    },
    {
        id: 2,
        taskName: '要件定義',
        startDate: '2025-01-10',
        endDate: '2025-01-25',
        progress: 90,
        color: '#e74c3c',
        assignee: '佐藤'
    },
    {
        id: 3,
        taskName: '設計',
        startDate: '2025-01-20',
        endDate: '2025-02-10',
        progress: 75,
        color: '#f39c12',
        assignee: '鈴木'
    },
    {
        id: 4,
        taskName: '開発フェーズ1',
        startDate: '2025-02-01',
        endDate: '2025-02-28',
        progress: 60,
        color: '#2ecc71',
        assignee: '山田'
    },
    {
        id: 5,
        taskName: '開発フェーズ2',
        startDate: '2025-02-15',
        endDate: '2025-03-15',
        progress: 30,
        color: '#9b59b6',
        assignee: '高橋'
    },
    {
        id: 6,
        taskName: 'テスト',
        startDate: '2025-03-01',
        endDate: '2025-03-20',
        progress: 10,
        color: '#34495e',
        assignee: '渡辺'
    },
    {
        id: 7,
        taskName: 'デプロイ・リリース',
        startDate: '2025-03-15',
        endDate: '2025-03-30',
        progress: 0,
        color: '#16a085',
        assignee: '伊藤'
    }
];

// ルート設定
app.get('/', (req, res) => {
    res.render('index', {
        title: 'プロジェクト ガンチャート',
        ganttData: JSON.stringify(ganttData)
    });
});

// API: ガンチャートデータの取得
app.get('/api/gantt-data', (req, res) => {
    res.json(ganttData);
});

// API: タスクの追加
app.post('/api/tasks', (req, res) => {
    const { taskName, startDate, endDate, progress, assignee } = req.body;

    if (!taskName || !startDate || !endDate) {
        return res.status(400).json({ error: '必要な項目が不足しています' });
    }

    const colors = ['#3498db', '#e74c3c', '#f39c12', '#2ecc71', '#9b59b6', '#34495e', '#16a085'];
    const newTask = {
        id: ganttData.length + 1,
        taskName,
        startDate,
        endDate,
        progress: parseInt(progress) || 0,
        color: colors[ganttData.length % colors.length],
        assignee: assignee || '未割当'
    };

    ganttData.push(newTask);
    res.json(newTask);
});

// API: タスクの更新
app.put('/api/tasks/:id', (req, res) => {
    const taskId = parseInt(req.params.id);
    const taskIndex = ganttData.findIndex(task => task.id === taskId);

    if (taskIndex === -1) {
        return res.status(404).json({ error: 'タスクが見つかりません' });
    }

    const { taskName, startDate, endDate, progress, assignee } = req.body;

    ganttData[taskIndex] = {
        ...ganttData[taskIndex],
        taskName: taskName || ganttData[taskIndex].taskName,
        startDate: startDate || ganttData[taskIndex].startDate,
        endDate: endDate || ganttData[taskIndex].endDate,
        progress: progress !== undefined ? parseInt(progress) : ganttData[taskIndex].progress,
        assignee: assignee || ganttData[taskIndex].assignee
    };

    res.json(ganttData[taskIndex]);
});

// API: タスクの削除
app.delete('/api/tasks/:id', (req, res) => {
    const taskId = parseInt(req.params.id);
    const taskIndex = ganttData.findIndex(task => task.id === taskId);

    if (taskIndex === -1) {
        return res.status(404).json({ error: 'タスクが見つかりません' });
    }

    const deletedTask = ganttData.splice(taskIndex, 1)[0];
    res.json(deletedTask);
});

// サーバー起動
app.listen(PORT, () => {
    console.log(`サーバーがポート${PORT}で起動しました`);
    console.log(`http://localhost:${PORT} でアクセスできます`);
});

module.exports = app;
