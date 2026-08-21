# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-08-21

### Changed

- Raised minimum Unity version to **6000.3** (Unity 6.3 LTS). The Main Toolbar extension API is not available in 6.0–6.2.

## [1.0.0] - 2026-08-21

### Added

- Play From First Scene button on the Unity Main Toolbar (opens Build Settings scene index 0, then enters Play Mode; restores the previous scene on exit).
- Scene Switcher dropdown listing scenes from Build Settings (with project-wide scene fallback).
- Helper that forces custom Main Toolbar elements visible after install (Unity 6.3+).
- Editor-only assembly definition (`UnitySceneToolbar.Editor`).

[1.0.1]: https://github.com/makarGames/Unity-Toolbar-Scene-Selector/releases/tag/v1.0.1
[1.0.0]: https://github.com/makarGames/Unity-Toolbar-Scene-Selector/releases/tag/v1.0.0
