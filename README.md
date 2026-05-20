<p align="center">
  <img alt="ProxyPulse — MTProto-прокси для Telegram" src="docs/banner.png" width="900">
</p>

<p align="center">
  <strong>Поиск MTProto-прокси и проверка доступности</strong><br>
  Подключение в Telegram одним кликом · Windows · без VPN и API-ключей
</p>

<p align="center">
  <a href="https://github.com/VorozhbitDM/ProxyPulse-MTProto-for-TG/releases/latest"><img src="https://img.shields.io/github/v/release/VorozhbitDM/ProxyPulse-MTProto-for-TG?style=flat-square&label=Release" alt="Release"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet-framework/net472"><img src="https://img.shields.io/badge/.NET%20Framework-4.7.2%2B-512BD4?style=flat-square" alt=".NET Framework"></a>
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square" alt="Windows">
</p>


---

## Возможности

Telegram иногда недоступен напрямую. В каналах и архивах публикуют MTProto-прокси, но вручную искать, проверять задержку и копировать `secret` неудобно.

**ProxyPulse** делает это за вас:

- собирает прокси из общедоступных источников;
- проверяет **доступность** (TCP, с таймаутом);
- сортирует по задержке;
- открывает выбранный прокси в **Telegram** одним кликом.

---

## Скриншоты

<details>
<summary><strong>Стартовый экран и результаты</strong></summary>

<br>

| Стартовый экран | Результаты поиска |
|:---:|:---:|
| ![Стартовый экран](docs/screenshot-welcome.jpg) | ![Результаты](docs/screenshot-results.jpg) |

</details>

---

## Быстрый старт

1. Скачайте **`ProxyPulse-v2.4-win-x64.zip`** в [релизе v2.4](https://github.com/VorozhbitDM/ProxyPulse-MTProto-for-TG/releases/tag/v2.4) (раздел *Assets*).
2. Распакуйте архив и запустите **`ProxyPulse.exe`** (один файл, без dll).
3. Нажмите **«Начать поиск»**.
4. Кликните по прокси в списке — Telegram предложит подключение.

### Требования

| | |
|---|---|
| ОС | Windows 10 / 11 |
| Runtime | [.NET Framework 4.7.2+](https://dotnet.microsoft.com/download/dotnet-framework/net472) (часто уже установлен) |
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

## Поддержка и развитие проекта

<p align="center">
  <a href="https://yoomoney.ru/to/4100119536071248">
    <img src="docs/yoomoney-support.png" alt="Перевести на ЮMoney" width="280" height="56">
  </a>
</p>

Проект с открытым исходным кодом. Если ProxyPulse вам помог — буду благодарен любой сумме.

---

## Лицензия

[MIT](LICENSE) — используйте и распространяйте свободно с указанием авторства.

---

<p align="center">
  <a href="https://github.com/VorozhbitDM">Denis Vorozhbit</a> ·
  <a href="https://github.com/VorozhbitDM/ProxyPulse-MTProto-for-TG">ProxyPulse-MTProto-for-TG</a>
</p>
