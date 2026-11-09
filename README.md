# 🎮 AI 小镇模拟游戏 (Unity 客户端)

## 📖 项目简介
本项目为“AI 小镇”多智能体模拟游戏的 **Unity 客户端**。负责渲染虚拟小镇场景、控制 NPC 移动（A* 寻路）以及提供与玩家交互的 UI 界面。

## 🛠️ 技术栈
*   **游戏引擎**：Unity (C#)
*   **寻路算法**：A* 算法（用于 NPC 自主避障与移动）
*   **通信方式**：WebSocket无状态通信

## 🚀 快速运行指南
1. 使用 **Unity Hub** 打开本项目根目录（确保 `Assets` 和 `ProjectSettings` 文件夹存在）。
2. 等待 Unity 导入资源完毕。
3. 打开主场景（通常在 `Assets/Scenes` 目录下）。
4. 点击顶部的 **Play** 按钮运行游戏。

## 🔗 配套服务端
本客户端需要配合 Python AI 服务端使用，负责处理智能体的大模型决策。
服务端仓库地址：[https://gitee.com/hackkid/aitown-python.git](https://gitee.com/hackkid/aitown-python.git)

## 🔒 安全说明
已配置 `.gitignore` 排除 Unity 编译缓存（Library、Temp 等），仓库仅包含纯源码与资源文件。