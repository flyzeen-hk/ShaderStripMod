# ShaderStripMod visual rules — formatVersion 1

Дата фиксации формата: 2026-08-14  
Реализация: `Source/VisualRules/VisualRuleCodec.cs` и `VisualRuleFileStore.cs`

## Файл

- Имя runtime-файла: `ShaderStripMod.visual-rules.txt`.
- Кодировка: UTF-8 без BOM; некорректный UTF-8 считается ошибкой чтения.
- Первая непустая строка данных обязана быть `formatVersion=1`.
- Пустые строки игнорируются.
- Строки, начинающиеся после trim с `#` или `//`, являются комментариями. При повторной записи сохраняются только комментарии перед `formatVersion`.
- Неподдерживаемая версия отклоняется целиком. Миграция появится только вместе с будущим `formatVersion=2`.

## Грамматика

В обозначениях ниже `TAB` — один настоящий символ табуляции:

```text
document     := comments "formatVersion=1" newline { comment | blank | rule-line }
rule-line    := "rule" TAB field { TAB field } newline
field        := key "=" escaped-value
key          := непустое имя поля без TAB и "="
escaped-value:= текст с обязательным экранированием специальных символов
```

Обязательные поля каждого правила:

- `id` — непустой и уникальный без учёта регистра;
- `ruleCategory` — `Color`, `Replacement`, `StaticDisplay` или `OriginalBlock`;
- `scope` — `Renderer`, `Object`, `ProjectileFamily`, `Group`, `Sprite`, `Texture`, `Material` или `Global`.

Для `OriginalBlock` обязателен `channelMask=Color|Replacement|StaticDisplay|All`, а scope может быть только `Renderer` или `Object`. Для остальных категорий канал выводится из `ruleCategory`; записанный вручную `channelMask` не меняет его.

## Экранирование значений

```text
\       -> \\
TAB     -> \t
CR      -> \r
LF      -> \n
=       -> \=
```

Неизвестная escape-последовательность и одиночный `\` в конце значения делают только эту строку ошибочной. Неизвестные имена полей сохраняются в `ExtraFields` и записываются обратно.

## Defaults

- `enabled=true`
- `priority=0`
- `stableOrder=номер строки`, если поле отсутствует
- `identityVersion=1`
- индексы/размеры, не заданные правилом: `-1`
- `intensity=1`, `preserveAlpha=true`
- `scaleX=1`, `scaleY=1`, `anchorX=0.5`, `anchorY=0.5`
- остальные bool — `false`, строки — пустые

Writer всегда сортирует правила по возрастанию `stableOrder`, затем по `id`, и записывает канонические Unity-имена без суффикса `(Clone)`.

## Golden-примеры

Здесь `[TAB]` означает настоящий TAB.

Минимальное правило:

```text
formatVersion=1
rule[TAB]id=minimal[TAB]ruleCategory=Color[TAB]scope=Global
```

Exact renderer:

```text
rule[TAB]id=enemy-body[TAB]ruleCategory=Replacement[TAB]scope=Renderer[TAB]sceneName=GG_Test[TAB]objectPath=Root/Enemy/Body[TAB]hierarchyDiscriminator=0/3/1[TAB]rendererType=SpriteRenderer[TAB]rendererComponentIndex=1[TAB]materialIndex=0[TAB]texturePropertyName=_MainTex
```

Windows path, Unicode и неизвестное поле:

```text
rule[TAB]id=жук\=boss[TAB]ruleCategory=Replacement[TAB]scope=Global[TAB]replacementRelativePath=images\\boss\=final\tvariant.png[TAB]futureField=future\=value\\data
```

Повреждённая строка изолируется диагностикой, а следующие корректные строки продолжают читаться:

```text
rule[TAB]id=broken[TAB]ruleCategory=NoSuchCategory[TAB]scope=Global
```

## Атомарное сохранение

`SaveAtomic` выполняет canonical serialize, проверочное чтение из памяти, запись временного файла в каталоге назначения, `Flush(true)`, повторное чтение временного файла, timestamp backup существующего файла и только затем `File.Replace`/`File.Move`. При ошибке рабочий файл не заменяется, временный файл удаляется best effort.
