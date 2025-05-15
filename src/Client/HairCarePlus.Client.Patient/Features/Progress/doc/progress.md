# Progress Page — Redesign v3 (June 2025)

## Цель
Создать минималистичную, но информативную страницу восстановления пациента, ориентированную на фото-контент, краткие AI-и врачебные комментарии, а также наглядное отображение долгосрочного пути (1 год) и ближайших ограничений.

## Общая схема
```
┌ Header ─────────────────────────────────────────────────────────────────┐
│  ▇▇▇▇▇▇▇  Year-Timeline  ▇▇▇▇▇▇▇   ● ← today                        │
│  ✂ 14d   ☀ 20d   🏋 28d   … (horizontal scroll if >4)               │
└─────────────────────────────────────────────────────────────────────────┘
┌ Card (Day 35) ──────────────────────────────────────────────────────────┐
│ 📷 swipe gallery (1 / 3)                                               │
│ AI-report                                                               │
│ Doctor comment                                                          │
└─────────────────────────────────────────────────────────────────────────┘
┌ Card (Day 34) …                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

*Вертикальный скролл; Header фиксируется (sticky) или сворачивается до компактной полосы.*

---

## 1 Header
### 1.1 Year Timeline (0–12 мес)
* Толщина линии: **8–10 px**.
* Сегменты: `0-1 m`, `1-3 m`, `3-6 m`, `6-12 m`.
* Текущая дата — контрастный маркер/ползунок.
* Тап по маркеру → popup «День N из 365» + короткий статус.

### 1.2 Restriction Timers
* Формат: горизонтальный `CollectionView` (иконки + days).
* Сортировка — ближайшие слева.
* Показать 2-4 иконки; при избытке — горизонтальный скролл.
* Цвет круга: `Primary` (>1 дня), `SurfaceVariant` (≤24 ч), `Surface` (завершено).

---

## 2 Timeline Feed (Vertical)
### 2.1 Карточка дня
| Элемент | Детали |
|---------|--------|
| Header  | «День NN / dd MMM yyyy» или «День NN». Крупный/полужирный. |
| Gallery | Главное фото + свайп влево/вправо → до 3 ракурсов. **Без** точек-индикаторов. |
| AI block | 1-3 строки, иконка «🤖». Меньший шрифт. |
| Doctor block | Комментарий врача, иконка/аватар «👩‍⚕️». Основной текст. |

*Карточка — `Border`, скругление 12, фон `CardBackground[_Light|_Dark]`, тень 0–2 dp.*
*Отступ между карточками: **16 px** (mobile), 24 px (tablet).*  

### 2.2 Поведение текста
* `MaxLines` 2-3; тап по тексту → full-screen sheet / expand внутри карточки.

---

## 3 Взаимодействие
* **Vertical scroll** — привычная лента.
* **Pull-to-Refresh** (`RefreshView`) — обновить ленту/комментарии.
* **Sticky Header** — годовой бар + таймеры остаются видимыми.
* **Swipe** внутри фото — листает ракурсы.
* **Tap** по тексту — разворачивает.

---

## 4 Стилистика
* Цвета — те же, что в чате: `Primary`, акцент, светлые/dark Surface; без градиентов.
* Шрифты:  
  • H1 — 16-18 sp (день)  
  • Body — 14 sp (doctor)  
  • Caption — 12 sp (AI, метки)
* Иконки — FontAwesome (FA 6) `solid` набор, одна цветовая схема.
* Отступы: 16 px края, 8–12 px между вложенными элементами.

---

## 5 Data / CQRS Flow
| Trigger | Query / Command | Обновляет |
|---------|-----------------|-----------|
| App start / Refresh | `GetProgressFeedQuery` (range 7 days) | Feed карточек |
| New photo saved | `PhotoCapturedMessage` | Feed (добавить день) |
| Restriction updated | `RestrictionsChangedMessage` | Timers в Header |
| Pull-to-Refresh | оба запроса | Header + Feed |

---

## 6 TODO for implementation
1. **UI**  
   a. Создать `YearTimelineView` (custom drawn).  
   b. Переоформить `RestrictionTimersView` в иконки.  
   c. Карточка — вынести в `ProgressCardView` для reuse.
2. **Sticky Header** — `CollectionView.Header + StickyHeaderBehavior` или MAUI-Shell ScrollHandler.
3. **Pull-to-Refresh** — обёрнуть ленту в `RefreshView`.
4. **Animations** — лёгкое увеличение карточки при тапе, плавный скролл к top на refresh.
5. **Theming** — добавить цвета в `Colors.xaml`, стили в `ProgressStyles.xaml`.
6. **Unit Tests** — ViewModel: корректная сортировка, реакция на messages.

---
© HairCare+, 2025 