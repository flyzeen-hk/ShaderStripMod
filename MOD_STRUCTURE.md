# ShaderStripMod: структура, взаимодействия и план развития

Дата обновления: 2026-10-25  
Текущая версия по исходникам: `v1.29.29`  
Главный класс: `ShaderStripMod`, физически разделённый на тематические `partial`-файлы в `Source/`

## Стабильная 1.29.29

Добавлена фиксация текущей игровой камеры: F8 или F9 → Settings / «Настройки». Повторное нажатие возвращает слежение. `CameraPin.cs` удерживает мировую позицию, временно замораживая только расчёт слежения внутри CameraController.LateUpdate; штатные режимы FOLLOWING/LOCKED сохраняются. Смена комнаты, смерть, переходы, катсцены, F12 и выгрузка освобождают камеру. Координаты и состояние фиксации не записываются в настройки.

F9 имеет непрозрачную подложку и видимую активную вкладку. `RuleControlsLayout.cs` размещает кнопки правил без пересечений; Rules прокручивается целиком, включая информацию. Настройки камеры и паузы расположены до библиотеки изображений, длинные переключатели переносятся, излишняя пустая прокрутка убрана. Ограничения PNG/JPG проверяются до декодирования также для обычного игрового фона. Итоги восьми проектов проверок и ограничения игрового прогона зафиксированы в локальном QA-отчёте; публичный репозиторий содержит README и CHANGELOG без QA-логов.

## Предыдущая стабильная 1.29.28

Исправлены границы предпросмотра и освобождение захвата мыши, наложение счётчика на строки списка, блокировка ввода меню через сохранённый EventSystem, накопительное затемнение текста при переходах, восстановление оформления и освобождение медиа. Видео получает обычный абсолютный Windows-путь с кириллицей. Настройки и preserve-списки сохраняются через `AtomicTextFile` с сохранением предыдущей копии. Повторное назначение камер исключает камеры render-to-texture и выбирает одну игровую камеру для фона и сохранённых объектов.

`TexturePreview.cs`, `BrowserInputGate.cs` и `BrowserListLayout.cs` отделяют просмотр, блокировку событий и геометрию списка. `MenuBackgroundDecorations.cs` временно скрывает стандартные украшения под готовым фоном и восстанавливает их. Проверки расположены в восьми отдельных проектах Tests. `Tests/RuntimeAudit/StableRuntimeAudit.cs` подключается только при `/p:RuntimeAudit=true`; в обычной Release DLL этого класса нет. Такой запуск требует отдельный marker-файл и работает только в главном меню, без открытия сохранения. Подробный отчёт и ограничения входят в пакет `dist/stable-1.29.28` соседнего каталога ShaderStripMod.

## Коротко

ShaderStripMod сейчас похож на runtime chroma-key / green-screen мод для Hollow Knight с ручной системой исключений.

Основная идея:
- игра оставляет видимыми нужные слои, прежде всего героя и часть эффектов;
- камеры получают однотонный фон, по умолчанию зеленый, но цвет можно выбрать;
- с `v1.29.20` вместо однотонного фона можно выбрать PNG/JPG/JPEG из папки `background-images` рядом с настройками мода; изображение масштабируется по размеру экрана и работает в меню и игровых сценах;
- материалы видимых рендереров переводятся на простой shader `Sprites/Default`;
- нужные объекты, спрайты, материалы, текстуры, пути и снаряды можно сохранить через preserve-правила;
- Texture Browser помогает найти нужный renderer/texture/sprite/material и добавить правило.

Мод всё ещё делает много разных вещей в одном логическом классе, но с 2026-08-14 класс физически разделён на тематические `partial`-файлы. Это облегчает навигацию и безопасные локальные изменения, хотя пока не заменяет настоящие независимые сервисы.

Фундамент Visual Overrides из Части 0 вынесен в отдельные классы `Source/VisualRules/` и `Source/Runtime/`. Части 1–2 подключили его к `Initialize`, F11, scene/cache lifecycle и Info F9 в read-only режиме. Runtime читает и разрешает правила, но не меняет Renderer/Material/Texture; применение каналов остаётся выключенным. Это фундамент и диагностика, а не пользовательская функция stable-линии.

## Пользовательский сценарий и границы stable

Упрощённая цепочка для пользователя:

```text
источник (игра или меню)
  -> выделение / preserve-правило
  -> видимые объекты
  -> фон (цвет, PNG/JPG или медиа меню)
  -> камера (обычная или закреплённая F8)
  -> скриншот / видео / экспорт
```

Пользовательские функции stable-линии: фон и chroma key, preserve-правила, браузер F9, маски спрайтов, оформление титульного меню, временная фиксация камеры F8, F10-дамп, F11-reload и PNG-экспорт.

Экспериментальные части: `Source/VisualRules/`, `Source/Runtime/`, dormant Part0 runtime gate и ownership-прототипы. Они не входят в обычный пользовательский сценарий и не должны включаться без отдельного UX, миграции и тестов реальной сцены.

## Файлы проекта

- `Source/ShaderStripMod.cs`  
  Общие поля и константы, жизненный цикл, hooks, Toggle и общий RestoreAll. Все field initializers намеренно оставлены здесь в исходном порядке.

- `Source/SettingsAndProfiles.cs`, `Source/Performance.cs`, `Source/RuntimePaths.cs`  
  Settings, performance profiles/scheduler и runtime/export paths.

- `Source/PreserveRules.cs`, `Source/PreserveInteraction.cs`, `Source/PreserveMatching.cs`  
  Persistence preserve rules, действия над ними и runtime matching/shader restore.

- `Source/BrowserDiscovery.cs`, `Source/BrowserEntries.cs`, `Source/BrowserGui.cs`, `Source/PreserveManagerGui.cs`  
  Жизненный цикл F9, discovery/grouping entries, основной IMGUI и менеджер правил.

- `Source/TextureExport.cs`, `Source/DiagnosticsAndIdentity.cs`  
  Экспорт/preview/metadata, F10 dump, имена, пути и диагностические helpers.

- `Source/Rendering.cs`, `Source/PreservedRendering.cs`  
  ApplyStrip, камеры/фон/слои, CommandBuffer, projectile watchers и preserved proxies.

- `Source/Models.cs`, `Source/RuntimeLoop.cs`  
  Существующие вложенные модели и компоненты ScanBehaviour/ProjectileVisibilityWatcher.

- `Source/Legacy/ShaderStripMod.v*.cs`  
  Старые reference snapshots. Они физически отделены от активных partial-файлов и рекурсивно исключены из Compile в csproj.

- `Source/VisualRules/`  
  Модели formatVersion=1, codec, atomic file store, RendererIdentity, resolver трёх каналов и чистая placement math.

- `Source/Runtime/OwnedBinding.cs`, `ExactMaterialOverrideLease.cs`, `ReferenceCountedResourceCache.cs`  
  Прототипы ownership/compare-before-restore и generation-safe ref-counted cache. Unity-прототип material clone прошёл отдельный opt-in gate, но не используется обычным runtime-flow.

- `Source/Runtime/VisualRuleRuntimeManager.cs`  
  Загрузка/reload visual rules, scope indexes, partial-document policy и bounded resolution cache.

- `Source/Runtime/VisualOverrideFoundation.cs`  
  Read-only Unity bridge: пути, master settings, сбор реального RendererIdentity, F9 diagnostics и HeroStart probe.

- `Source/Runtime/Part0RuntimeGate.cs`  
  Dormant opt-in harness для единственного material-clone/Tint/restore теста. Без специального marker-файла не выполняется.

- `Tests/`  
  Отдельный `net472` test runner чистой логики. `Tests/**/*.cs` исключены из основного csproj.

Физическое разбиение старого класса было механическим: 17 тематических файлов объявляют один `public sealed partial class ShaderStripMod`, поэтому private API и runtime-flow не менялись. Корень проекта отделяет исходники от файлов чтения: `.md`/`.txt` планы и документация остаются в корне, активный C# — в `Source/`.

До Части 0 контрольная Release-сборка и normalized IL дали 733 одинаковых члена и 0 missing/added/changed. После добавления фундамента normalized IL всех этих прежних 733 членов остался идентичным; новые типы добавлены отдельно. Текущая сборка проходит с 0 warnings/0 errors.

- `PART0_STATUS.md`  
  Краткая таблица закрытой Части 0, runtime gate и performance comparison.

- `VISUAL_RULES_FORMAT_V1.md`  
  Зафиксированная grammar, escaping, defaults и golden-примеры.

- `ShaderStripMod.csproj`  
  Проект `net472`. Путь к DLL Hollow Knight / Unity / Modding API задаётся свойством `HollowKnightRefs`; по умолчанию используется локальная папка `HollowKnightManaged`, затем стандартный Steam-путь.

- `README.md`, `CHANGELOG.md`, `LICENSE`, `.gitignore`  
  Публичная документация, история стабильной линии, MIT-лицензия и правила исключения локальных файлов из Git.

- `backups-history/`  
  История бэкапов исходников, сборок и установленных файлов.

- `bin/Release/ShaderStripMod.dll` и `ShaderStripMod.pdb`  
  Собранная версия мода.

- `obj/`  
  Временные файлы сборки.

## Runtime-файлы мода

Эти файлы создаются рядом с установленной DLL мода, если папка доступна. Если нет, используется `Application.persistentDataPath`.

- `ShaderStripMod.preserve.txt`  
  Главный файл правил сохранения. Его нельзя смешивать с настройками UI. Потеря этого файла означает потерю ручных правил.

- `ShaderStripMod.settings.txt`  
  Настройки экспорта, фона, Performance, projectile performance и staged Visual Overrides:
  - `exportFolderMode=Downloads|Mod|Custom`
  - `exportFolder=...` для custom-режима
  - `backgroundColor=Green|Blue|Red|Yellow|Cyan|Magenta|Orange|White|Black`
  - `pauseGameWhileBrowserOpen=true|false`
  - `visualOverridesEnabled=true|false`
  - `colorOverridesEnabled=true|false`
  - `textureReplacementsEnabled=true|false`
  - `staticDisplayEnabled=true|false`
  - `performanceProfile=Quality|Balanced|Performance|Ultra|Custom`
  - `applyStripInterval=...`
  - `proxyUpdateInterval=...`
  - `browserRefreshInterval=...`
  - `totalHeavyWorkBudgetMs=...`
  - `totalGcBudgetPerFrameKb=...`
  - `staggerPerformanceTasks=true|false`
  - `scanOnlyActiveBrowserTab=true|false`
  - `pauseBrowserScansWhenClosed=true|false`
  - `rendererPathCache=true|false`
  - `preserveMatchCache=true|false`
  - `projectileCandidateCache=true|false`
  - `browserSnapshotCache=true|false`
  - `drawSourceCache=true|false`
  - `showPerformanceCounters=true|false`
  - `showCacheStats=true|false`
  - `collectDetailedMetrics=true|false`
  - `showGcAllocStats=true|false`
  - `virtualizedBrowserList=true|false`
  - `cacheWarmupAfterSceneChange=true|false`
  - `projectileUseUnscaledTime=true|false`
  - `maxVisibleBrowserEntries=...`
  - `browserListOverscanRows=...`
  - `maxCachedRendererPaths=...`
  - `projectileCaptureMode=OnlyProjectilesTab|BrowserOpen|Always|Manual`
  - `projectilePerformance=Safe|Balanced|Aggressive|Custom`
  - `projectileCaptureInterval=...`
  - `recentProjectileLifetime=...`
  - `preservedSyncInterval=...`
  - `maxPreservedRenderers=...`
  - `maxProjectileParticleRenderers=...`

  Четыре visual override switch сейчас по умолчанию `false`. На этапе Частей 1–2 они отражаются только в диагностике и не активируют renderer mutations.

- `ShaderStripMod.visual-rules.txt`  
  Отдельный UTF-8 файл `formatVersion=1`. При отсутствии атомарно создаётся пустым; F11 перечитывает его вместе с preserve rules. Валидные строки плохого документа могут загрузиться частично, а полностью отклонённый reload сохраняет предыдущий рабочий document в памяти.

- `replacements/`  
  Зарезервированный root пользовательских PNG и backups visual-rules. Папка вычисляется при старте, но создаётся лениво только при реальном использовании replacement/save, поэтому Parts 1–2 её не создают.

- `ShaderStripMod.sprite-dump.txt`  
  Дамп имен sprites/textures/materials/renderers, создается по `F10`.

- `exports/` или папка Downloads  
  PNG-экспорты и metadata `.txt` к ним.

## Основной жизненный цикл

Упрощенная схема:

```text
Initialize
  -> InitializeListFiles
  -> LoadExportSettings
  -> LoadPreserveList
  -> InitializeVisualOverridesFoundation (read-only load/index)
  -> InitializePart0RuntimeGate (dormant без marker)
  -> создать GameObject ShaderStripMod.Runner
  -> повесить ScanBehaviour
  -> подписаться на SceneChanged и HeroController.Start
  -> ApplyStrip

ScanBehaviour.Update
  -> горячие клавиши F9/F10/F11/F12
  -> scheduler распределяет обычные тяжелые задачи по кадрам
  -> CaptureRecentProjectileRenderers по projectileCaptureMode/интервалу
  -> UpdatePreservedRendererProxies по proxyUpdateInterval
  -> SyncPreservedRendererDraws по интервалу или сразу после изменения projectile/preserve settings
  -> ApplyStrip по applyStripInterval

ScanBehaviour.OnGUI
  -> DrawTextureBrowserGui
  -> Info показывает identity, winners/blocks и причины resolver без применения правил

SceneChanged / HeroStart
  -> восстановить старое состояние
  -> сбросить caches
  -> инвалидировать resolution cache
  -> HeroStart один раз выполняет read-only identity/resolver probe
  -> заново ApplyStrip

Unload / DisableVisuals
  -> RestoreAll
  -> снять hooks
  -> уничтожить runner/proxy объекты
```

## Горячие клавиши

- `F8` - закрепить игровую камеру в текущей точке / вернуть слежение за персонажем (с 1.29.29).
- `F9` - открыть/закрыть Texture Browser; по умолчанию открытое окно ставит игровое время на паузу.
- `F10` - dump loaded sprites/textures/materials/renderers.
- `F11` - перезагрузить `ShaderStripMod.preserve.txt` и `ShaderStripMod.visual-rules.txt`.
- `F12` - включить/выключить визуальный режим мода.

Режим `Nearby` доступен внутри Texture Browser через `F9`. Его прежняя дублирующая клавиша F8 удалена в 1.29.8; с 1.29.29 F8 управляет закреплением камеры.

Пауза при открытом Texture Browser отключается во вкладке `Settings`. При закрытии браузера мод восстанавливает значение `Time.timeScale`, которое было до открытия.

Пока Texture Browser открыт, мод показывает обычный игровой курсор и снимает его блокировку. С `v1.29.14` мод перехватывает `InputHandler.OnGUI` и только на время открытого F9 пропускает игровую команду скрытия курсора, устраняя переключение `visible=false/true` внутри кадра. При закрытии меню исходная логика Hollow Knight и предыдущие `Cursor.visible` и `Cursor.lockState` восстанавливаются.

## Главные состояния в памяти

Preserve-правила:
- `preservedAssetNames` - legacy broad rules по имени asset.
- `preservedSpriteNames` - точные имена sprite.
- `preservedTextureNames` - точные имена texture.
- `preservedMaterialNames` - точные имена material.
- `preservedObjectPaths` - точный object path.
- `preservedGroupObjectPaths` - object path + children.
- `preservedProjectileObjectPaths` - projectile path + children/family matching.
- `preservedRendererRules` - точные renderer rules с scene/path/type и optional sprite/material/texture.

Восстановление состояния:
- `strippedShaders` - original shader для материалов, которые были заменены.
- `strippedBackgrounds` - original background color камер.
- `strippedMasks` - original cullingMask камер.
- `strippedClearFlags` - original clearFlags камер.
- `promotedEffects` - original layer для эффектов, временно поднятых на слой героя.

Рендер сохраненных объектов:
- `preservedRendererCommandBuffers` - CommandBuffer на камерах для дорисовки preserved renderers.
- `preservedRendererDrawSources` - текущий cache рендереров, которые надо дорисовать.
- `preservedRendererProxies` - fallback-прокси для SpriteRenderer/MeshRenderer.
- `proxyRoot` - корневой GameObject для proxy-объектов.

Texture Browser:
- `textureBrowserEntries` - единый список строк для всех режимов браузера.
- `filteredTextureBrowserIndexes` - переиспользуемый буфер фильтрации списка без создания нового List каждый OnGUI.
- `textureBrowserSearch` - фильтр.
- `selectedTextureBrowserIndex` - выбранная строка.
- `textureBrowser...Mode` - Loaded/Nearby/Entities/Atlases/Rules/Projectiles.
- `textureBrowserDetailTab` - правая вкладка Info/Preview/Rules/Export/Settings/Performance.
- `textureBrowserPreviewZoom/Pan/Dragging` - состояние preview.

Снаряды:
- `recentProjectileRenderers` - недавно увиденные projectile-like renderers.
- `nextRecentProjectileCaptureTime` - throttle для поиска.
- `preservedSyncIntervalSetting` - custom-интервал синхронизации дорисовки preserved renderers.

Настройки:
- `exportFolderMode/exportDirectory/exportDirectoryInput`
- `backgroundColorName/stripBackground`
- `performanceProfile`, `applyStripIntervalSetting`, `proxyUpdateIntervalSetting`, `heavyWorkBudgetMs`, `gcBudgetKb`
- `staggerPerformanceTasks`, `projectileCaptureMode`, `projectileUseUnscaledTime`
- `rendererPathCacheEnabled`, `preserveMatchCacheEnabled`, `projectileCandidateCacheEnabled`, `browserSnapshotCacheEnabled`, `drawSourceCacheEnabled`
- `virtualizedBrowserList`, `maxVisibleBrowserEntries`, `browserListOverscanRows`
- `showPerformanceCounters`, `showCacheStats`, `collectDetailedMetrics`, `showGcAllocStats`
- `projectilePerformanceMode`, `projectileCaptureIntervalSetting`, `recentProjectileLifetimeSetting`, `preservedSyncIntervalSetting`, `maxPreservedRenderersSetting`, `maxProjectileParticleRenderersSetting`
- `visualOverridesEnabled`, `colorOverridesEnabled`, `textureReplacementsEnabled`, `staticDisplayEnabled` — staged switches; пока не меняют renderer

Read-only Visual Overrides:
- `visualRuleRuntimeManager` — активный документ, indexes, diagnostics и resolution cache
- `visualRulesFilePath`, `visualRulesBackupDirectory`, `replacementsDirectory` — вычисленные runtime paths
- `visualOverrideRuntimeProbeLogged` — ограничивает HeroStart probe одним сообщением на сцену

Кэширование и счетчики:
- `objectPathCache` - фактический bounded cache для object path по instance id.
- `objectPathCacheOrder` - простой порядок вытеснения path-cache, не полноценный LRU.
- `objectPathSegments` и `objectPathBuilder` - переиспользуемые буферы для построения path без лишних `StringBuilder` на каждый вызов.
- `proxyRemoveBuffer` - переиспользуемый буфер удаления stale proxy.
- `lastApplyStripMs/lastDrawSyncMs/lastProjectileScanMs/lastProxyUpdateMs` - последние измеренные времена.
- `last...GcBytes` - разница `GC.GetTotalMemory(false)` до/после операции; это приблизительный heap delta, а не точный allocation profiler.

UI:
- с `v1.29.7` ширина левой панели Texture Browser адаптируется к ширине окна, чтобы правая панель не становилась слишком узкой;
- detail tabs переносятся в два ряда на узкой панели;
- Performance-вкладка использует более вертикальную раскладку, короткие labels и больше высоты секций, чтобы текст не накладывался на соседний текст;
- поля ввода в Performance и Projectile Performance адаптируют ширину label/input/suffix под текущую ширину панели;
- status/counter строки в Performance разбиты на несколько коротких строк вместо длинных строк через `|`.

## Формат preserve-правил

Поддерживаемые виды строк:

- `rendererPath=...\trendererType=...\tscene=...\tsprite=...\tmaterial=...\ttexture=...`
- `projectileRendererPath=...\trendererType=...\tscene=...`
- `path=...`
- `groupPath=...`
- `projectilePath=...`
- `sprite=...`
- `texture=...`
- `material=...`
- `asset=...`
- plain line без `key=` считается legacy asset rule.

Смысл:
- `rendererPath` - самый точный и предпочтительный вариант для конкретного renderer.
- `path` - только конкретный object path.
- `groupPath` - object path и дочерние объекты.
- `projectilePath` - object path/семейство снарядов и дочерние объекты.
- `sprite/texture/material` - точное имя asset, но может быть шире, чем кажется.
- `asset` - legacy-режим, самый широкий и потенциально опасный.

При сохранении через UI файл полностью пересобирается: правила сортируются и записываются в стандартном порядке.

## Как работает ApplyStrip

`ApplyStrip` - центральная точка визуального режима.

В menu-сценах или когда слой героя еще неизвестен:
- вызывается `ApplyMenuBackground`;
- камеры получают выбранный цвет фона;
- menu mask старается оставить видимыми нужные UI/menu слои.

В gameplay-сценах:
- определяется `heroLayer`;
- собирается `visibleMask`, сейчас это слой героя + слой `Attack`;
- если раньше был menu-mode, старое состояние камер восстанавливается;
- `RestorePromotedEffects`;
- `PromotePreservedRenderers`;
- `PromoteEffects`;
- `ApplyCameraMask`;
- `StripAllMaterials`.

## Камеры и фон

`ApplyCameraMask`:
- выбирает world camera: `Camera.main`, иначе первая камера;
- world camera получает `visibleMask`;
- остальные камеры получают `cullingMask = 0`;
- всем обработанным камерам ставится `stripBackground`;
- старые camera state сохраняются в словари для восстановления.

Выбор цвета фона находится в Settings-вкладке Texture Browser и сохраняется в `ShaderStripMod.settings.txt`.

## StripAllMaterials

`StripAllMaterials`:
- ищет `Shader.Find("Sprites/Default")`;
- перебирает все `Renderer` через `Resources.FindObjectsOfTypeAll<Renderer>()`;
- пропускает proxy renderers;
- работает только с renderer на видимых слоях;
- если material не preserved, сохраняет original shader в `strippedShaders`;
- заменяет shader на `Sprites/Default`.

Это один из участков, где любые широкие сканы и слишком частое применение могут влиять на производительность.

## Как сохраняются нужные объекты

Есть два механизма:

1. Не обесцвечивать material, если renderer/material/sprite/texture/path попал под preserve-правило.  
   Это проверяется через `ShouldPreserveMaterial`.

2. Дорисовывать preserved renderer, если он находится на слое, который камера уже не видит.  
   Основной путь: `CommandBuffer.DrawRenderer` на world camera.  
   Fallback: proxy-объекты на слое героя для `SpriteRenderer` и `MeshRenderer`.

Ограничения:
- максимум preserved renderers по умолчанию = 160, но в Performance можно выставить custom `0..999`;
- максимум projectile particle draw sources по умолчанию = 48, но в Performance можно выставить custom `0..999`;
- `ParticleSystemRenderer` без projectile rule считается дорогим и обычно не дорисовывается;
- renderer должен быть активен, включен, относиться к текущей gameplay scene и не быть proxy.

## PromoteEffects

Мод отдельно пытается сохранить часть эффектов героя:
- ищет GameObject по тегам `Nail Attack`, `Hero Spell`;
- ищет дочерние объекты героя с именами `Slash`, `Fireball`, `Focus`, `Scream`, `Dive`, `Shield`, `Soul`;
- безопасные visual-only объекты переводит на слой героя;
- объекты с collider/rigidbody/joint/FSM/damage/health компонентами не трогает.

Это попытка сохранить эффекты игрока без грубого сохранения всего мира.

## Texture Browser

Окно IMGUI масштабируется под экран. Сейчас основные режимы сверху:

- `Loaded`  
  Все загруженные Sprite/Texture/Material из `Resources.FindObjectsOfTypeAll`. Может быть большим и шумным.

- `Entities`  
  Группы renderer-объектов текущей сцены: Boss/NPC/Mob/Entity. Это уже зачаток будущих Entity Presets, но пока без сохранения пресетов.

- `Nearby`  
  Рендереры текущей комнаты рядом с героем/камерой, сортировка по дистанции.

- `Atlases`  
  Материальные texture/atlas entries из рендереров текущей сцены, с score для выбора лучшего atlas source.

- `Rules`  
  Менеджер текущих preserve-правил.

- `Projectiles`  
  Недавно замеченные projectile-like renderers. Работает через runtime-наблюдение, а не через полный список assets.

- `Dump`  
  Создает `ShaderStripMod.sprite-dump.txt`.

Правая панель:
- `Info` - подробности выбранной строки; для projectile/preserved renderer показывает диагностику причин видимости и дорисовки.
- `Preview` - просмотр texture/sprite с zoom/pan.
- `Rules` - кнопки добавления/удаления renderer/asset/path/projectile rules для выбранной строки.
- `Export` - экспорт PNG/atlas и путь экспорта.
- `Settings` - цвет фона.
- `Performance` - профили нагрузки, projectile performance, scheduler/cache/diagnostics settings и счетчики.

## Performance

Вкладка `Performance` появилась в `v1.29.6`, а в `v1.29.7` получила hotfix против наложения текста.

Она отвечает за нагрузку runtime-систем, а не за preserve-правила:
- профили `Quality`, `Balanced`, `Performance`, `Ultra`, `Custom`;
- кнопки `Reset`, `Q->Custom`, `B->Custom`, `P->Custom`, `U->Custom`;
- интервалы `ApplyStrip`, `Proxy update`, `Browser refresh`;
- общий бюджет `totalHeavyWorkBudgetMs` и приблизительный GC budget;
- режим захвата снарядов `OnlyProjectilesTab`, `BrowserOpen`, `Always`, `Manual`;
- перенесенный блок `Projectile Performance`;
- cache flags, diagnostics flags и счетчики.

Фактические профили `v1.29.7`:

```text
Quality:
  applyStripInterval=1
  proxyUpdateInterval=0
  browserRefreshInterval=0.25
  totalHeavyWorkBudgetMs=4
  totalGcBudgetPerFrameKb=32
  maxVisibleBrowserEntries=1000
  browserListOverscanRows=6
  maxCachedRendererPaths=6000
  projectileCaptureMode=BrowserOpen
  projectileCaptureInterval=0.06
  recentProjectileLifetime=8
  preservedSyncInterval=0.06
  maxPreservedRenderers=320
  maxProjectileParticleRenderers=120

Balanced:
  applyStripInterval=2
  proxyUpdateInterval=0
  browserRefreshInterval=0.5
  totalHeavyWorkBudgetMs=2
  totalGcBudgetPerFrameKb=4
  maxVisibleBrowserEntries=500
  browserListOverscanRows=4
  maxCachedRendererPaths=3000
  projectileCaptureMode=OnlyProjectilesTab
  projectileCaptureInterval=0.12
  recentProjectileLifetime=3
  preservedSyncInterval=0.12
  maxPreservedRenderers=160
  maxProjectileParticleRenderers=48

Performance:
  applyStripInterval=3
  proxyUpdateInterval=0.12
  browserRefreshInterval=1
  totalHeavyWorkBudgetMs=1
  totalGcBudgetPerFrameKb=2
  maxVisibleBrowserEntries=350
  browserListOverscanRows=3
  maxCachedRendererPaths=3000
  projectileCaptureMode=OnlyProjectilesTab
  projectileCaptureInterval=0.30
  recentProjectileLifetime=2
  preservedSyncInterval=0.25
  maxPreservedRenderers=100
  maxProjectileParticleRenderers=24

Ultra:
  applyStripInterval=5
  proxyUpdateInterval=0.25
  browserRefreshInterval=2
  totalHeavyWorkBudgetMs=0.5
  totalGcBudgetPerFrameKb=1
  maxVisibleBrowserEntries=200
  browserListOverscanRows=2
  maxCachedRendererPaths=1500
  projectileCaptureMode=Manual
  projectileCaptureInterval=0.50
  recentProjectileLifetime=1.5
  preservedSyncInterval=0.50
  maxPreservedRenderers=60
  maxProjectileParticleRenderers=12
```

Custom:
- редактирует все поля вручную;
- интервалы `0` означают every frame;
- projectile limits остаются в диапазоне `0..999`;
- hard caps применяются к memory/UI-heavy настройкам: visible entries, overscan rows, cached paths, frame budget, GC budget.

Scheduler:
- работает в `ScanBehaviour.Update`;
- обычные periodic-задачи проходят через `TryEnterScheduledTask`;
- за кадр старается запускать только одну обычную тяжелую задачу;
- срочные задачи обходят ограничение: `preserveDrawSyncRequested`, `preservedSyncInterval=0`, `projectileCaptureInterval=0`, `applyStripInterval=0`, `proxyUpdateInterval=0`;
- это простой frame staggering, не полноценная очередь incremental tasks.

Счетчики:
- собираются только если включены `showPerformanceCounters`, `showCacheStats` или `collectDetailedMetrics`;
- показывают последние значения для `ApplyStrip`, `Draw sync`, `Projectile scan`, `Proxy update`;
- GC-значение сейчас является heap delta через `GC.GetTotalMemory(false)`, поэтому оно полезно как приблизительный индикатор, но не заменяет настоящий Unity Profiler allocation view.

Кэши:
- фактически реализован `rendererPathCache` для object path;
- `drawSourceCache` влияет на то, использовать ли cache draw sources или чаще пересобирать его;
- `preserveMatchCache`, `projectileCandidateCache`, `browserSnapshotCache`, `cacheWarmupAfterSceneChange` пока являются сохраненными настройками/флагами для будущих этапов и не являются полноценными отдельными системами.

Time source:
- `ApplyStrip`, draw sync, proxy update и UI идут по `Time.unscaledTime`;
- projectile capture может использовать `Time.unscaledTime` или `Time.time` через `projectileUseUnscaledTime`;
- по умолчанию сейчас используется unscaled time, чтобы диагностика работала даже при паузе/меню.

## Экспорт текстур

Экспорт работает так:
- выбирается `TextureExportSource`;
- для sprite export берется sprite region, если Unity позволяет получить `textureRect`;
- иначе может экспортироваться полный atlas;
- texture копируется через temporary `RenderTexture`, `Graphics.Blit`, `Texture2D.ReadPixels`;
- PNG пишется в выбранную папку;
- рядом пишется `.txt` metadata с source texture/material/renderer/path/scene/region.

Возможные проблемы:
- tight/packed sprites могут экспортироваться как полный atlas;
- некоторые texture types могут не прочитаться;
- atlas auto-pick является эвристикой, а не гарантией.

## Снаряды

Текущая система снарядов осторожная:
- с `v1.29.9` безопасные визуальные GameObject сохранённых снарядов временно переводятся на слой героя и рисуются штатным Unity pipeline; объекты с collider/FSM/физикой остаются на CommandBuffer fallback;
- на renderer-объекты известных projectile rules устанавливается runtime watcher, который повторно применяет видимость при активации pooled-объекта;
- projectile renderer rules больше не требуют совпадения текущего sprite/material/texture, поэтому смена кадра анимации не отменяет правило;
- сохранение renderer-дерева снаряда включает все дочерние renderer-компоненты под выбранным projectile root, а не только дочерние объекты с projectile hint;
- нет постоянного тяжелого auto-scan всех снарядов;
- `CaptureRecentProjectileRenderers` работает throttled, интервал теперь настраивается в Performance;
- `SyncPreservedRendererDraws` тоже получил настройку `preservedSyncInterval`, потому что короткоживущий renderer мог быть найден, но исчезнуть раньше следующей синхронизации дорисовки;
- projectile browser показывает то, что было реально замечено недавно;
- lifetime snapshot по умолчанию около 3 секунд, но теперь настраивается в Performance;
- снаряд определяется по layer/name/path hints.

Практический режим для проверки невидимых снарядов:
- в Performance выбрать `Custom`;
- поставить `projectileCaptureInterval=0`, чтобы список Projectiles обновлялся каждый кадр;
- поставить `preservedSyncInterval=0`, чтобы cache дорисовки preserved renderers синхронизировался каждый кадр;
- при лагах вернуть `Balanced` или поднять интервалы обратно.

Во вкладке `Info` у projectile entry теперь должны быть диагностические строки:
- `Can draw`;
- `Skip reason`;
- `In draw source cache`;
- `Renderer enabled`;
- `Object active`;
- `Scene match`;
- `Projectile path match`;
- `Preserved by`.

Из-за этого:
- если атака уже прошла, renderer может не попасть в список;
- если имя/слой нетипичный, projectile может не определиться;
- зато нет возврата к тяжелой версии, которая вызывала сильные лаги.

## На что это похоже архитектурно

Похоже на смесь:
- runtime visual filter;
- chroma-key overlay tool;
- Unity object/texture inspector;
- rule-based allowlist manager;
- exporter/debugger.

Это не обычный content mod, который просто заменяет ассеты. Он постоянно взаимодействует с runtime-сценой Unity: камерами, слоями, рендерами, материалами, GameObject paths и временно появляющимися объектами.

## Главные существующие проблемы

- Физическая проблема одного файла устранена, но `partial`-файлы по-прежнему образуют один логически связанный класс с большим количеством общих private-состояний. Новые parser/resolver/cache системы лучше добавлять отдельными сервисами, а не расширять этот coupling.
- Preserve-правила можно менять из игры; это удобно, но рискованно. Пользователь уже случайно удалял правила.
- `SaveExportSettings` пересобирает settings-файл и не сохраняет неизвестные будущие ключи.
- `SavePreserveList` пересобирает preserve-файл целиком; ручные комментарии/группировка пользователя теряются.
- `Resources.FindObjectsOfTypeAll` используется во многих местах. Это нормально для ручных refresh, но опасно для частых автоматических операций.
- Entity grouping сейчас эвристический: по root path, layers и hints, без надежной базы пресетов.
- Asset rules по имени могут быть слишком широкими, особенно при общих texture/atlas/material.
- Unity object path может меняться между сценами, фазами боя или prefab-инстансами.
- Projectile detection не может гарантировать все снаряды, потому что часть объектов существует очень коротко.
- CommandBuffer/proxy fallback сложный: возможны отличия порядка отрисовки, sorting, particle behavior и shader-specific эффектов.
- Низкий лимит draw renderers или projectile particle sources может скрыть часть preserved объектов при широких правилах; слишком высокий лимит может вызвать лаги.
- Некоторые ignored rules (`_Enemies`, `sprite_set_*`, `GG_Arena_Prefab/BG`) защищают производительность, но могут удивлять при ручной настройке.
- Нет автоматических тестов парсера preserve/settings и matching-логики.
- Бэкапы делаются вручную в рабочем процессе, а не встроены как обязательная versioned migration система.

## Проблемы, которые могут появиться при новых функциях

- Еще больше UI-кнопок сделают Texture Browser перегруженным, если не выделить отдельные панели.
- Entity Presets без отдельного файла смешаются с preserve rules и снова создадут риск случайного удаления.
- Автообнаружение всех боссов по assets будет давать false positives из-за общих атласов.
- Runtime scan всех объектов ради пресетов может вернуть лаги, похожие на неудачную auto-projectile версию.
- Если сделать массовое `Enable All` без preview/counts, можно создать слишком широкие правила и резко ухудшить производительность.
- Если добавить JSON/сложный формат без миграции, старые `.txt` rules/settings можно случайно сломать.

## Минимальный план развития

1. Безопасность файлов
   - Автоматический backup `preserve.txt` перед любым изменением из UI уже есть для части операций, но его стоит сделать единообразным.
   - Добавить confirm для `Clear`, `Remove Rule`, `Clean Ignored`, `Compact`.
   - Разделить destructive actions и обычные toggles визуально.

2. Разделение настроек
   - Переименовать `LoadExportSettings/SaveExportSettings` в общие `LoadSettings/SaveSettings`.
   - Научить settings сохранять неизвестные ключи.
   - Не перезаписывать комментарии пользователя без необходимости.

3. Разгрузка главного класса
   - Выполнено: существующий код механически разделён на тематические `partial`-файлы без изменения IL.
   - Далее: вынести parser preserve-файла в независимо тестируемый сервис.
   - Далее: вынести settings storage.
   - Далее: уменьшать прямые зависимости Texture Browser UI от runtime renderer state.
   - Далее: новые visual-rule resolver/cache создавать отдельными классами, а не новыми partial-блоками.

4. Диагностика правил
   - Для каждого правила показывать `matched renderers: N`.
   - Отдельно показывать ignored/effective/expensive/skipped.
   - Добавить причину, почему renderer не был сохранен.

5. Entity Presets, очень осторожно
   - Сначала read-only вкладка с текущими видимыми группами.
   - Потом сохранение runtime-наблюдений в `ShaderStripMod.entity-presets.txt`.
   - Потом preview состава preset.
   - Только после этого кнопки Apply/Remove по секциям.

6. Производительность
   - Создать единый scan service с throttle и budget.
   - Не запускать `FindObjectsOfTypeAll` из нескольких подсистем без общей координации.
   - Кэшировать scene/renderers и инвалидировать cache на scene change / refresh.
   - В UI показывать, когда операция тяжелая.

7. Тесты
   - Parser tests для preserve lines.
   - Matching tests для rendererPath/path/groupPath/projectilePath.
   - Settings migration tests.
   - Минимальные regression tests на ignored rules.

## Приоритеты, если продолжать разработку

Самый полезный следующий шаг: не новые визуальные функции, а безопасная основа для них.

Практичный порядок:
1. Confirm/undo для действий с правилами.
2. Диагностика "почему объект не сохранился".
3. Разделить settings и preserve UI понятнее.
4. Runtime read-only Entity Browser 2.0.
5. Только потом Entity Presets с применением правил.

Так меньше шанс снова получить лаги или случайно сломать рабочий preserve-файл.






## Stable 1.29.21: области одного спрайта

Source/SpriteMaskGeometry.cs — независимое от Unity разбиение простого контура на треугольники и пересечение с исходной геометрией; сохраняет UV, цвет и прозрачность вершин.
Source/SpriteMaskRecord.cs — проверяемый формат .mask, координаты 0..1, атомарная запись с .bak.
Source/SpriteMasks.cs — идентификация renderer по сцене/иерархии/компоненту, fingerprint геометрии и UV, кэш сетки, CommandBuffer.DrawMesh, освобождение ресурсов.
Source/SpriteMaskEditor.cs — вкладка Mask, предпросмотр геометрии, прямоугольник/контур, библиотека сохранённых масок.

Маска сохраняется в sprite-masks рядом с preserve-файлом и сама служит правилом видимости. Активная маска имеет приоритет над обычной полной отрисовкой renderer; fallback proxy для неё запрещён. Исходные игровые текстуры и сетки не изменяются. Изменение геометрии/UV скрывает маску до повторного сохранения. Изменения цвета вершин обновляют кэш; изменение положения объекта не влияет на выделение.

Поддержаны простые SpriteRenderer и плоские читаемые MeshRenderer с одной частью и одним материалом. Ограничения: до 64 точек контура, 4096 исходных вершин, 512 масок. Уже видимые игровые слои, снаряды, 3D и неподдерживаемые сетки не редактируются. Маски предназначены для статичных декораций; перекрытые внутри рисунка детали не восстанавливаются.

Тесты production алгоритмов и записи файлов: 666 assertions. Сборка проходит без предупреждений и ошибок. Интеграция Unity и внешний вид игрового UI не проверены в запущенной игре: computer-use недоступен из-за ошибки запуска sandbox helper.

## Stable 1.29.22: исправления по снимкам 58 и 59

Прежний GL.DrawMeshNow предпросмотр мог возвращать полностью прозрачную картинку. Он заменён на Source/SpriteMaskPreview.cs: чтение исходной текстуры и интерполяция реальных UV, включая повёрнутые участки атласа и отдельный alpha-канал. В редакторе картинка открывается автоматически. Тексты на русском; кнопка применения остаётся над картинкой, есть исходник/результат, zoom колесом и pan правой кнопкой. Библиотека масок вынесена в отдельный экран этой вкладки.

Активная маска теперь может применяться и к renderer, который игра рисует на видимом слое. Полная отрисовка подавляется через forceRenderingOff с учётом исходного значения; enabled и игровые слои не меняются. При выключении/удалении/выгрузке маски исходное состояние восстанавливается. Автоматическое продвижение эффектов не переносит замаскированный объект на слой героя.

Белая полоса на строках 766–767 снимков 58/59 точно совпадает с нижней рамкой F9 (x65..1188 и x67..1186). Source/BackgroundCameraFrame.cs обеспечивает полный rect экранной камеры перед отрисовкой для обоих режимов фона. Исходный rect восстанавливается при отключении и смене сцены; offscreen-камеры не изменяются.

1961 регрессионная проверка production кода: пиксели предпросмотра, повёрнутые UV, прозрачность, результат выделения, подавление полной отрисовки и восстановление состояния, покрытие камеры и прежние проверки. Тесты камеры используют управляемый stub и не являются запуском Unity. Визуальная проверка в самой игре остаётся невыполненной: node_repl завершается с windows sandbox failed/helper_unknown_error.

## Stable 1.29.23: выделение, Esc и язык (2026-10-10)

- Редактор Mask оставляет прямоугольное выделение: четыре полосы рамки внутри границ изображения, чёрная подложка/жёлтые линии, затемнение скрываемой части, Preview result и Fit image. GUI.matrix внутри группы больше не вращается.
- Контур по точкам удалён из интерфейса. Старые .mask продолжают загружаться и рисоваться. Редактор показывает их ограничивающий прямоугольник, но файл изменяется только после Apply and close.
- Esc закрывает F9. Обработчики InputHandler.Update и OnGUI, HeroController input/queue/attack, EventSystem блокируют игровой ввод до закрытия и отпускания кнопок. acceptingInput меняется только на время оригинального Update с finally-восстановлением; обновление контроллера сохраняется. Hooks снимаются при Unload.
- F9 → Settings → Interface language: English / Русский, uiLanguage в settings.txt. English по умолчанию. Переводятся элементы управления, настройки и инструкции маски; имена ресурсов/объектов, пути и сырая диагностика сохраняются. Размеры переносимых строк измеряются после перевода, стили кнопок кэшируются.
- Информация Info/Rules/Export переносится по ширине и длинным путям; подсказки показывают полные подписи.
- Маска сохраняет геометрию/UV выбранного renderer по его идентификатору сцены и пути. town_bench уже отдельный Sprite внутри атласа: мод не распознаёт скамейки. Объекты, нарисованные вместе внутри одного Sprite, выбираются вручную; перекрытые пиксели не восстанавливаются.
- Регрессионные проверки: 2397 assertions; часть проверок использует substitutes Unity/InputHandler. Фактическая отрисовка, мышь и Esc в запущенной игре не проверены.
## Stable 1.29.24: видимость меню паузы Esc (2026-10-10)

Причина: GameManager.PauseGameToggle вызывает GameCameras.MoveMenuToHUDCamera, переводит UICanvas на hudCamera и включает бит uGUI (27). Gameplay ApplyCameraMask обнулял все вторичные камеры; следующая обработка скрывала меню. Меню паузы находится в игровой сцене, поэтому ветка IsMenuScene не помогала.

Source/GameMenuCamera.cs получает реальную камеру центрального UICanvas и сохраняет на ней слои меню, включая uGUI. Маска героя/Attack остаётся отдельной: слой HUD (UI/5) автоматически не включается, материалы меню не проходят StripAllMaterials. Камера меню очищает только depth, поэтому фон не перекрывает интерфейс. Gameplay-фон исключает назначенную UI-камеру из выбора. Камеры с targetTexture не изменяются этой логикой.

ConfigureGameplayCamera вызывается из ApplyCameraMask и перед каждым cull в Camera.onPreCull. Поэтому результат не зависит от интервала сканирования, Time.timeScale или повторного XOR uGUI из GameCameras. Оригинальные маски камер продолжают сохраняться в strippedMasks и восстанавливаться при отключении/выгрузке. Путь главного меню сохранён, GetMenuVisibleMask дополнен слоем uGUI.

Проверки: 2431 assertion, включая production-логику GameMenuCamera с substitutes камер/Canvas, повторные открытия/закрытия и смену UI-камеры, восстановление потёртой маски перед кадром, targetTexture и защиту UI-камеры от использования в качестве фона. Сборка 0 warnings/errors. Реальная отрисовка в запущенной игре не проверена.
## Stable 1.29.26: применение выделения и интерфейс (2026-10-10)

- Всплывающее повторение текста полностью удалено: нет DrawUiTooltip и GUIContent.tooltip у labels/buttons/toggles. Перенос обычного текста сохранён.
- Source/SpriteMaskEditorInput.cs содержит production-обработчик мыши. ID захваченного элемента сохраняется при MouseDown, не заменяется при следующем GetControlID. Выделение завершается по raw MouseUp, включая отпускание за пределами clip и уже consumed событие. Пропущенное отпускание восстанавливается на Repaint по физическому состоянию мыши; потеря focus/смена вкладки/закрытие F9 отменяют capture. Чужой hotControl не сбрасывается.
- Одновременно действует только один жест: правая кнопка и wheel не могут перезаписать левое выделение. Apply доступен после завершения корректного прямоугольника; статус сообщает Selection ready.
- Clear selection и Remove saved mask выводят редактор из result view и позволяют новое выделение. Show original доступен независимо от текущей валидности выделения. Слишком маленькие прямоугольники отсеиваются с учётом epsilon triangulation.
- Уничтоженный renderer освобождает редактор; несовпадающий fingerprint сохранённой маски показывает отдельное сообщение вместо ложного Loaded. Очистка после удаления сохраняет подтверждение удаления.
- GUI group закрывается в finally; GUI.matrix/enabled/color/backgroundColor восстанавливаются после окна мода. Обычный preview drag сбрасывается при закрытии F9, смене detail tab и потере фокуса.
- Проверки: 2460 assertions, включая выполнение production HandleMaskEditorMouse с substitutes Event/GUI/Input, смену control ID, raw MouseUp вне картинки, пропущенный release, focus loss, два mouse buttons, очистку result, возможность Validate/WriteAtomic/Parse результата выделения. В проверках нет полноценного Unity IMGUI/Button rendering; живое меню игры не проверено.
## Stable 1.29.26 — просторный редактор и ресурсы меню

Mask занимает всю рабочую область F9. MenuResources обнаруживает ресурсы сцен меню и Canvas, включая неактивные страницы, Image/RawImage/Text/TextMesh/TMP, шрифты и атласы. MenuResourcePolicy используется поиском и фильтрацией; MaskWorkspaceLayout рассчитывает большую область. MenuViewport исправляет зазоры округления до 2 пикселей, BackgroundCameraFrame согласует Camera.rect и tk2dCamera.CameraSettings.rect с восстановлением. Подробности и ограничения игровой проверки: dist/stable-1.29.26/README.txt.

## Stable 1.29.27 — меню и полноэкранное выделение (2026-10-10)

Source/MaskWorkspaceLayout.cs теперь определяет окно по Screen.width/height, а Source/SpriteMaskEditor.cs оставляет основную панель в одну строку, убирая дополнительные действия под More options. При 1360×768 шахматное поле равно 1312×598. Source/SpriteMaskEditorInput.cs разрешает MouseDown в сером поле с clamp к краям изображения; сохранены safeguards capture/release/Apply.

Source/MenuResources.cs и MenuTk2d.cs обходят Canvas, UIManager и отдельный gameTitle (LogoTitle), постоянный UI в title scene, tk2dTextMesh/tk2dBaseSprite, Text/TextMesh/TMP и atlas pages. Refresh очищает stale search. MenuObjectPreview.cs растеризует реальную геометрию/UV выбранного renderer, не изменяя состояние maskEditorPixels. Полный атлас остаётся доступен.

Source/CustomMenu.cs, CustomMenuGui.cs и CustomMenuStyle.cs задают отдельный Menu design: импорт изображения/GIF/видео, логотип, placement, текст кнопок. Используется только UIManager.mainMenuScreen с Selectable/tk2dUIItem; preserve альфа, selection brightness и исходное оформление. Source/MenuValueOverride.cs отслеживает внешние изменения, читает фактически нормализованный setter результат, восстанавливает только принадлежащее моду значение. Размеры UI/TMP/tk2d изменяются относительно исходного; исходный логотип восстанавливается с учётом смены языка/MenuStyle.

Source/GifAnimation.cs содержит ограниченный decoder и compositing disposal 2/3; MenuMediaPlayback.cs использует Task для GIF и VideoPlayer APIOnly/Freerun/None audio для видео. Unity texture upload, video API и lifecycle выполняются на main thread. F9/manual pause действует до prepareCompleted; fail не показывает старую текстуру и не вызывает рекурсивный Stop/error. Release происходит при выключении, scene change и Unload. MenuMediaPolicy.cs проверяет extension/path и ограничивает импорт; menu-media/removed сохраняет удалённое.

В SettingsAndProfiles.cs добавлены customMenuEnabled/Paused/File/LogoFile/Layout/TextColor/TextScale. Фон игровых сцен и оформление главного меню хранятся отдельно; импорт не перезаписывает одинаковые имена. UI English/Russian сохраняется.

Проверки: 2505 основных assertions, 25 menu discovery, 145932 GIF pixels/media/style/import, всего 148462. Тесты GIF сравнивают кадры с Pillow; остальные Unity API заменены моделями. Все сборки без ошибок/предупреждений. Реальная Hollow Knight загрузила установленную 1.29.27 без ошибок мода. Визуальные проверки и native video playback не завершены: FrameArrived/window capture timed out в Computer Use. Подробности и инструкции — dist/stable-1.29.27/README.txt.
