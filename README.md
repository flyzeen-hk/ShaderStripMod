# ShaderStripMod

Stable **1.31.1** · testing build · Hollow Knight (Modding API) · Windows-first

A capture and visual-isolation tool for Hollow Knight: chroma-key backgrounds, selected scene objects, title-menu media, a full-screen media collage and a temporary gameplay-camera pin.

**English** · [Русский](#русский)

**Download:** [ShaderStripMod 1.29.30 — release notes and ZIP](https://github.com/flyzeen-hk/ShaderStripMod/releases/tag/v1.29.30). The public build is a pre-release: live gameplay testing is still pending. The 1.29.31-1.31.1 input, video-sound, media-collage, hotkey, interface and capture-mode patches are installed locally for testing and are not published yet.

## English

### What it does

ShaderStripMod is not a cosmetic asset swap. It works with the live Unity scene:

- **Chroma key / green screen.** Hides most of the scene and keeps the hero, attacks and selected objects visible.
- **Background replacement.** Uses a solid color or a PNG/JPG image in gameplay, and PNG/JPG/GIF/video in the title menu.
- **Texture browser (F9 by default).** Browses textures, sprites, materials and renderer objects; adds preserve rules and opens sprite/atlas masks.
- **Preserve rules.** Forces individual renderers, objects, object paths or projectile families to stay visible.
- **Menu design.** Custom menu background, logo and button text style, with optional video sound. Menu design and gameplay isolation are independent switches.
- **Media collage (F9 → Panels).** Draws PNG/JPG/GIF images and videos as zero-gap tiles over the full-screen gameplay, with eight preset layouts (full screen, two halves, two rows, three columns, three rows, main + side, main + bottom, 2×2 grid). The hero and preserved objects always render above the collage, and chroma-key/preserve behavior keeps working. Tiles resize with the window and use Cover, Contain or Stretch placement. Up to four tiles can each have their own pause, sound, mute and volume. The preset is saved to `ShaderStripMod.composition.txt`; Save, Reload and Reset are in the Panels tab. Requires gameplay isolation and a gameplay room with a live hero; it pauses during transitions and releases the command buffer and media on scene change. Empty tiles show the background color. An unsupported preset `formatVersion` is refused and the file is never overwritten; the tab shows whether autosave succeeded. Video tiles report a missing audio track.
- **Camera pin (F8 by default).** Freezes the gameplay camera at its current position while the hero keeps moving. The pin is temporary and is released on room change, death, transitions, cutscenes or unload. The isolation hotkey only disables gameplay isolation and keeps the pin, and the camera-pin hotkey is ignored while the browser is open.
- **Diagnostics.** The dump and reload hotkeys (F10 and F11 by default) write loaded resources and reload rules; exports and performance profiles are available in F9.
- **Capture status (F9 → Settings).** A read-only summary shows gameplay isolation, title menu design, camera pin state, preserve matches and limits, and collage state, so you can see why an object is hidden before changing rules.
- **Capture modes and presets (F9 → Settings).** Four modes change what the capture shows: Selected objects (your preserve rules apply), Hero + attacks (hero and attack effects; preserve rules are suspended, not deleted), Hero only (hides attack effects and suspends preserve rules) and Full scene (the original room with gameplay isolation off). A preset stores a snapshot of the current preserve rules plus the isolation flag in `ShaderStripMod.capture-presets.txt`; type a name and use Save, Apply or Delete. Applying backs up `ShaderStripMod.preserve.txt` first; replacing or deleting a preset backs up the presets file. Apply, replace and delete are transactional: a failed backup or write leaves both the rules and the presets file unchanged. Masks, imported images, media and collage settings are not part of a preset.
- **Configurable hotkeys (F9 → Hotkeys).** Every mod action can use one keyboard key. Changes are saved immediately to `ShaderStripMod.hotkeys.txt`. Esc, Enter, Tab and arrow keys are reserved; conflicts are checked only between the mod's own actions.

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

The mod is not yet listed in ModLinks. Download the current pre-release from the [v1.29.30 release page](https://github.com/flyzeen-hk/ShaderStripMod/releases/tag/v1.29.30). The 1.31.1 testing build is not published yet. After the mod is listed in ModLinks, installing and updating through Lumafly will be the recommended route.

The release ZIP contains only `ShaderStripMod.dll`, `README.md`, `CHANGELOG.md` and `LICENSE`. It does not contain QA logs, screenshots, user settings or media.

### Controls

| Key | Action |
| --- | --- |
| `F8` | Pin / unpin the gameplay camera at its current position. Ignored while the browser is open. |
| `F9` | Open / close the texture browser. The browser pauses the game by default. |
| `F10` | Dump loaded sprites, textures, materials and renderers. |
| `F11` | Reload `ShaderStripMod.preserve.txt` and `ShaderStripMod.visual-rules.txt`. |
| `F12` | Enable / disable gameplay isolation. The camera pin and title menu design stay. |

The table shows defaults. All five actions can be reassigned in F9 → Hotkeys; a changed key also changes the labels shown in the browser and the generated files. Inside F9, `Esc` closes the browser unless a key capture is active, where it cancels the capture. While the browser is open, the other hotkeys do not fire from text fields or other browser controls. The import controls, gameplay/menu toggles and the camera/pause switches are on the Settings tab; menu video sound is in the Menu design tab; media collage is in the Panels tab. Sprite masks are opened from the Nearby tab.

### Runtime files

The mod writes next to its DLL, or to `Application.persistentDataPath` if that folder is not writable:

```text
ShaderStripMod.settings.txt        UI, export, performance and visual settings
ShaderStripMod.preserve.txt        preserve rules (manual work, back it up)
ShaderStripMod.visual-rules.txt    optional visual rules (formatVersion=1)
ShaderStripMod.composition.txt     media collage layout, placement and tile media settings (formatVersion=2)
ShaderStripMod.hotkeys.txt         configurable hotkey bindings (formatVersion=1)
ShaderStripMod.capture-presets.txt saved capture presets: preserve-rule snapshots and isolation flag (formatVersion=1)
ShaderStripMod.sprite-dump.txt     created by the dump hotkey
background-images/                 imported gameplay background images
menu-media/                        imported menu images, GIF and video
sprite-masks/                      saved sprite/atlas masks
replacements/                      user replacement images
exports/                           PNG exports and metadata
```

Deleted background and menu media is moved to a `removed` subfolder rather than erased. F9 → Settings (images) and F9 → Menu design (menu media) list the removed files with their size and offer Restore, Delete permanently and Empty trash. Both Delete permanently and Empty trash require a second confirmation. Restore never overwrites an existing file.

### Known limitations

- **Media collage is an untested prototype.** 1.31.1 has not been run in a live gameplay session. Camera-event ordering, tile alignment at different resolutions, empty-tile background color and hardware GPUs are unverified. Enable it only in a save you can leave.
- **Capture modes are unverified in game.** Switching between Selected objects, Hero + attacks, Hero only and Full scene, the suspension and restoration of preserve rules, and hiding attacks through the Attack layer and promoted effects have not been checked in a live room. Applying a preset rewrites `ShaderStripMod.preserve.txt` (after a backup), so test presets in a save you can leave.
- **Gameplay coverage is incomplete.** The last live run covered the title menu, F9, PNG/GIF/MP4 H.264, pause and input at 1360×768 and 800×600. It did not open a save or a gameplay room. Camera pin behavior in real rooms, during deaths, transitions and cutscenes was checked with Unity/game API substitutions. The 1.29.31-1.31.1 input gate, independent toggles, video sound, media collage, configurable hotkeys, capture modes/presets and the reworked browser layout have not been run in a live session yet.
- **GPU coverage.** The live run used software DirectX WARP. Hardware GPUs and other video codecs are unverified.
- **Windows-only import dialog.** The background/logo/media picker uses Windows APIs.
- **Preserve rules are heuristic.** A rule targets a renderer, sprite region or object path, not a semantic object type. Shared atlases can produce false matches. Renderer/particle limits can skip objects that match a broad rule; F9 → Performance and F9 → Rules now report the counts and reasons. Diagnostics are invalidated on scene changes, rule changes and `F12` toggles; while visuals are off, the selected-rule report is explicitly marked as a dry scan.
- **Settings are rewritten on save.** Unknown future keys or manual comments may be lost in `ShaderStripMod.settings.txt`, the generated `ShaderStripMod.composition.txt`, `ShaderStripMod.capture-presets.txt` and `ShaderStripMod.hotkeys.txt`. Back up these files (and `ShaderStripMod.preserve.txt`) before upgrading.
- **Hotkeys are checked only against the mod.** Hollow Knight and other mods may already use the same key. Edit `ShaderStripMod.hotkeys.txt` only while the game is closed. Esc, Enter, Tab and arrow keys are reserved; clearing the browser hotkey means the browser cannot be opened from the keyboard until you edit the file.
- **Camera keys.** While the browser is open, the camera-pin, dump, reload and isolation hotkeys are ignored; use F9 → Settings to pin or release the camera. The isolation hotkey keeps the camera pin and only restores the visual mode.
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

The build environment is not pinned. A later rebuild can have a different module version id (MVID) and PDB checksum even though the method bodies are unchanged. The public release ZIP ships the audited 1.29.30 binary; 1.31.1 is a local testing candidate.

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

Stable 1.31.1 passes 159,612 assertions across these eight projects. RegressionChecks covers capture modes and preset limits, preset-file parsing and round-trips, preserve-rule suspension, snapshot restore, zero-gap collage layouts, placement modes, format-v1/v2 presets, unsupported-version refusal, legacy-profile migration, inactive-state policy, hotkeys and localization. Most assertions are pixel comparisons for GIF and preview code; they are not a substitute for a live gameplay test.

### Reporting issues

Describe the game version, mod version, scene, steps and whether the issue happens with other mods disabled. Do not post an unredacted `Player.log` or `ModLog.txt`: they can contain local paths, your Steam user name and a list of installed mods.

### License

MIT. See [LICENSE](LICENSE).

## Русский

**Скачать:** [ShaderStripMod 1.29.30 — описание версии и ZIP](https://github.com/flyzeen-hk/ShaderStripMod/releases/tag/v1.29.30). Публичная сборка остаётся pre-release: живой игровой тест ещё не пройден. Патчи 1.29.31-1.31.1 для ввода, независимых переключателей, звука видео, медиакомпозиции, настраиваемых клавиш, интерфейса и режимов захвата установлены локально для теста и пока не опубликованы.

### Что делает мод

ShaderStripMod — инструмент для съёмки и визуального выделения в Hollow Knight, а не обычная замена косметических ассетов. Он работает с живой сценой Unity:

- **Chroma key / зелёный фон.** Скрывает большую часть сцены, оставляя героя, атаки и выбранные объекты.
- **Замена фона.** Однотонный цвет или PNG/JPG в игре; PNG/JPG/GIF/видео в титульном меню.
- **Браузер текстур (F9 по умолчанию).** Просмотр текстур, спрайтов, материалов и renderer-объектов, добавление preserve-правил и масок спрайта/атласа.
- **Preserve-правила.** Принудительно оставляют видимыми отдельные renderer-объекты, пути объектов, семейства снарядов и т. п.
- **Оформление меню.** Свой фон, логотип и стиль текста кнопок; звук видео меню можно включить отдельно. Оформление меню и изоляция геймплея управляются независимо.
- **Медиакомпозиция (F9 → Panels).** Рисует PNG/JPG/GIF и видео плитками без зазоров поверх полноэкранного геймплея; доступны восемь раскладок (весь экран, две половины, две строки, три колонки, три строки, основная + сбоку, основная + снизу, сетка 2×2). Персонаж и сохранённые объекты всегда отрисовываются поверх композиции, а зелёный фон и preserve-правила продолжают работать. Плитки меняют размер вместе с окном и используют размещение Cover, Contain или Stretch. До четырёх плиток имеют свои паузу, звук, mute и громкость. Пресет хранится в `ShaderStripMod.composition.txt`; кнопки Save, Reload и Reset находятся во вкладке Panels. Нужны изоляция геймплея и игровая комната с живым героем; при переходах композиция приостанавливается, а при смене сцены освобождает CommandBuffer и медиа. Пустые плитки показывают цвет фона. Неподдерживаемая версия `formatVersion` в пресете отклоняется, файл никогда не перезаписывается; вкладка показывает результат автосохранения. Видеоплитки сообщают об отсутствии звуковой дорожки.
- **Фиксация камеры (F8 по умолчанию).** Замораживает игровую камеру в текущей точке, пока герой продолжает двигаться. Фиксация временная: снимается при смене комнаты, смерти, переходах, катсценах и выгрузке мода. Клавиша изоляции выключает только изоляцию геймплея и сохраняет фиксацию, а клавиша фиксации камеры игнорируется при открытом браузере.
- **Диагностика.** Клавиши дампа и перечитывания правил (F10 и F11 по умолчанию) сохраняют загруженные ресурсы и обновляют правила; экспорт и профили производительности доступны в F9.
- **Состояние захвата (F9 → Настройки).** Только для чтения: сводка показывает изоляцию геймплея, оформление меню, состояние камеры, совпадения и лимиты preserve, а также состояние композиции, чтобы было видно, почему объект скрыт, ещё до изменения правил.
- **Режимы захвата и пресеты (F9 → Настройки).** Четыре режима меняют то, что попадает в кадр: Selected objects (работают ваши preserve-правила), Hero + attacks (герой и эффекты атак; preserve-правила приостанавливаются, но не удаляются), Hero only (скрывает эффекты атак и приостанавливает правила) и Full scene (исходная комната с выключенной изоляцией геймплея). Пресет сохраняет снимок текущих preserve-правил и флаг изоляции в `ShaderStripMod.capture-presets.txt`; введите имя и нажмите Save, Apply или Delete. Перед применением создаётся бэкап `ShaderStripMod.preserve.txt`, перед заменой или удалением пресета — бэкап файла пресетов. Применение, замена и удаление транзакционны: если бэкап или запись не удались, и правила, и файл пресетов остаются прежними. Маски, импортированные изображения, медиа и настройки композиции в пресет не входят.
- **Настраиваемые клавиши (F9 → Hotkeys).** Каждое действие мода использует одну клавишу. Изменения сразу сохраняются в `ShaderStripMod.hotkeys.txt`. Esc, Enter, Tab и стрелки зарезервированы; конфликты проверяются только между действиями мода.

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

Мод пока не добавлен в ModLinks. Текущую pre-release-сборку можно скачать со [страницы релиза v1.29.30](https://github.com/flyzeen-hk/ShaderStripMod/releases/tag/v1.29.30). Сборка 1.31.1 ещё не опубликована. После добавления в ModLinks установка и обновление через Lumafly будут рекомендованным способом.

Релизный ZIP содержит только `ShaderStripMod.dll`, `README.md`, `CHANGELOG.md` и `LICENSE`. В нём нет QA-логов, скриншотов, пользовательских настроек и медиа.

### Управление

| Клавиша | Действие |
| --- | --- |
| `F8` | Закрепить / открепить игровую камеру в текущей точке. Игнорируется при открытом браузере. |
| `F9` | Открыть / закрыть браузер текстур. По умолчанию ставит игру на паузу. |
| `F10` | Дамп загруженных спрайтов, текстур, материалов и renderer-объектов. |
| `F11` | Перечитать `ShaderStripMod.preserve.txt` и `ShaderStripMod.visual-rules.txt`. |
| `F12` | Включить / выключить изоляцию геймплея. Фиксация камеры и оформление меню сохраняются. |

В таблице указаны клавиши по умолчанию. Все пять действий можно переназначить в F9 → Hotkeys; изменённая клавиша также обновляет подписи в браузере и в создаваемых файлах. Внутри F9 `Esc` закрывает браузер, если не идёт захват клавиши; во время захвата Esc его отменяет. Пока браузер открыт, остальные клавиши не срабатывают из текстовых полей и других элементов браузера. Импорт и переключатели камеры/паузы находятся во вкладке Settings. Звук видео меню — во вкладке Menu design, медиакомпозиция — во вкладке Panels. Маски спрайта открываются из вкладки Nearby.

### Runtime-файлы

Мод пишет рядом со своей DLL, а если папка недоступна — в `Application.persistentDataPath`:

```text
ShaderStripMod.settings.txt        настройки интерфейса, экспорта и производительности
ShaderStripMod.preserve.txt        preserve-правила (ручная работа, делайте бэкап)
ShaderStripMod.visual-rules.txt    необязательные visual rules (formatVersion=1)
ShaderStripMod.composition.txt     раскладка, размещение и медиа-настройки плиток композиции (formatVersion=2)
ShaderStripMod.hotkeys.txt         настраиваемые привязки клавиш (formatVersion=1)
ShaderStripMod.capture-presets.txt сохранённые пресеты захвата: снимки preserve-правил и флаг изоляции (formatVersion=1)
ShaderStripMod.sprite-dump.txt     создаётся по клавише дампа
background-images/                 импортированные фоновые изображения игры
menu-media/                        изображения, GIF и видео меню
sprite-masks/                      сохранённые маски спрайтов/атласов
replacements/                      пользовательские изображения замен
exports/                           PNG-экспорты и metadata
```

Удалённые фон и медиа меню перемещаются в подпапку `removed`, а не стираются. F9 → Settings (изображения) и F9 → Menu design (медиа меню) показывают список удалённых файлов с размером и кнопками Restore, Delete permanently и Empty trash. И Delete permanently, и Empty trash требуют повторного подтверждения. Restore не перезаписывает существующий файл.

### Известные ограничения

- **Медиакомпозиция — непроверенный прототип.** 1.31.1 ещё не запускалась в живой игровой сессии. Порядок camera events, выравнивание плиток при разных разрешениях, цвет фона пустых плиток и аппаратные GPU не проверены. Включайте её только в сохранении, которое не боитесь покинуть.
- **Режимы захвата не проверены в игре.** Переключение между Selected objects, Hero + attacks, Hero only и Full scene, приостановка и восстановление preserve-правил, а также скрытие атак через слой Attack и promoted-эффекты не проверялись в живой комнате. Применение пресета перезаписывает `ShaderStripMod.preserve.txt` (после бэкапа), поэтому пробуйте пресеты в сохранении, которое не боитесь покинуть.
- **Игровые сцены проверены не полностью.** Последний живой прогон охватил титульное меню, F9, PNG/GIF/MP4 H.264, паузу и ввод при 1360×768 и 800×600. Сохранение и игровая комната не открывались. Поведение фиксации камеры в реальных комнатах, при смерти, переходах и катсценах проверялось заменами Unity/game API. Изменения 1.29.31-1.31.1 для input-gate, независимых переключателей, звука видео, медиакомпозиции, настраиваемых клавиш, режимов захвата/пресетов и переработанного интерфейса ещё не проверены в живой игровой сессии.
- **GPU.** Живой прогон использовал программный DirectX WARP. Аппаратные GPU и другие видеокодеки не проверены.
- **Импорт только в Windows.** Выбор фона, логотипа и медиа использует Windows API.
- **Preserve-правила эвристические.** Правило нацелено на renderer, область спрайта или путь объекта, а не на смысловой тип объекта. Общие атласы дают ложные совпадения. Лимиты renderer/particle-источников могут пропустить объекты, подходящие под широкое правило; F9 → Performance и F9 → Rules теперь показывают количества и причины. Диагностика сбрасывается при смене сцены, изменении правил и выключении визуального режима; при выключенном F12 отчёт явно помечен как сухая проверка.
- **Настройки перезаписываются при сохранении.** Неизвестные будущие ключи и ручные комментарии могут потеряться в `ShaderStripMod.settings.txt`, в созданном моде пресете `ShaderStripMod.composition.txt`, в `ShaderStripMod.capture-presets.txt` и в `ShaderStripMod.hotkeys.txt`. Перед обновлением сохраните копии этих файлов (и `ShaderStripMod.preserve.txt`).
- **Конфликты клавиш проверяются только внутри мода.** Hollow Knight и другие моды могут уже использовать ту же клавишу. Редактируйте `ShaderStripMod.hotkeys.txt` только при закрытой игре. Esc, Enter, Tab и стрелки зарезервированы; если очистить клавишу браузера, открыть его с клавиатуры можно будет только после правки файла.
- **Клавиши камеры.** При открытом браузере клавиши фиксации камеры, дампа, перечитывания правил и изоляции игнорируются; фиксация включается и снимается кнопкой в F9 → Settings. Клавиша изоляции сохраняет фиксацию камеры и только выключает изоляцию геймплея.
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

Окружение сборки не зафиксировано. Пересборка из этих исходников может получить другой MVID и контрольную сумму PDB при тех же телах методов. В публичный релизный ZIP входит проверенный бинарник 1.29.30; 1.31.1 — локальный тестовый кандидат.

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

Стабильная 1.31.1 проходит 159 612 assertion в этих восьми проектах. RegressionChecks покрывает режимы захвата и лимиты пресетов, разбор и round-trip файла пресетов, приостановку preserve-правил, восстановление снимка, раскладки без зазоров, режимы размещения, пресеты formatVersion=1/2, отказ от неподдерживаемой версии, миграцию старого профиля, политику неактивных состояний, горячие клавиши и локализацию. Большинство — попиксельные сравнения GIF и preview, они не заменяют живой игровой тест.

### Сообщения об ошибках

Укажите версию игры, версию мода, сцену, шаги и воспроизводится ли проблема с отключёнными другими модами. Не публикуйте неотредактированные `Player.log` и `ModLog.txt`: в них могут быть локальные пути, имя пользователя Steam и список установленных модов.

### Лицензия

MIT. См. [LICENSE](LICENSE).
