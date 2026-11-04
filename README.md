# DayHop — Android Travel Application

A C# Android travel application for exploring destinations and activities, browsing place information, and saving places to a personal list.

## Project overview

**DayHop** is a mobile application project developed with **C#** and an **Android interface defined in XML**. The app provides location and keyword inputs, place-information cards, navigation between results, and a saved-list screen.

## Features

- **Welcome screen:** A DayHop introduction screen with a swipe-up prompt.
- **Location input:** Enter a location to focus travel exploration.
- **Keyword search interface:** Search for places or activities using keywords.
- **Place cards:** Display a place image, title, category, distance field, and description.
- **Browse controls:** Move between displayed places with **Back** and **Next** buttons.
- **Saved list:** Save places to a wishlist and open a dedicated saved-list screen.

> Feature descriptions reflect the provided Android layouts and project structure. Runtime behavior and third-party data integrations have not been independently verified.

## Technologies

- **C#** — application logic and Android activities
- **.NET for Android** — Android application project
- **XML** — Android layouts, manifest, and resource definitions
- **CSV** — project data files (`activities.csv`, `cityinformation.csv`)
- **Visual Studio** — IDE for opening and building the solution

## Project structure

```text
Android-Travel-Application/
├── AndroidApp/
│   ├── AndroidApp.sln
│   ├── AndroidApp.csproj
│   ├── AndroidManifest.xml
│   ├── LockActivity.cs
│   ├── MainActivity.cs
│   ├── SavedListActivity.cs
│   └── Resources/
│       ├── layout/
│       │   ├── activity_lock.xml
│       │   ├── activity_main.xml
│       │   └── activity_saved_list.xml
│       ├── mipmap-anydpi-v26/
│       │   ├── appicon.xml
│       │   └── appicon_round.xml
│       ├── mipmap-hdpi/
│       │   ├── appicon.png
│       │   ├── appicon_background.png
│       │   └── appicon_foreground.png
│       └── values/
│           ├── ic_launcher_background.xml
│           └── strings.xml
├── activities.csv
├── cityinformation.csv
└── .gitignore
```

## Getting started

1. Clone the repository:

   ```bash
   git clone https://github.com/pdthao/Android-Travel-Application.git
   ```

2. Open `AndroidApp/AndroidApp.sln` in **Visual Studio** with the appropriate **.NET for Android** development tools installed.
3. Restore any required dependencies and check the target Android SDK and framework in `AndroidApp.csproj`.
4. Build and run the app on an Android emulator or a connected Android device.

> The build requirements and execution steps may need adjustment to match the versions and dependencies specified in the project file.

## App screens

| Screen | Files | Purpose |
| --- | --- | --- |
| Welcome | `LockActivity.cs`, `activity_lock.xml` | DayHop introduction |
| Explore | `MainActivity.cs`, `activity_main.xml` | Location and keyword inputs, place cards, browsing |
| Saved places | `SavedListActivity.cs`, `activity_saved_list.xml` | View saved places |

## Project data

- `activities.csv` — activity dataset included with the repository
- `cityinformation.csv` — city information dataset included with the repository

## Future improvements

- Add application screenshots and a short demonstration video.
- Document supported Android versions and exact build dependencies.
- Improve data handling and destination discovery as the project evolves.

## Author

** Phuong Dieu Thao (Jerry) - 芳妙草 **  
GitHub: [@pdthao](https://github.com/pdthao)

## Note

This repository is shared as a software development and portfolio project. Please check the project files for confidential information before publishing changes.
