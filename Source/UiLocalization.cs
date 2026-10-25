using System;
using System.Collections.Generic;
namespace ShaderStripMod
{
    internal static class UiLocalization
    {
        private static readonly Dictionary<string,string> Russian=new Dictionary<string,string>(StringComparer.Ordinal);
        private static readonly Dictionary<string,string> English=new Dictionary<string,string>(StringComparer.Ordinal);
        static UiLocalization()
        {
            foreach(var line in Translations.Split('\n'))
            {
                string trimmed=line.TrimEnd('\r');int split=trimmed.IndexOf(" => ",StringComparison.Ordinal);
                if(split<0) continue;
                string en=trimmed.Substring(0,split),ru=trimmed.Substring(split+4);
                Russian[en]=ru;English[ru]=en;
            }
        }
        internal static string NormalizeLanguage(string value)
        {
            return string.Equals(value,"Russian",StringComparison.OrdinalIgnoreCase)||string.Equals(value,"ru",StringComparison.OrdinalIgnoreCase)||value=="Русский" ? "Russian" : "English";
        }
        internal static string Text(string value,string language)
        {
            if(string.IsNullOrEmpty(value)) return value??"";
            if(value=="1. Open Nearby.\n2. Select the object's Nearby Sprite entry.\n3. Open Mask to see its image.")
                return NormalizeLanguage(language)=="Russian" ? "1. Откройте «Рядом».\n2. Выберите строку Nearby Sprite нужного объекта.\n3. Откройте «Область»: появится изображение." : value;
            string canonical; if(!English.TryGetValue(value,out canonical)) canonical=value;
            string result;if(NormalizeLanguage(language)=="Russian"&&Russian.TryGetValue(canonical,out result)) return result;
            if(canonical!=value) return canonical;
            // Translate UI prefixes only. Resource names, paths, shaders and raw diagnostics stay intact.
            foreach(var prefix in Prefixes)
            {
                string ru; if(!Russian.TryGetValue(prefix,out ru)) continue;
                if(value.StartsWith(prefix,StringComparison.Ordinal)) return (NormalizeLanguage(language)=="Russian"?ru:prefix)+Text(value.Substring(prefix.Length),language);
                if(value.StartsWith(ru,StringComparison.Ordinal)) return (NormalizeLanguage(language)=="Russian"?ru:prefix)+Text(value.Substring(ru.Length),language);
            }
            if(value.EndsWith(" ON",StringComparison.Ordinal)) return Text(value.Substring(0,value.Length-3),language)+(NormalizeLanguage(language)=="Russian"?" ВКЛ":" ON");
            return value;
        }
        private static readonly string[] Prefixes={"Cannot open image: ","Cannot apply: ","Cannot change mask: ","Mask hidden: ","Enabled: ","Disabled: ","Image: ","Color: ","Loaded masks: ","Object: ","Background: ","Logo: ","Menu media error: ","Logo error: ","Button style error: ","Video cannot play: "};
        private const string Translations=@"Gameplay camera (F8) => Камера персонажа (F8)
Camera pinned in this room. => Камера закреплена в этой комнате.
Camera follows the hero. => Камера следует за персонажем.
Pin camera here (F8) => Закрепить камеру здесь (F8)
Release camera (F8) => Открепить камеру (F8)
Pin the current view; the hero can move independently. Released automatically on scene changes, death and cutscenes. Pinning is not saved. => Закрепляет текущий кадр: персонаж может двигаться. Камера открепляется при смене комнаты, смерти и катсценах. Закрепление не сохраняется.
Enter a room to pin the camera. Transitions and cutscenes must finish first. => Войдите в комнату, чтобы закрепить камеру. Дождитесь окончания перехода или катсцены.
Camera pinned here. Press F8 again to release. => Камера закреплена здесь. Нажмите F8 ещё раз, чтобы открепить.
Camera could not be pinned. => Не удалось закрепить камеру.
Camera released for a scene change, respawn or transition. => Камера откреплена при смене комнаты, возрождении или переходе.
Title menu media is configured in Menu design. => Фон и логотип главного меню настраиваются во вкладке «Оформление меню».
Menu design can be edited here. Preview it in the title menu. => Здесь можно изменить оформление. Результат будет виден в главном меню игры.
Choose your menu logo => Выберите свой логотип меню
Choose menu background (image, GIF or video) => Выберите фон меню: изображение, GIF или видео
Choose background image => Выберите изображение фона
More options => Дополнительно
Hide options => Скрыть параметры
Menu design => Оформление меню
Show full atlas => Показать весь атлас
Show object artwork => Показать изображение объекта
Custom title menu => Свой вид главного меню
Background, logo and button text. Applies to the title menu; game buttons keep their actions. => Фон, логотип и текст кнопок главного меню. Действия кнопок игры сохраняются.
Enable custom menu => Включить своё оформление
Import background: image / GIF / video => Загрузить фон: изображение / GIF / видео
Background:  => Фон: 
Not selected => Не выбран
Pause menu animation => Приостановить анимацию меню
Restart / retry media => Перезапустить / повторить загрузку
Background placement => Размещение фона
Import logo (PNG / JPG) => Загрузить логотип (PNG / JPG)
Use game logo => Вернуть логотип игры
Logo:  => Логотип: 
Game logo => Логотип игры
Button text color (keeps selection brightness) => Цвет текста кнопок (яркость выделения сохраняется)
Original => Исходный
Gold => Золотой
Pink => Розовый
Button text size => Размер текста кнопок
Reset menu design => Сбросить оформление меню
Imported menu media => Загруженные файлы меню
As background => На фон
As logo => Как логотип
PNG/JPG backgrounds and logos, animated GIF, MP4/WebM/MOV/M4V/AVI video. Video is silent and looped; codecs depend on Unity/Windows. MP4 H.264 is the preferred format. => PNG/JPG для фона и логотипа, анимированный GIF, видео MP4/WebM/MOV/M4V/AVI. Видео зациклено и без звука. Поддержка кодеков зависит от Unity/Windows; рекомендуется MP4 H.264.
F9 pause also freezes menu animation. Media removal keeps a recoverable copy in menu-media/removed. => Пауза F9 останавливает и анимацию меню. При удалении файла его копия остаётся в menu-media/removed.
Enable the mod visuals with F12 to preview your menu. => Включите замену фона клавишей F12, чтобы увидеть своё меню.
Import a background or logo to create your menu. => Загрузите фон или логотип для своего меню.
Custom logo selected. => Свой логотип выбран.
Custom menu background selected. => Свой фон меню выбран.
Menu media removed (recoverable copy saved). => Файл меню удалён; сохранена резервная копия.
Loading menu media... => Загружаю файл меню...
Menu image loaded. => Изображение меню загружено.
Menu GIF loaded. => GIF меню загружен.
Menu video ready (silent, looped). => Видео меню готово (зациклено, без звука).
Menu media error:  => Ошибка файла меню: 
Logo error:  => Ошибка логотипа: 
Button style error:  => Ошибка оформления кнопок: 
Video cannot play:  => Видео не воспроизводится: 
Video must be smaller than 1 GiB. => Размер видео должен быть меньше 1 ГиБ.
Image/GIF must be smaller than 64 MiB. => Размер изображения или GIF должен быть меньше 64 МиБ.
Cannot decode menu image. => Не удалось прочитать изображение меню.
Image dimensions exceed 8192 or 8 million pixels. => Размеры изображения превышают 8192 пикселя по стороне или 8 миллионов пикселей всего.
Invalid PNG/JPG image dimensions. => Неверные размеры изображения PNG/JPG.
Menu image dimensions exceed 8192. => Размеры изображения меню превышают 8192 пикселя.
Logo dimensions exceed 8192. => Размеры логотипа превышают 8192 пикселя.
Choose a PNG/JPG logo smaller than 64 MiB. => Выберите логотип PNG/JPG размером меньше 64 МиБ.
Cannot decode logo image. => Не удалось прочитать изображение логотипа.
Choose a PNG/JPG logo. => Выберите логотип PNG/JPG.
Unsupported menu media format. => Неподдерживаемый формат файла меню.
File is empty or exceeds the media size limit. => Файл пуст или превышает допустимый размер.
Video dimensions exceed 8192. => Размеры видео превышают 8192 пикселя.
Video preparation timed out. Try an MP4 encoded with H.264. => Истекло время подготовки видео. Попробуйте MP4 с кодеком H.264.
Choose a file from the imported menu media list. => Выберите файл из списка загруженных файлов меню.
Media must stay inside menu-media. => Файл должен находиться в папке menu-media.
GIF file exceeds 64 MiB. => Размер GIF превышает 64 МиБ.
GIF dimensions exceed 4096 or 8 million pixels. => Размеры GIF превышают 4096 пикселей или 8 миллионов пикселей в кадре.
GIF decoded memory/frame limit reached; use a video for long animations. => GIF превышает ограничение памяти или кадров. Для длинной анимации используйте видео.
Invalid GIF signature. => Неверный заголовок GIF.
GIF has no complete image sequence. => В GIF нет полной последовательности кадров.
Truncated GIF. => GIF обрывается до конца файла.
Incomplete GIF LZW image. => Кадр GIF неполный.
Back to browser => Вернуться к списку
Mask selection => Выделение области
Menu => Меню
Left drag: keep the framed region. Wheel: zoom. Right drag: pan. Esc: close without saving. => Левой кнопкой выделите нужную часть. Колесо: масштаб. Правая кнопка: перемещение. Esc: закрыть без сохранения.
Menu resources support Info, Preview and Export. Visibility rules and masks currently apply to room objects. => Ресурсы меню можно изучать и экспортировать во вкладках «Информация», «Просмотр» и «Экспорт». Правила видимости и выделение области пока работают для объектов локаций.
Object:  => Объект: 
Core Runtime - Custom values => Основной цикл: свои значения
ApplyStrip interval => Интервал ApplyStrip
Proxy update interval => Интервал копий
Browser refresh => Обновление списка
Frame budget => Бюджет кадра
GC budget => Бюджет GC
Visible entries => Видимые строки
List overscan rows => Запас строк списка
Use unscaled time => Независимое от паузы время
Renderer path cache => Кэш путей renderer
Preserve cache flag => Кэш правил видимости
Projectile cache flag => Кэш снарядов
Browser snapshot flag => Кэш списка
Draw source cache => Кэш источников отрисовки
Virtualized list => Виртуализация списка
Max path cache => Лимит кэша путей
Show counters => Показывать счётчики
Show cache stats => Статистика кэша
Detailed metrics => Подробные метрики
GC heap delta => Изменение памяти GC
Stagger scheduler => Распределять задачи по кадрам
Scene warmup flag => Подготовка кэша сцены
Capture interval => Интервал сканирования
Recent lifetime => Время хранения снарядов
Preserved sync => Синхронизация объектов
Max renderers => Лимит renderer
Max particle renderers => Лимит частиц
seconds => секунды
KB/frame => КБ/кадр
Only Tab => Во вкладке
Browser => В F9
Always => Всегда
Manual => Вручную
Quality => Качество
Balanced => Баланс
Performance => Скорость
Ultra => Ультра
Custom => Свой
Safe => Безопасный
Aggressive => Агрессивный
Q->Custom => Качество → свой
B->Custom => Баланс → свой
P->Custom => Скорость → свой
U->Custom => Ультра → свой
Game pause while browser is open: enabled. => Пауза при открытом F9 включена.
Game pause while browser is open: disabled. => Пауза при открытом F9 отключена.
Loaded => Загружено
Entities => Сущности
Nearby => Рядом
Atlases => Атласы
Rules => Правила
Projectiles => Снаряды
Dump => Дамп
Close => Закрыть
Info => Данные
Preview => Просмотр
Export => Экспорт
Settings => Настройки
Set => Настр.
Perf => Производ.
Mask => Область
Select an entry. => Выберите строку.
Texture Browser => Обзор текстур
Renderer rules => Правила отображения
Asset rules => Правила ресурсов
Path rules => Правила путей
Export PNG => Экспорт PNG
Export Atlas => Экспорт атласа
Interface language => Язык интерфейса
Background: choose a color or image => Фон: выберите цвет или изображение
Choose image... => Выбрать изображение...
Image placement => Размещение изображения
Fill screen (crop edges) => Заполнить экран (обрезать края)
Fit entire image (keep proportions) => Вписать целиком (сохранить пропорции)
Stretch to screen => Растянуть по экрану
Imported images => Загруженные изображения
No imported images. Use Choose image... to select a file. => Изображений пока нет. Нажмите «Выбрать изображение...».
Remove => Удалить
Remove clears an image from the list. A recoverable copy stays in background-images/removed. => Изображение удаляется из списка. Резервная копия остаётся в background-images/removed.
Pause time and sound while F9 is open => Останавливать время и звук при открытом F9
Preserved objects relative to the hero => Расположение сохранённых объектов относительно героя
Behind hero => За героем
In front of hero => Перед героем
Green => Зелёный
Blue => Синий
Red => Красный
Yellow => Жёлтый
Cyan => Голубой
Magenta => Пурпурный
Orange => Оранжевый
White => Белый
Black => Чёрный
Save => Сохранить
Downloads => Загрузки
Mod => Мод
Zoom => Масштаб
No preview texture. => Нет текстуры для просмотра.
No renderer => Нет renderer
Renderer => Renderer
Renderer ON => Renderer ВКЛ
Path Renderers => Renderer по пути
No projectile => Нет снаряда
Projectile => Снаряд
Projectile ON => Снаряд ВКЛ
Clear => Сбросить
Sprite => Спрайт
Texture => Текстура
Material => Материал
Object => Объект
Group => Группа
Reload => Обновить
Compact => Уплотнить
Clean Ignored => Очистить лишние
Remove Rule => Удалить правило
No object path. => Нет пути объекта.
Back to image => К изображению
Saved masks => Сохранённые области
Reload image => Обновить изображение
Apply and close => Применить и закрыть
Show original => Показать исходник
Preview result => Показать результат
Clear selection => Сбросить выделение
Remove saved mask => Удалить сохранённую область
Fit image => Показать целиком
No saved masks yet. => Сохранённых областей пока нет.
Enable => Включить
Disable => Отключить
Enabled:  => Включено: 
Disabled:  => Отключено: 
Image:  => Изображение: 
Color:  => Цвет: 
Cannot open image:  => Не удалось открыть изображение: 
Cannot apply:  => Не удалось применить: 
Cannot change mask:  => Не удалось изменить область: 
Mask hidden:  => Область скрыта: 
Loaded masks:  => Загружено областей: 
Drag with the left mouse button. Inside the yellow frame stays visible; outside is hidden. => Обведите прямоугольник левой кнопкой мыши. Внутри жёлтой рамки останется видимая часть; снаружи всё скроется.
Wheel: zoom. Right drag: pan. Esc: close without saving. => Колесо: масштаб. Правая кнопка: сдвиг. Esc: закрыть без сохранения.
Saved mask loaded as a rectangle. Changes are saved only when you press Apply and close. => Сохранённая область открыта как прямоугольник. Изменения сохранятся только после «Применить и закрыть».
Drag a rectangle around the part to KEEP. The shaded area will be hidden. Preview the result, then Apply and close. => Обведите часть, которую нужно ОСТАВИТЬ. Затемнённая часть будет скрыта. Проверьте результат и нажмите «Применить и закрыть».
Saved mask no longer matches this sprite. Select a new rectangle, then Apply and close. => Сохранённая область не соответствует этому спрайту. Выделите новый прямоугольник и нажмите «Применить и закрыть».
Selection ready. Click Apply and close. => Выделение готово. Нажмите «Применить и закрыть».
Select a rectangle with a nonzero width and height. => Выделите прямоугольник с ненулевой шириной и высотой.
The image changed. Reload image and select the rectangle again. => Изображение изменилось. Нажмите «Обновить изображение» и выделите область заново.
Separate alpha texture dimensions do not match. => Размер отдельного канала прозрачности не совпадает с текстурой.
This renderer has no visible image. Select the decoration's Nearby Sprite entry. => У этого renderer нет видимого изображения. Выберите Nearby Sprite нужной декорации.
Select an object in Nearby. => Выбери объект во вкладке Nearby.
Masks are for decorations. The hero, projectiles and proxy objects cannot be edited. => Области предназначены для декораций. Герой, снаряды и служебные копии не редактируются.
The object has multiple materials. Select a separate sprite. => У объекта несколько материалов. Выбери отдельный спрайт.
Select a regular static sprite. => Выбери обычный статичный спрайт.
This object type does not support masks. => Этот тип объекта не поддерживает выделение области.
This object's geometry cannot be safely clipped. Select a separate sprite. => Геометрию этого объекта нельзя безопасно разделить. Выбери отдельный спрайт.
This object has no accessible image. => У этого объекта нет доступного изображения.
Only flat sprites and decorations are supported. => Поддерживаются плоские спрайты и декорации.
The selection contains no image pixels. => В выделенной области нет изображения объекта.
The source image changed. Open Mask and save the selection again. => Исходное изображение изменилось. Открой область и сохрани выделение заново.
Saved mask limit reached: 512. => Достигнут предел: 512 сохранённых областей.
Mask saved: only the selected part is visible. => Область сохранена: видна только выделенная часть.
Mask enabled. => Область включена.
Mask disabled. Other visibility rules still apply. => Область отключена. Остальные правила видимости продолжают действовать.
Mask removed. A backup is in sprite-masks/removed. => Область удалена. Резервная копия сохранена в sprite-masks/removed.
Profile => Профиль
Reset => Сбросить
Apply Custom => Применить
Reset Defaults => По умолчанию
Capture Now => Сканировать
Clear Caches => Очистить кэш
Caching => Кэширование
Diagnostics => Диагностика
Counters disabled. => Счётчики отключены.
Current cost => Текущие затраты
Projectile Capture => Сканирование снарядов
Projectile Performance => Производительность снарядов
0 interval = every frame. => Интервал 0 = каждый кадр.
Hard caps are applied to risky values. => Для опасных значений действуют ограничения.
Custom values 0..999 => Свои значения 0..999
0 intervals = every frame. High limits can cause severe lag. => Интервал 0 = каждый кадр. Высокие лимиты могут вызвать зависания.
";
    }
}
