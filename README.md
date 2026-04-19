# 自律桌面伴侣 (SelfDisciplineMate)

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-0078d7.svg)
![Framework](https://img.shields.io/badge/framework-.NET%208.0%20WPF-512bd4.svg)

**自律桌面伴侣** 是一款专为改善个人工作效率与自律习惯而设计的 Windows 全能桌面工具。它通过将“任务完成度”与“娱乐软件访问权限”深度挂钩，通过系统级拦截技术，强制用户在获得娱乐之前先完成工作。

---

## 🌟 核心理念：将自律“游戏化”

本项目的设计核心在于一套**动态权限评估系统**。你的娱乐权限并非恒定不变，而是取决于你对自己的要求：
- **奖励勤奋**：今日任务 100% 完成，无需顾虑昨日表现，立即解锁最高权限。
- **惩罚懈怠**：若昨日完成率低，今日的大型游戏（如守望先锋、Steam）将由于“昨日债务”而被系统级锁死，直到你通过完成今日任务来“赎回”权限。

---

## 🚀 核心功能描述

### 1. 智能任务管理 (Weight-based Tasks)
- **权重系统**：每个任务可以设置 1-5 星的权重。进度计算不仅仅是“完成个数”，而是基于权重的总和。
- **持久化模板**：每日凌晨根据任务模板自动生成今日清单，支持软删除与排序。

### 2. 分级娱乐权限 (Tiered Permission System)
系统根据**昨日完成率**确定初始权限，并根据**今日实时进度**动态调整：
- **Blender 级 (20%+)**: 允许基础创作/办公工具。
- **Godot/UE 级 (40%~60%)**: 允许开发与学习。
- **Steam/Minecraft/守望先锋级 (70%~90%+)**: 逐步解锁重型娱乐。
- **今日全勤奖励**：一旦今日任务全部清零，系统将无视昨日表现，解锁全部权限。

### 3. 系统级进程拦截 (IFEO 阻断)
不同于普通的“进程轮询杀死（Process Killer）”，本项目采用 **Image File Execution Options (IFEO)** 注册表注入技术：
- **更平滑、更强效**：在用户双击运行 exe 的瞬间，系统会直接将其重定向到拦截通知界面。
- **低开销**：无需后台频繁扫描进程列表，极度节省 CPU 与内存。
- **拦截通知**：弹出美观的任务提醒窗口，告知用户还需完成多少任务才能运行此程序。

### 4. 深度集成的番茄钟 (Pomodoro)
- 内置专注计时器，支持自定义专注、短休、长休时长。
- 支持番茄完成数统计，并与主窗口消息交互，在专注结束后给予正向语言激励。

### 5. 历史足迹 (Calendar Statistics)
- **日历视图**：通过色彩与数据悬浮窗记录每一天的奋斗轨迹。
- **复盘功能**：查看过去一个月的任务完成率曲线与番茄钟总数。

### 6. 极致的桌面体验
- **托盘常驻**：关闭窗口不退出程序，保持常驻守护。
- **自动启动**：支持开机自启动配置，成为你的桌面管家。

---

## 🎨 设计与交互逻辑


### 架构设计 (MVVM)
- 基于 **.NET 8** 与 **WPF** 开发。
- **SQLite 数据库**：采用轻量级本地数据库存储任务、日志与番茄数据。
- **MVVM Toolkit**：使用 `CommunityToolkit.Mvvm` 进行组件化开发。
- **弱引用消息通信**：各窗口（番茄钟、主界面、设置）通过消息总线进行解耦状态同步。

---

## 🛠️ 技术栈
- **核心框架**: .NET 8.0 Windows Desktop
- **UI**: WPF (XML/C#)
- **数据库**: SQLite 
- **通信**: MVVM Toolkit Messager
- **注册表操作**: Microsoft.Win32.Registry (用于拦截与自启动)

---

## 📂 快速预览项目结构
- `Models/`: 数据模型（任务、模板、日志）。
- `ViewModels/`: 包含主界面逻辑、设置逻辑。
- `Services/`: 包含数据库访问、进程拦截核心、权限判定逻辑。
- `BlockNotifier/`: 被拦截时弹出的极简通知应用。

---

## 📝 开源协议
本项目采用 [MIT License](LICENSE) 协议。



---


**Self-Discipline Desktop Companion** is an all-in-one Windows desktop tool designed specifically to improve personal work efficiency and self-discipline habits. By deeply linking "task completion rate" with "access permissions to entertainment software", it uses system-level interception technology to force users to finish their work before gaining access to entertainment programs.

---

## 🌟 Core Concept: Gamifying Self-Discipline

The core design of this project lies in a **dynamic permission evaluation system**. Your entertainment permissions are not fixed; instead, they depend on your own requirements:
- **Reward Diligence**: Complete 100% of today’s tasks, and you will immediately unlock the highest permissions regardless of your performance the previous day.
- **Penalize Laxity**: If your completion rate was low yesterday, today’s access to major games (such as Overwatch, Steam) will be system-locked due to "yesterday’s debt", until you "redeem" the permissions by finishing today’s tasks.

---

## 🚀 Core Features

### 1. Intelligent Task Management (Weight-based Tasks)
- **Weight System**: Each task can be assigned a weight of 1 to 5 stars. Progress is calculated not merely by the "number of completed tasks", but by the total weighted value.
- **Persistent Templates**: Today’s to-do list is automatically generated from task templates every early morning, supporting soft deletion and sorting.

### 2. Tiered Entertainment Permission System
The system determines initial permissions based on **yesterday’s completion rate** and dynamically adjusts them according to **today’s real-time progress**:
- **Blender Level (20%+)**: Access to basic creation and office tools is allowed.
- **Godot/UE Level (40%~60%)**: Access to development and learning software is allowed.
- **Steam/Minecraft/Overwatch Level (70%~90%+)**: Heavy entertainment programs are gradually unlocked.
- **Perfect Attendance Reward**: Once all tasks for the day are completed, the system ignores previous day’s performance and unlocks all permissions.

### 3. System-level Process Interception (IFEO Blocking)
Unlike ordinary "Process Killer" tools that poll and terminate processes, this project adopts **Image File Execution Options (IFEO)** registry injection technology:
- **Smoother & More Effective**: The moment a user double-clicks to run an executable file, the system directly redirects it to an interception notification interface.
- **Low Overhead**: No frequent background scanning of process lists, greatly saving CPU and memory usage.
- **Interception Notifications**: An elegant task reminder window pops up, informing users how many more tasks need to be completed to run the program.

### 4. Deeply Integrated Pomodoro Timer
- Built-in focus timer supporting custom durations for focus sessions, short breaks, and long breaks.
- Tracks the number of completed Pomodoro sessions, interacts with the main window, and provides positive verbal encouragement after each focus session ends.

### 5. Historical Track (Calendar Statistics)
- **Calendar View**: Records daily progress using colors and data tooltips.
- **Review Function**: Views the task completion rate curve and total Pomodoro count for the past month.

### 6. Ultimate Desktop Experience
- **Tray Persistence**: The program remains running in the system tray when the window is closed, acting as a persistent guardian.
- **Auto-Startup**: Supports configuration for automatic launch on system startup, serving as your personal desktop steward.

---

## 🎨 Design & Interaction Logic

### Architecture Design (MVVM)
- Developed based on **.NET 8** and **WPF**.
- **SQLite Database**: A lightweight local database used to store tasks, logs, and Pomodoro data.
- **MVVM Toolkit**: Component-based development using `CommunityToolkit.Mvvm`.
- **Weak-Reference Messaging**: Windows (Pomodoro, main interface, settings) synchronize states in a decoupled manner via a message bus.

---

## 🛠️ Tech Stack
- **Core Framework**: .NET 8.0 Windows Desktop
- **UI**: WPF (XML/C#)
- **Database**: SQLite
- **Communication**: MVVM Toolkit Messager
- **Registry Operations**: Microsoft.Win32.Registry (for interception and auto-startup)

---

## 📂 Quick Project Structure Overview
- `Models/`: Data models (tasks, templates, logs).
- `ViewModels/`: Contains main interface logic and settings logic.
- `Services/`: Includes database access, core process interception, and permission judgment logic.
- `BlockNotifier/`: Minimalist notification application that pops up when access is blocked.

---

This project is licensed under the [MIT License](LICENSE).
