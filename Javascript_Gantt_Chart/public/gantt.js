// ガンチャート管理クラス
class GanttChartManager {
    constructor() {
        this.chart = null;
        this.ganttData = [];
        this.viewConfig = {
            startDate: null,
            endDate: null,
            timeUnit: 'day'
        };
        this.init();
    }

    // 初期化
    init() {
        // サーバーからデータを取得
        this.loadData();
        // イベントリスナーを設定
        this.setupEventListeners();
    }

    // データを読み込み
    async loadData() {
        try {
            const response = await fetch('/api/gantt-data');
            this.ganttData = await response.json();
            this.initChart();
            this.updateTaskList();
        } catch (error) {
            console.error('データの読み込みに失敗しました:', error);
        }
    }

    // Chart.jsでガンチャートを初期化
    initChart() {
        const ctx = document.getElementById('ganttChart').getContext('2d');
        
        if (this.chart) {
            this.chart.destroy();
        }

        // Y軸のラベルを作成
        const taskLabels = this.ganttData.map(task => `${task.taskName} (${task.assignee})`);

        // データセットを一つにまとめる
        const chartData = this.ganttData.map((task, index) => {
            const startDate = new Date(task.startDate);
            const endDate = new Date(task.endDate);
            
            return {
                x: [startDate, endDate],
                y: index, // インデックスを使用
                backgroundColor: this.adjustOpacity(task.color, 0.8),
                borderColor: task.color,
                borderWidth: 2,
                taskInfo: task
            };
        });

        this.chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: taskLabels,
                datasets: [{
                    label: 'タスク期間',
                    data: chartData,
                    backgroundColor: chartData.map(item => item.backgroundColor),
                    borderColor: chartData.map(item => item.borderColor),
                    borderWidth: 2,
                    barThickness: 25,
                    categoryPercentage: 0.8,
                    barPercentage: 0.9
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: 'y',
                interaction: {
                    intersect: false,
                    mode: 'nearest'
                },
                scales: {
                    x: this.getXAxisConfig(),
                    y: {
                        type: 'category',
                        labels: taskLabels,
                        title: {
                            display: true,
                            text: 'タスク',
                            font: {
                                size: 14,
                                weight: 'bold'
                            }
                        },
                        grid: {
                            color: '#f0f0f0',
                            lineWidth: 1
                        },
                        ticks: {
                            font: {
                                size: 12
                            }
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0,0,0,0.8)',
                        titleColor: 'white',
                        bodyColor: 'white',
                        borderColor: '#3498db',
                        borderWidth: 1,
                        callbacks: {
                            title: function(context) {
                                try {
                                    const dataIndex = context[0].dataIndex;
                                    const task = ganttManager.ganttData[dataIndex];
                                    return task ? task.taskName : '';
                                } catch (error) {
                                    console.error('Tooltip title error:', error);
                                    return '';
                                }
                            },
                            label: function(context) {
                                try {
                                    const dataIndex = context.dataIndex;
                                    const task = ganttManager.ganttData[dataIndex];
                                    if (!task) return '';
                                    
                                    return [
                                        `開始: ${task.startDate}`,
                                        `終了: ${task.endDate}`,
                                        `進捗: ${task.progress}%`,
                                        `担当: ${task.assignee}`
                                    ];
                                } catch (error) {
                                    console.error('Tooltip label error:', error);
                                    return '';
                                }
                            }
                        }
                    },
                    zoom: {
                        limits: {
                            x: {min: 'original', max: 'original'},
                        },
                        pan: {
                            enabled: true,
                            mode: 'x',
                            modifierKey: 'ctrl',
                        },
                        zoom: {
                            wheel: {
                                enabled: true,
                            },
                            pinch: {
                                enabled: true
                            },
                            mode: 'x',
                        }
                    }
                },
                onHover: (event, elements) => {
                    if (event.native && event.native.target) {
                        event.native.target.style.cursor = elements.length > 0 ? 'pointer' : 'default';
                    }
                },
                onClick: (event, elements) => {
                    if (elements.length > 0) {
                        const dataIndex = elements[0].index;
                        const task = this.ganttData[dataIndex];
                        if (task) {
                            this.showTaskDetails(task);
                        }
                    }
                }
            }
        });
    }

    // X軸の設定を取得
    getXAxisConfig() {
        const config = {
            type: 'time',
            time: {
                unit: this.viewConfig.timeUnit,
                displayFormats: {
                    day: 'MM/dd',
                    week: 'MM/dd',
                    month: 'yyyy/MM'
                }
            },
            title: {
                display: true,
                text: '期間',
                font: {
                    size: 14,
                    weight: 'bold'
                }
            },
            grid: {
                color: '#e0e0e0',
                lineWidth: 1
            },
            ticks: {
                font: {
                    size: 11
                }
            }
        };

        // 表示期間が設定されている場合
        if (this.viewConfig.startDate && this.viewConfig.endDate) {
            config.min = this.viewConfig.startDate;
            config.max = this.viewConfig.endDate;
        }

        return config;
    }

    // タスク詳細を表示
    showTaskDetails(task) {
        alert(`タスク詳細:\n\nタスク名: ${task.taskName}\n開始日: ${task.startDate}\n終了日: ${task.endDate}\n進捗: ${task.progress}%\n担当者: ${task.assignee}`);
    }

    // イベントリスナーを設定
    setupEventListeners() {
        // タスク追加ボタン
        const addButton = document.getElementById('addTaskBtn');
        if (addButton) {
            addButton.addEventListener('click', () => this.addTask());
        }

        // Enterキーでタスク追加
        document.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && e.target.closest('.controls')) {
                this.addTask();
            }
        });

        // 期間操作のイベントリスナー
        this.setupPeriodControls();
    }

    // 期間操作のイベントリスナーを設定
    setupPeriodControls() {
        // 期間更新ボタン
        const updatePeriodBtn = document.getElementById('updatePeriodBtn');
        if (updatePeriodBtn) {
            updatePeriodBtn.addEventListener('click', () => this.updateViewPeriod());
        }

        // 期間リセットボタン
        const resetPeriodBtn = document.getElementById('resetPeriodBtn');
        if (resetPeriodBtn) {
            resetPeriodBtn.addEventListener('click', () => this.resetViewPeriod());
        }

        // データに合わせるボタン
        const fitToDataBtn = document.getElementById('fitToDataBtn');
        if (fitToDataBtn) {
            fitToDataBtn.addEventListener('click', () => this.fitToData());
        }

        // ズームボタン
        const zoomInBtn = document.getElementById('zoomInBtn');
        const zoomOutBtn = document.getElementById('zoomOutBtn');
        const zoomResetBtn = document.getElementById('zoomResetBtn');

        if (zoomInBtn) {
            zoomInBtn.addEventListener('click', () => this.zoomIn());
        }
        if (zoomOutBtn) {
            zoomOutBtn.addEventListener('click', () => this.zoomOut());
        }
        if (zoomResetBtn) {
            zoomResetBtn.addEventListener('click', () => this.resetZoom());
        }

        // プリセットボタン
        const presetBtns = document.querySelectorAll('.preset-btn');
        presetBtns.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const preset = e.target.dataset.preset;
                this.setViewPreset(preset);
            });
        });

        // 時間単位変更
        const timeUnitSelect = document.getElementById('timeUnit');
        if (timeUnitSelect) {
            timeUnitSelect.addEventListener('change', (e) => {
                this.viewConfig.timeUnit = e.target.value;
                this.initChart();
            });
        }
    }

    // 表示期間を更新
    updateViewPeriod() {
        const startDate = document.getElementById('viewStartDate').value;
        const endDate = document.getElementById('viewEndDate').value;

        if (!startDate || !endDate) {
            alert('開始日と終了日を設定してください');
            return;
        }

        if (new Date(startDate) >= new Date(endDate)) {
            alert('開始日は終了日より前の日付を選択してください');
            return;
        }

        this.viewConfig.startDate = startDate;
        this.viewConfig.endDate = endDate;
        this.initChart();
    }

    // 表示期間をリセット
    resetViewPeriod() {
        this.viewConfig.startDate = null;
        this.viewConfig.endDate = null;
        document.getElementById('viewStartDate').value = '';
        document.getElementById('viewEndDate').value = '';
        this.initChart();
    }

    // データに期間を合わせる
    fitToData() {
        if (this.ganttData.length === 0) return;

        const dates = this.ganttData.flatMap(task => [
            new Date(task.startDate),
            new Date(task.endDate)
        ]);

        const minDate = new Date(Math.min(...dates));
        const maxDate = new Date(Math.max(...dates));

        // 少し余裕を持たせる
        minDate.setDate(minDate.getDate() - 2);
        maxDate.setDate(maxDate.getDate() + 2);

        this.viewConfig.startDate = minDate.toISOString().split('T')[0];
        this.viewConfig.endDate = maxDate.toISOString().split('T')[0];

        document.getElementById('viewStartDate').value = this.viewConfig.startDate;
        document.getElementById('viewEndDate').value = this.viewConfig.endDate;

        this.initChart();
    }

    // ズームイン
    zoomIn() {
        if (this.chart) {
            this.chart.zoom(1.2);
        }
    }

    // ズームアウト
    zoomOut() {
        if (this.chart) {
            this.chart.zoom(0.8);
        }
    }

    // ズームリセット
    resetZoom() {
        if (this.chart) {
            this.chart.resetZoom();
        }
    }

    // プリセット表示期間を設定
    setViewPreset(preset) {
        const today = new Date();
        let startDate, endDate;

        switch (preset) {
            case 'week':
                startDate = new Date(today);
                startDate.setDate(today.getDate() - today.getDay());
                endDate = new Date(startDate);
                endDate.setDate(startDate.getDate() + 6);
                break;
            case 'month':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = new Date(today.getFullYear(), today.getMonth() + 1, 0);
                break;
            case 'quarter':
                const quarter = Math.floor(today.getMonth() / 3);
                startDate = new Date(today.getFullYear(), quarter * 3, 1);
                endDate = new Date(today.getFullYear(), (quarter + 1) * 3, 0);
                break;
            case 'year':
                startDate = new Date(today.getFullYear(), 0, 1);
                endDate = new Date(today.getFullYear(), 11, 31);
                break;
        }

        this.viewConfig.startDate = startDate.toISOString().split('T')[0];
        this.viewConfig.endDate = endDate.toISOString().split('T')[0];

        document.getElementById('viewStartDate').value = this.viewConfig.startDate;
        document.getElementById('viewEndDate').value = this.viewConfig.endDate;

        // プリセットボタンのアクティブ状態を更新
        document.querySelectorAll('.preset-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-preset="${preset}"]`).classList.add('active');

        this.initChart();
    }

    // タスク一覧を更新
    updateTaskList() {
        const taskList = document.getElementById('taskList');
        if (!taskList) return;

        taskList.innerHTML = '';

        this.ganttData.forEach(task => {
            const taskItem = this.createTaskElement(task);
            taskList.appendChild(taskItem);
        });
    }

    // タスク要素を作成
    createTaskElement(task) {
        const taskItem = document.createElement('div');
        taskItem.className = 'task-item';
        
        const duration = this.calculateDuration(task.startDate, task.endDate);
        
        taskItem.innerHTML = `
            <div class="task-info">
                <div class="task-name">${task.taskName}</div>
                <div class="task-details">
                    ${task.startDate} ～ ${task.endDate} (${duration}日間)
                    <span class="assignee-badge">${task.assignee}</span>
                </div>
                <div class="progress-container">
                    <div class="progress-bar">
                        <div class="progress-fill" style="width: ${task.progress}%"></div>
                    </div>
                    <div class="progress-text">${task.progress}%</div>
                </div>
            </div>
            <div class="task-actions">
                <button class="btn-edit" onclick="ganttManager.editTask(${task.id})">編集</button>
                <button class="btn-delete" onclick="ganttManager.deleteTask(${task.id})">削除</button>
            </div>
        `;
        
        return taskItem;
    }

    // 期間を計算
    calculateDuration(startDate, endDate) {
        const start = new Date(startDate);
        const end = new Date(endDate);
        const diffTime = Math.abs(end - start);
        return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    }

    // 色の透明度を調整
    adjustOpacity(color, opacity) {
        // HEXカラーをRGBAに変換
        const hex = color.replace('#', '');
        const r = parseInt(hex.substr(0, 2), 16);
        const g = parseInt(hex.substr(2, 2), 16);
        const b = parseInt(hex.substr(4, 2), 16);
        return `rgba(${r}, ${g}, ${b}, ${opacity})`;
    }

    // タスクを追加
    async addTask() {
        const taskName = document.getElementById('taskName').value.trim();
        const startDate = document.getElementById('startDate').value;
        const endDate = document.getElementById('endDate').value;
        const progress = document.getElementById('progress').value;
        const assignee = document.getElementById('assignee').value.trim();

        // バリデーション
        if (!taskName || !startDate || !endDate) {
            alert('タスク名、開始日、終了日は必須です');
            return;
        }

        if (new Date(startDate) > new Date(endDate)) {
            alert('開始日は終了日より前の日付を選択してください');
            return;
        }

        try {
            this.setLoading(true);
            
            const response = await fetch('/api/tasks', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    taskName,
                    startDate,
                    endDate,
                    progress: parseInt(progress) || 0,
                    assignee: assignee || '未割当'
                })
            });

            if (response.ok) {
                const newTask = await response.json();
                this.ganttData.push(newTask);
                this.initChart();
                this.updateTaskList();
                this.clearForm();
                this.showNotification('タスクが正常に追加されました', 'success');
            } else {
                throw new Error('サーバーエラー');
            }
        } catch (error) {
            console.error('エラー:', error);
            this.showNotification('タスクの追加に失敗しました', 'error');
        } finally {
            this.setLoading(false);
        }
    }

    // タスクを削除
    async deleteTask(taskId) {
        if (!confirm('このタスクを削除しますか？')) {
            return;
        }

        try {
            this.setLoading(true);
            
            const response = await fetch(`/api/tasks/${taskId}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                this.ganttData = this.ganttData.filter(task => task.id !== taskId);
                this.initChart();
                this.updateTaskList();
                this.showNotification('タスクが削除されました', 'success');
            } else {
                throw new Error('サーバーエラー');
            }
        } catch (error) {
            console.error('エラー:', error);
            this.showNotification('タスクの削除に失敗しました', 'error');
        } finally {
            this.setLoading(false);
        }
    }

    // タスクを編集
    editTask(taskId) {
        const task = this.ganttData.find(t => t.id === taskId);
        if (!task) return;

        // 簡単な編集フォーム（実際のプロジェクトではモーダルを使用）
        const newTaskName = prompt('タスク名:', task.taskName);
        if (newTaskName === null) return;

        const newProgress = prompt('進捗 (%):', task.progress);
        if (newProgress === null) return;

        const newAssignee = prompt('担当者:', task.assignee);
        if (newAssignee === null) return;

        this.updateTaskData(taskId, {
            taskName: newTaskName || task.taskName,
            progress: parseInt(newProgress) || task.progress,
            assignee: newAssignee || task.assignee
        });
    }

    // タスクデータを更新
    async updateTaskData(taskId, updateData) {
        try {
            this.setLoading(true);
            
            const response = await fetch(`/api/tasks/${taskId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(updateData)
            });

            if (response.ok) {
                const updatedTask = await response.json();
                const taskIndex = this.ganttData.findIndex(task => task.id === taskId);
                if (taskIndex !== -1) {
                    this.ganttData[taskIndex] = updatedTask;
                    this.initChart();
                    this.updateTaskList();
                    this.showNotification('タスクが更新されました', 'success');
                }
            } else {
                throw new Error('サーバーエラー');
            }
        } catch (error) {
            console.error('エラー:', error);
            this.showNotification('タスクの更新に失敗しました', 'error');
        } finally {
            this.setLoading(false);
        }
    }

    // フォームをクリア
    clearForm() {
        document.getElementById('taskName').value = '';
        document.getElementById('startDate').value = '';
        document.getElementById('endDate').value = '';
        document.getElementById('progress').value = '0';
        document.getElementById('assignee').value = '';
    }

    // ローディング状態を設定
    setLoading(isLoading) {
        const container = document.querySelector('.container');
        if (isLoading) {
            container.classList.add('loading');
        } else {
            container.classList.remove('loading');
        }
    }

    // 通知を表示
    showNotification(message, type = 'info') {
        // 簡単な通知実装（実際のプロジェクトではトーストライブラリを使用）
        const notification = document.createElement('div');
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            background: ${type === 'success' ? '#2ecc71' : type === 'error' ? '#e74c3c' : '#3498db'};
            color: white;
            border-radius: 5px;
            z-index: 1000;
            box-shadow: 0 2px 10px rgba(0,0,0,0.2);
        `;
        notification.textContent = message;
        document.body.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 3000);
    }
}

// グローバルインスタンス
let ganttManager;

// ページ読み込み時に初期化
document.addEventListener('DOMContentLoaded', function() {
    ganttManager = new GanttChartManager();
});