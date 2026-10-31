# ShaderStripMod

Stable **1.29.30** · Hollow Knight (Modding API) · Windows-first

A capture and visual-isolation tool for Hollow Knight: chroma-key backgrounds, selected scene objects, title-menu media and a temporary gameplay-camera pin.

**English** · [Русский](#русский)

## English

### What it does

ShaderStripMod is not a cosmetic asset swap. It works with the live Unity scene:

- **Chroma key / green screen.** Hides most of the scene and keeps the hero, attacks and selected objects visible.
- **Background replacement.** Uses a solid color or a PNG/JPG image in gameplay, and PNG/JPG/GIF/video in the title menu.
- **Texture browser (F9).** Browses textures, sprites, materials and renderer objects; adds preserve rules and opens sprite/atlas masks.
- **Preserve rules.** Forces individual renderers, objects, object paths or projectile families to stay visible.
- **Menu design.** Custom menu background, logo and button text style.
- **Camera pin (F8).** Freezes the gameplay camera at its current position while the hero keeps moving. The pin is temporary and is released on room change, death, transitions, cutscenes or unload. F12 only disables the visual mode and keeps the pin, and F8 is ignored while F9 is open.
- **Diagnostics.** F10 dumps loaded resources, F11 reloads rules, exports and performance profiles are available in F9.

It is intended for screenshots, video, streams and visual projects, not for a normal playthrough.

### Requirements

- Hollow Knight with the [Hollow Knight Modding API](https://github.com/hk-modding/api) installed (for example through Lumafly or Scarab).
- Tested against the Steam build installed on 2026-10-11. The exact game/API build number was not recorded, so newer game or API versions are unverified.
- Windows for the background/logo/media import dialog. Other platforms are untested and the import picker is unavailable there.

### Installation

1. Install the Modding API using your mod manager.
2. Close the game.
3. Copy `ShaderStripMod.dll` into:
   `...\Hollow Knight\hollow_knight_Data\Managed\Mods\ShaderStripMod\`
   (create the `ShaderStripMod` folder if it does not exist).
4. Start the game.

The mod is not yet listed in ModLinks. After it is listed, installing and updating through Lumafly will be the recommended route.

The release ZIP contains only `ShaderStripMod.dll`, `README.md`, `CHANGELOG.md` and `LICENSE`. It does not contain QA logs, screenshots, user settings or media.

### Controls

| Key | Action |
| --- | --- |
| `F8` | Pin / unpin the gameplay camera at its current position. Ignored while F9 is open. |
| `F9` | Open / close the texture browser. The browser pauses the game by default. |
| `F10` | Dump loaded sprites, textures, materials and renderers. |
| `F11` | Reload `ShaderStripMod.preserve.txt` and `ShaderStripMod.visual-rules.txt`. |
| `F12` | Enable / disable the mod's visual mode. The camera pin stays. |

Inside F9, `Esc` closes the browser. The import controls and the camera/pause switches are on the Settings tab. Sprite masks are opened from the Nearby tab.

### Runtime files

The mod writes next to its DLL, or to `Application.persistentDataPath` if that folder is not writable:

```text
ShaderStripMod.settings.txt        UI, export, performance and visual settings
ShaderStripMod.preserve.txt        preserve rules (manual work, back it up)
ShaderStripMod.visual-rules.txt    optional visual rules (formatVersion=1)
ShaderStripMod.sprite-dump.txt     created by F10
background-images/                 imported gameplay background images
menu-media/                        imported menu images, GIF and video
sprite-masks/                      saved sprite/atlas masks
replacements/                      user replacement images
exports/                           PNG exports and metadata
```

Deleted background and menu media is moved to a `removed` subfolder rather than erased. F9 → Settings (images) and F9 → Menu design (menu media) list the removed files with their size and offer Restore, Delete permanently and Empty trash. Both Delete permanently and Empty trash require a second confirmation. Restore never overwrites an existing file.

### Known limitations

- **Gameplay coverage is incomplete.** The last live run covered the title menu, F9, PNG/GIF/MP4 H.264, pause and input at 1360×768 and 800×600. It did not open a save or a gameplay room. Camera pin behavior in real rooms, during deaths, transitions and cutscenes was checked with Unity/game API substitutions. The 1.29.30 camera, F12 and transition changes have not been run in a live session yet.
- **GPU coverage.** The live run used software DirectX WARP. Hardware GPUs and other video codecs are unverified.
- **Windows-only import dialog.** The background/logo/media picker uses Windows APIs.
- **Preserve rules are heuristic.** A rule targets a renderer, sprite region or object path, not a semantic object type. Shared atlases can produce false matches. Renderer/particle limits can skip objects that match a broad rule; F9 → Performance and F9 → Rules now report the counts and reasons. Diagnostics are invalidated on scene changes, rule changes and `F12` toggles; while visuals are off, the selected-rule report is explicitly marked as a dry scan.
- **Settings are rewritten on save.** Unknown future keys or manual comments may be lost. Back up `ShaderStripMod.settings.txt` and `ShaderStripMod.preserve.txt` before upgrading.
- **Camera keys.** While F9 is open, `F8` is ignored; use F9 → Settings to pin or release the camera. `F12` keeps the camera pin and only restores the visual mode.
- **No cross-platform promise.** The Modding API is cross-platform, but this mod's file picker and live behavior are only supported on Windows.

### Uninstall

1. Close the game.
2. Delete the `Managed\Mods\ShaderStripMod` folder.
3. Optionally delete the runtime files and folders listed above.

### Build from source

Prerequisites: Windows, .NET SDK with the .NET Framework 4.7.2 targeting pack, and a Hollow Knight installation with the Modding API.

Point the build at the game's managed DLLs in either way:

```powershell
# Option A: local ignored copy
# Copy the contents of hollow_knight_Data\Managed into HollowKnightManaged\
dotnet build ShaderStripMod.csproj -c Release

# Option B: explicit path
dotnet build ShaderStripMod.csproj -c Release -p:HollowKnightRefs="C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\"
```

The project fails with a clear message if `Assembly-CSharp.dll` cannot be found. Build output is `bin\Release\ShaderStripMod.dll` and `.pdb`.

The build environment is not pinned. A later rebuild can have a different module version id (MVID) and PDB checksum even though the method bodies are unchanged. The release ZIP ships the audited 1.29.30 binary.

Run the automatic checks:

```powershell
dotnet run --project Tests/RegressionChecks.csproj -c Release
dotnet run --project Tests/MenuResourcesChecks.csproj -c Release
dotnet run --project Tests/MenuMediaChecks.csproj -c Release
dotnet run --project Tests/TexturePreviewChecks.csproj -c Release
dotnet run --project Tests/PersistenceChecks.csproj -c Release
dotnet run --project Tests/BrowserInputGateChecks.csproj -c Release
dotnet run --project Tests/MenuBackgroundDecorationsChecks.csproj -c Release
dotnet run --project Tests/CameraPinChecks.csproj -c Release
```

Stable 1.29.30 passes 157,583 assertions across these eight projects. Most of them are pixel comparisons for GIF and preview code; they are not a substitute for a live gameplay test.

### Reporting issues

Describe the game version, mod version, scene, steps and whether the issue happens with other mods disabled. Do not post an unredacted `Player.log` or `ModLog.txt`: they can contain local paths, your Steam user name and a list of installed mods.

### License

MIT. See [LICENSE](LICENSE).

## Русский

### Что делает мод

ShaderStripMod — инструмент для съёмки и визуального выделения в Hollow Knight, а не обычная замена косметических ассетов. Он работает с живой сценой Unity:

- **Chroma key / зелёный фон.** Скрывает большую часть сцены, оставляя героя, атаки и выбранные объекты.
- **Замена фона.** Однотонный цвет или PNG/JPG в игре; PNG/JPG/GIF/видео в титульном меню.
- **Браузер текстур (F9).** Просмотр текстур, спрайтов, материалов и renderer-объектов, добавление preserve-правил и масок спрайта/атласа.
- **Preserve-правила.** Принудительно оставляют видимыми отдельные renderer-объекты, пути объектов, семейства снарядов и т. п.
- **Оформление меню.** Свой фон, логотип и стиль текста кнопок.
- **Фиксация камеры (F8).** Замораживает игровую камеру в текущей точке, пока герой продолжает двигаться. Фиксация временная: снимается при смене комнаты, смерти, переходах, катсценах и выгрузке мода. F12 выключает только визуальный режим и сохраняет фиксацию, а при открытом F9 клавиша F8 игнорируется.
- **Диагностика.** F10 сохраняет дамп загруженных ресурсов, F11 перечитывает правила; экспорт и профили производительности доступны в F9.

Мод предназначен для скриншотов, видео, стримов и визуальных проектов, а не для обычного прохождения.

### Требования

- Hollow Knight с установленным [Hollow Knight Modding API](https://github.com/hk-modding/api) (например, через Lumafly или Scarab).
- Проверено на Steam-сборке, установленной 2026-10-11. Точный номер сборки игры/API не зафиксирован, поэтому более новые версии не проверены.
- Windows для диалога импорта фона/логотипа/медиа. Другие платформы не тестировались, и выбор файлов там недоступен.

### Установка

1. Установите Modding API через менеджер модов.
2. Закройте игру.
3. Скопируйте `ShaderStripMod.dll` в:
   `...\Hollow Knight\hollow_knight_Data\Managed\Mods\ShaderStripMod\`
   (папку `ShaderStripMod` создайте, если её нет).
4. Запустите игру.

Мод пока не добавлен в ModLinks. После добавления установка и обновление через Lumafly будут рекомендованным способом.

Релизный ZIP содержит только `ShaderStripMod.dll`, `README.md`, `CHANGELOG.md` и `LICENSE`. В нём нет QA-логов, скриншотов, пользовательских настроек и медиа.

### Управление

| Клавиша | Действие |
| --- | --- |
| `F8` | Закрепить / открепить игровую камеру в текущей точке. Игнорируется при открытом F9. |
| `F9` | Открыть / закрыть браузер текстур. По умолчанию ставит игру на паузу. |
| `F10` | Дамп загруженных спрайтов, текстур, материалов и renderer-объектов. |
| `F11` | Перечитать `ShaderStripMod.preserve.txt` и `ShaderStripMod.visual-rules.txt`. |
| `F12` | Включить / выключить визуальный режим мода. Фиксация камеры сохраняется. |

Внутри F9 `Esc` закрывает браузер. Импорт и переключатели камеры/паузы находятся во вкладке Settings. Маски спрайта открываются из вкладки Nearby.

### Runtime-файлы

Мод пишет рядом со своей DLL, а если папка недоступна — в `Application.persistentDataPath`:

```text
ShaderStripMod.settings.txt        настройки интерфейса, экспорта и производительности
ShaderStripMod.preserve.txt        preserve-правила (ручная работа, делайте бэкап)
ShaderStripMod.visual-rules.txt    необязательные visual rules (formatVersion=1)
ShaderStripMod.sprite-dump.txt     создаётся по F10
background-images/                 импортированные фоновые изображения игры
menu-media/                        изображения, GIF и видео меню
sprite-masks/                      сохранённые маски спрайтов/атласов
replacements/                      пользовательские изображения замен
exports/                           PNG-экспорты и metadata
```

Удалённые фон и медиа меню перемещаются в подпапку `removed`, а не стираются. F9 → Settings (изображения) и F9 → Menu design (медиа меню) показывают список удалённых файлов с размером и кнопками Restore, Delete permanently и Empty trash. И Delete permanently, и Empty trash требуют повторного подтверждения. Restore не перезаписывает существующий файл.

### Известные ограничения

- **Игровые сцены проверены не полностью.** Последний живой прогон охватил титульное меню, F9, PNG/GIF/MP4 H.264, паузу и ввод при 1360×768 и 800×600. Сохранение и игровая комната не открывались. Поведение фиксации камеры в реальных комнатах, при смерти, переходах и катсценах проверялось заменами Unity/game API. Изменения 1.29.30 для камеры, F12 и переходов ещё не проверены в живой игровой сессии.
- **GPU.** Живой прогон использовал программный DirectX WARP. Аппаратные GPU и другие видеокодеки не проверены.
- **Импорт только в Windows.** Выбор фона, логотипа и медиа использует Windows API.
- **Preserve-правила эвристические.** Правило нацелено на renderer, область спрайта или путь объекта, а не на смысловой тип объекта. Общие атласы дают ложные совпадения. Лимиты renderer/particle-источников могут пропустить объекты, подходящие под широкое правило; F9 → Performance и F9 → Rules теперь показывают количества и причины. Диагностика сбрасывается при смене сцены, изменении правил и выключении визуального режима; при выключенном F12 отчёт явно помечен как сухая проверка.
- **Настройки перезаписываются при сохранении.** Неизвестные будущие ключи и ручные комментарии могут потеряться. Перед обновлением сохраните копии `ShaderStripMod.settings.txt` и `ShaderStripMod.preserve.txt`.
- **Клавиши камеры.** При открытом F9 клавиша `F8` игнорируется; фиксация включается и снимается кнопкой в F9 → Settings. `F12` сохраняет фиксацию камеры и только восстанавливает визуальный режим.
- **Кроссплатформенность не обещается.** Modding API кроссплатформенный, но выбор файлов и живое поведение этого мода поддерживаются только на Windows.

### Удаление

1. Закройте игру.
2. Удалите папку `Managed\Mods\ShaderStripMod`.
3. При желании удалите runtime-файлы и папки из списка выше.

### Сборка из исходников

Требования: Windows, .NET SDK с targeting pack .NET Framework 4.7.2 и установленный Hollow Knight с Modding API.

Укажите путь к managed-DLL игры одним из способов:

```powershell
# Вариант A: локальная игнорируемая копия
# Скопируйте содержимое hollow_knight_Data\Managed в HollowKnightManaged\
dotnet build ShaderStripMod.csproj -c Release

# Вариант B: явный путь
dotnet build ShaderStripMod.csproj -c Release -p:HollowKnightRefs="C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\"
```

Если `Assembly-CSharp.dll` не найден, проект завершится с понятным сообщением. Результат сборки — `bin\Release\ShaderStripMod.dll` и `.pdb`.

Окружение сборки не зафиксировано. Пересборка из этих исходников может получить другой MVID и контрольную сумму PDB при тех же телах методов. В релизный ZIP входит проверенный бинарник 1.29.30.

Автоматические проверки:

```powershell
dotnet run --project Tests/RegressionChecks.csproj -c Release
dotnet run --project Tests/MenuResourcesChecks.csproj -c Release
dotnet run --project Tests/MenuMediaChecks.csproj -c Release
dotnet run --project Tests/TexturePreviewChecks.csproj -c Release
dotnet run --project Tests/PersistenceChecks.csproj -c Release
dotnet run --project Tests/BrowserInputGateChecks.csproj -c Release
dotnet run --project Tests/MenuBackgroundDecorationsChecks.csproj -c Release
dotnet run --project Tests/CameraPinChecks.csproj -c Release
```

Стабильная 1.29.30 проходит 157 583 assertion в этих восьми проектах. Большинство — попиксельные сравнения GIF и preview, они не заменяют живой игровой тест.

### Сообщения об ошибках

Укажите версию игры, версию мода, сцену, шаги и воспроизводится ли проблема с отключёнными другими модами. Не публикуйте неотредактированные `Player.log` и `ModLog.txt`: в них могут быть локальные пути, имя пользователя Steam и список установленных модов.

### Лицензия

MIT. См. [LICENSE](LICENSE).
