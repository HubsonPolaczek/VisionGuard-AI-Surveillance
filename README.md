# 👁️ VisionGuard AI

**VisionGuard** to inteligentny system monitoringu hybrydowego, łączący moc sztucznej inteligencji (Computer Vision) z nowoczesnym interfejsem desktopowym.

System wykrywa ludzi w czasie rzeczywistym, wysyła alerty przez sieć lokalną, uruchamia alarm dźwiękowy i automatycznie gromadzi dowody zdjęciowe.

![Python](https://img.shields.io/badge/Python-3.11-yellow) ![YOLOv8](https://img.shields.io/badge/AI-YOLOv8-blue) ![C#](https://img.shields.io/badge/C%23-WPF%20.NET%208-purple) ![UDP](https://img.shields.io/badge/Protocol-UDP-red)

## 🚀 Jak to działa? (Architektura)

Projekt składa się z dwóch niezależnych modułów komunikujących się przez **UDP Sockets**:

1.  **Mózg (AI Engine - Python):**
    * Pobiera obraz z kamery w czasie rzeczywistym.
    * Wykorzystuje model **YOLOv8** do detekcji obiektów (klasa: *Person*).
    * Zarządza folderem `dowody/` (Loop Recording - trzyma tylko 50 najnowszych zdjęć).
    * Wysyła ramki danych JSON przez sieć lokalną (localhost:5005).

2.  **Centrum Dowodzenia (Security Client - C# WPF):**
    * Nasłuchuje na porcie UDP 5005.
    * Wizualizuje stan zagrożenia (Zielony/Czerwony ekran).
    * Odtwarza **sygnał alarmowy** po wykryciu intruza.
    * Prowadzi **Dziennik Zdarzeń (Live Log)**.
    * Umożliwia szybki dostęp do zebranych dowodów jednym kliknięciem.

## 🛠️ Technologie

* **Python:** OpenCV, Ultralytics (YOLO), Socket, OS/Glob.
* **C#:** WPF (XAML), Async/Await, System.Media, System.Text.Json, ObservableCollection.
* **Sieć:** Protokół UDP (Low Latency).

---
*Projekt edukacyjny stworzony w celu nauki integracji systemów i AI.*
