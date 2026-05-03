---
name: unity-zenject-dependencies
description: Не использовать FindFirstObjectByType в Unity-коде; получать зависимости через Zenject. Применять при создании, правке или ревью Unity C# скриптов, MonoBehaviour, сервисов, UI и геймплейной логики, где нужна ссылка на другой объект или сервис.
---

# Unity Zenject Dependencies

## Правило

Не испольуем `FindFirstObjectByType`.

Используем вместо этого Zenject.

## Как писать

- Для зависимостей в `MonoBehaviour` используй `[Inject]`-метод или `[Inject]`-поля в стиле проекта.
- Для обычных C# классов предпочитай constructor injection.
- Если объект создаётся как prefab, проверь, что он создаётся через Zenject (`DiContainer`, factory, installer binding), а не обычным `Instantiate`, если ему нужны зависимости.
- Для ссылок на сценовые объекты добавляй binding в installer или явно прокидывай dependency через prefab/factory.
- Не заменяй `FindFirstObjectByType` на другие глобальные поиски вроде `FindObjectOfType`, `GameObject.Find` или `FindWithTag`.

## При правке существующего кода

- Удали поиск объекта из runtime-кода.
- Добавь явную зависимость через Zenject.
- Проверь, где создаётся объект, и добавь binding/factory только в ближайшее подходящее место.
- Не добавляй лишние service locator обёртки вокруг контейнера.
