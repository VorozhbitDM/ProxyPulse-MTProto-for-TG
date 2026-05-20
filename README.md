# ProxyPulse

**Поиск MTProto-прокси и проверка доступности** — одним кликом подключение в Telegram. Без VPN, без API-ключей и без ручной настройки.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2%2B-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Windows](https://img.shields.io/badge/Platform-Windows-0078D6)](https://github.com/VorozhbitDM/ProxyPulse-MTProto-for-TG)

---

## Зачем это нужно

Telegram иногда недоступен напрямую. В каналах и архивах публикуют MTProto-прокси, но вручную искать, проверять пинг и копировать `secret` неудобно.

**ProxyPulse** делает это за вас:

- собирает прокси из общедоступных источников (через web.archive.org);
- проверяет **доступность** (TCP, с таймаутом);
- сортирует по задержке;
- открывает выбранный прокси в **Telegram** одним кликом.

---

## Скриншот

> Добавьте сюда скриншот главного окна: перетащите `docs/screenshot.png` в README или вставьте в Issue и скопируйте URL.

---

## Быстрый старт

1. Скачайте **`ProxyPulse.exe`** из [Releases](https://github.com/VorozhbitDM/ProxyPulse-MTProto-for-TG/releases) (раздел появится после первой публикации).
2. Запустите файл (достаточно **одного exe**, без dll).
3. Нажмите **«Начать поиск»**.
4. Кликните по прокси в списке — Telegram предложит подключение.

### Требования

| | |
|---|---|
| ОС | Windows 10 / 11 |
| Runtime | [.NET Framework 4.7.2+](https://dotnet.microsoft.com/download/dotnet-framework/net472) (часто уже установлен) |
| Сеть | Доступ к `web.archive.org` |
| Telegram | Установленный Telegram Desktop или из Microsoft Store |

---

## Сборка из исходников

```powershell
git clone https://github.com/VorozhbitDM/ProxyPulse-MTProto-for-TG.git
cd ProxyPulse-MTProto-for-TG
.\build.ps1
```

Готовый файл: `dist\ProxyPulse.exe`

Нужен [.NET SDK](https://dotnet.microsoft.com/download) (для сборки) или MSBuild + .NET Framework 4.7.2.

---

## Как это устроено

- **Источник списка** — снимки публичного канала в [Internet Archive](https://web.archive.org), без прямого доступа к заблокированным доменам.
- **Парсинг** — ссылки `tg://proxy/?…` и `t.me/proxy?…`.
- **Проверка** — TCP-подключение к `server:port`, лимит ~2,5 с на прокси.
- **Подключение** — `tg://proxy?…` и запасной `https://t.me/proxy?…`.

---

## Поддержать проект

Проект бесплатный и с открытым исходным кодом. Если он вам помог — можно поддержать разработку любым удобным способом:

| Способ | Ссылка | Комментарий |
|--------|--------|-------------|
| **GitHub Sponsors** | [github.com/sponsors/VorozhbitDM](https://github.com/sponsors/VorozhbitDM) | Нужно [включить Sponsors](https://github.com/sponsors) в настройках GitHub |
| **Boosty** | *добавьте ссылку* | Удобно для аудитории из СНГ, рубли |
| **Ko-fi** | [ko-fi.com](https://ko-fi.com) | Международные донаты, простая регистрация |
| **Donation Alerts** | [donationalerts.com](https://www.donationalerts.com) | Стримы и разовые переводы |
| **СБП / карта** | *ссылка банка или Tribute* | Часто через Boosty или отдельный сервис |

После настройки кнопки обновите ссылку в `src/ProxyPulse/AppLinks.cs` (`Donate`) или оставьте ведение на этот раздел README.

В приложении: **Справка → Задонатить** открывает этот раздел на GitHub.

---

## Публикация exe в Releases

1. Соберите: `.\build.ps1`
2. На GitHub: **Releases → Create a new release**
3. Тег, например `v2.3`
4. Прикрепите `dist/ProxyPulse.exe`
5. Кратко опишите изменения

Так пользователи смогут скачивать программу без клонирования репозитория.

---

## Лицензия

[MIT](LICENSE) — используйте и распространяйте свободно с указанием авторства.

---

## Автор

[Denis Vorozhbit](https://github.com/VorozhbitDM) · репозиторий: [ProxyPulse-MTProto-for-TG](https://github.com/VorozhbitDM/ProxyPulse-MTProto-for-TG)
