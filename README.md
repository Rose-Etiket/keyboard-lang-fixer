# Keyboard Language Fixer

A free, open-source background utility for Windows that fixes one of the most annoying typing problems for bilingual users: **typing a whole word or sentence in the wrong keyboard language** (e.g. typing English while the Persian layout is active, or vice versa) — without you noticing until it's too late.

It watches what you type (locally, on your own machine — nothing is sent anywhere), and the moment it recognizes that the word you just finished doesn't make sense in the language you're typing in but *would* make sense in the other one, it silently deletes it, retypes it correctly, and switches your keyboard language for you. Similar in spirit to the well-known **Punto Switcher** for Russian/English — this project brings the same idea to **Persian ⇄ English**.

> این یک ابزار متن‌باز و رایگان است، ساخته‌شده برای حل یک مشکل بسیار آزاردهنده و همیشگی: زمانی که با کیبورد فارسی/انگلیسی تایپ می‌کنیم و متوجه نمی‌شویم زبان اشتباه است، تا وقتی که چند کلمه یا یک جمله کامل را اشتباه تایپ کرده‌ایم.

---

## ✨ What it does

- Runs quietly in the system tray — no visible window, no interruption to normal typing.
- Detects, right after you finish a word, whether it's a real word in the language you're currently typing in.
- If it isn't, but the *same physical keys* would spell a real word in the other installed layout (Persian/English), it:
  1. Deletes what you just typed,
  2. Retypes the correct word,
  3. Switches your active keyboard layout so you can keep typing normally,
  4. Plays a short, quiet beep so you know it happened (optional, can be turned off).
- **Only ever acts on the first word after you click, switch windows, or move the caret** — never in the middle of a sentence you're already typing, so it won't interfere if you deliberately mix languages mid-paragraph.
- Everything runs 100% locally. No network access, no telemetry, no data collection.

## 📥 Download

**[⬇️ Download the latest Windows build](https://github.com/Rose-Etiket/keyboard-lang-fixer/releases/latest)**

1. Download and unzip `KeyboardLangFixer-win-x64.zip` from the [Releases page](https://github.com/Rose-Etiket/keyboard-lang-fixer/releases/latest).
2. Run `KeyboardLangFixer.exe`. A small icon appears in your system tray — that's it, it's running.
3. To have it start automatically with Windows, put a shortcut to the exe in your Startup folder (`Win+R` → `shell:startup`).

No installation, no admin rights, no dependencies to install — it's a single self-contained `.exe`.

### Requirements

- Windows 10 or 11, 64-bit.
- Both **English (US)** and **Persian** keyboard layouts must be installed in Windows (Settings → Time & Language → Language & region). If either is missing, the tray menu will say so and the app stays idle.

## ⚙️ Tray menu options

Right-click the tray icon for:
- **ثبت گزارش تشخیصی (log)** — enable/disable a local diagnostic log file, off by default not required for normal use, useful only when troubleshooting.
- **بوق هنگام تغییر زبان** — enable/disable the short beep played when a correction happens.
- **خروج** — quit.

## 🧠 How it works (for the curious / for contributors)

A global low-level keyboard hook (`WH_KEYBOARD_LL`) observes physical key presses without blocking them. For each word (ended by space/enter/tab/punctuation), it re-interprets the *same physical keys* under both the Persian and English keyboard layouts using `ToUnicodeEx`, and checks each interpretation against a bundled dictionary (`data/en_words.txt`, `data/fa_words.txt`). If the current interpretation isn't a real word but the other one is, it corrects it via `SendInput` and requests a layout switch (`WM_INPUTLANGCHANGEREQUEST`) for the focused window. See the source in [`src/`](src) — it's a small, single-purpose codebase, easy to read end to end.

## 🤝 Why open source — و چرا متن‌باز شد

This project is released as free, open-source software specifically so that **more Iranian developers can join in and make it better** — more accurate word lists, smarter detection, support for more languages/layouts, an installer, auto-start support, etc. The goal is simple: a free, no-fuss tool that any Persian-speaking Windows user can install in ten seconds and forget about.

این پروژه به این دلیل متن‌باز شده که **برنامه‌نویسان بیشتری از ایران** بتوانند در توسعه‌ی آن مشارکت کنند — دیکشنری‌های بهتر و کامل‌تر، تشخیص هوشمندتر، پشتیبانی از زبان‌های دیگر، نصب‌کننده‌ی رسمی و امکاناتی از این دست. هدف نهایی این است که کاربران فارسی‌زبان ویندوز بتوانند به‌صورت کاملاً رایگان و ساده از این ابزار استفاده کنند، بدون نیاز به دانش فنی خاصی.

Pull requests, issues, and word-list contributions are all welcome.

## 🛠️ Building from source

```bash
git clone https://github.com/Rose-Etiket/keyboard-lang-fixer.git
cd keyboard-lang-fixer/src
dotnet run -c Release
```

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). To produce a portable single-file build like the one in Releases:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## ⚠️ Known limitations

- Currently supports Persian ⇄ English only.
- Correction only triggers at the start of a typing "streak" (after a click/focus/caret move), by design — see above.
- Very long unbroken strings (URLs, codes, passwords) are intentionally left untouched.
- Word lists are a reasonable starting point (~10k English / ~29k Persian words) but not exhaustive — false negatives (a wrong word not getting corrected) are more likely than false positives.

## 🙏 Credits

The bundled word lists come from these open sources:
- English: [first20hours/google-10000-english](https://github.com/first20hours/google-10000-english)
- Persian: [jadijadi/persianwords](https://github.com/jadijadi/persianwords) (`persian_dict_19k.txt`, CC0-1.0)

## 📄 License

MIT — see [LICENSE](LICENSE). Free for anyone to use, modify, and share.
