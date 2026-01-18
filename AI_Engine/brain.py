import cv2
from ultralytics import YOLO
import math
import socket
import json
import time
import os
import glob  # NOWOŚĆ: Do łatwego szukania plików

# --- KONFIGURACJA ---
CONFIDENCE_THRESHOLD = 0.5
UDP_IP = "127.0.0.1"
UDP_PORT = 5005

# USTAWIENIA DOWODÓW
EVIDENCE_FOLDER = "dowody"
PHOTO_DELAY = 5.0  # (ZMIANA) Rób zdjęcie raz na 5 sekund, nie częściej
MAX_PHOTOS = 50  # (NOWOŚĆ) Trzymaj maksymalnie 50 zdjęć w folderze

if not os.path.exists(EVIDENCE_FOLDER):
    os.makedirs(EVIDENCE_FOLDER)


def manage_storage():
    """Funkcja usuwająca najstarsze zdjęcia, jeśli przekroczono limit"""
    # Pobierz listę wszystkich zdjęć .jpg w folderze
    files = glob.glob(os.path.join(EVIDENCE_FOLDER, "*.jpg"))

    # Jeśli jest ich więcej niż limit
    if len(files) >= MAX_PHOTOS:
        # Posortuj je po czasie utworzenia (najstarsze pierwsze)
        files.sort(key=os.path.getmtime)

        # Usuń najstarsze
        try:
            os.remove(files[0])
            print(f"[AUTO-CLEAN] Usunięto stare zdjęcie: {files[0]}")
        except Exception as e:
            print(f"[BLAD] Nie udało się usunąć pliku: {e}")


def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print(f"[INFO] Start VisionGuard AI. Limit zdjęć: {MAX_PHOTOS}")

    model = YOLO("yolov8n.pt")
    cap = cv2.VideoCapture(0)
    cap.set(3, 640)
    cap.set(4, 480)

    last_alert_time = 0

    while True:
        success, img = cap.read()
        if not success: break

        results = model(img, stream=True, verbose=False)
        intruder_detected = False
        max_conf = 0

        for r in results:
            boxes = r.boxes
            for box in boxes:
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                conf = math.ceil((box.conf[0] * 100)) / 100
                cls = int(box.cls[0])

                if cls == 0 and conf > CONFIDENCE_THRESHOLD:
                    intruder_detected = True
                    max_conf = max(max_conf, conf)

                    cv2.rectangle(img, (x1, y1), (x2, y2), (0, 0, 255), 3)
                    cv2.putText(img, f"INTRUZ: {int(conf * 100)}%", (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6,
                                (0, 0, 255), 2)

        # --- LOGIKA ZAPISU ---
        if intruder_detected:
            current_time = time.time()

            # Sprawdzamy czy minęło 5 sekund od ostatniego zdjęcia
            if current_time - last_alert_time > PHOTO_DELAY:
                # 1. Najpierw zrób miejsce (usuń stare, jeśli trzeba)
                manage_storage()

                # 2. Zapisz nowe zdjęcie
                timestamp_str = time.strftime("%H-%M-%S")
                filename = f"intruz_{timestamp_str}.jpg"
                filepath = os.path.join(EVIDENCE_FOLDER, filename)

                cv2.imwrite(filepath, img)
                print(f"[FOTO] Zapisano dowód: {filepath}")

                # 3. Wyślij alert do C#
                data = {
                    "alert": True,
                    "confidence": max_conf,
                    "object": "human",
                    "timestamp": time.strftime("%H:%M:%S"),
                    "image_path": filepath
                }
                json_msg = json.dumps(data)
                sock.sendto(json_msg.encode(), (UDP_IP, UDP_PORT))

                last_alert_time = current_time

        cv2.imshow("VisionGuard AI", img)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()